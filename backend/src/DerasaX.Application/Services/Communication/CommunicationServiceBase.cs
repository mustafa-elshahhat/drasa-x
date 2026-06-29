using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.Communication
{
    /// <summary>
    /// Shared base for the Phase 5 communication services. Centralises trusted-tenant/identity
    /// resolution, role helpers, the parent↔teacher relationship rules (which gate who may
    /// contact whom), same-tenant user lookups and notification staging.
    /// </summary>
    public abstract class CommunicationServiceBase
    {
        protected readonly IUnitOfWork UnitOfWork;
        protected readonly ITenantContext Tenant;
        protected readonly IAuditWriter Audit;
        protected readonly UserManager<ApplicationUser> Users;

        protected CommunicationServiceBase(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit,
            UserManager<ApplicationUser> users)
        {
            UnitOfWork = unitOfWork;
            Tenant = tenant;
            Audit = audit;
            Users = users;
        }

        protected string RequireTenant() =>
            Tenant.TenantId ?? throw new UnauthorizedException("Tenant context is required for this operation.");

        protected string RequireUser() =>
            Tenant.UserId ?? throw new UnauthorizedException("Authenticated user is required for this operation.");

        protected bool IsSchoolAdmin => Tenant.Role == Roles.SchoolAdmin;
        protected bool IsTeacher => Tenant.Role == Roles.Teacher;
        protected bool IsParent => Tenant.Role == Roles.Parent;
        protected bool IsStudent => Tenant.Role == Roles.Student;

        /// <summary>Loads a same-tenant user of the expected role, or throws 404 (no cross-tenant leak).</summary>
        protected async Task<ApplicationUser> RequireTenantUserAsync(string userId, string expectedRole, CancellationToken ct)
        {
            var tenantId = RequireTenant();
            var user = await Users.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null || user.TenantId != tenantId || user.IsDeleted)
                throw new NotFoundException("User not found.");
            var roles = await Users.GetRolesAsync(user);
            if (!roles.Contains(expectedRole))
                throw new NotFoundException("User not found.");
            return user;
        }

        /// <summary>
        /// True when the parent may contact the teacher: an active linked child (with the
        /// contact-teachers permission) is enrolled in a class the teacher is assigned to.
        /// </summary>
        protected async Task<bool> ParentTeacherLinkedAsync(string parentId, string teacherId, string? studentId)
        {
            var links = await UnitOfWork.Repository<ParentStudentRelationship, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<ParentStudentRelationship, string>(r =>
                    r.ParentId == parentId && r.IsActive && r.CanContactTeachers));
            var childIds = links.Select(r => r.StudentId)
                .Where(id => studentId == null || id == studentId).ToHashSet();
            if (childIds.Count == 0) return false;

            var classes = await UnitOfWork.Repository<TeacherClassAssignment, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<TeacherClassAssignment, string>(a => a.TeacherId == teacherId && a.IsActive));
            var classIds = classes.Select(a => a.SchoolClassId).ToHashSet();
            if (classIds.Count == 0) return false;

            var enrolled = await UnitOfWork.Repository<Enrollment, string>().CountAsync(
                new CriteriaSpecification<Enrollment, string>(e =>
                    childIds.Contains(e.StudentId) && e.Status == EnrollmentStatus.Active && classIds.Contains(e.SchoolClassId)));
            return enrolled > 0;
        }

        /// <summary>
        /// True when the teacher and student share an active teaching relationship: the student is
        /// actively enrolled in a class the teacher is currently assigned to. Gates teacher↔student
        /// private messaging the same way the parent rule gates parent↔teacher messaging.
        /// </summary>
        protected async Task<bool> TeacherStudentLinkedAsync(string teacherId, string studentId)
        {
            var classes = await UnitOfWork.Repository<TeacherClassAssignment, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<TeacherClassAssignment, string>(a => a.TeacherId == teacherId && a.IsActive));
            var classIds = classes.Select(a => a.SchoolClassId).ToHashSet();
            if (classIds.Count == 0) return false;

            var enrolled = await UnitOfWork.Repository<Enrollment, string>().CountAsync(
                new CriteriaSpecification<Enrollment, string>(e =>
                    e.StudentId == studentId && e.Status == EnrollmentStatus.Active && classIds.Contains(e.SchoolClassId)));
            return enrolled > 0;
        }

        /// <summary>Returns the same-tenant user (or 404) together with whether they hold the given role.</summary>
        protected async Task<(ApplicationUser user, bool inRole)> LoadTenantUserWithRoleAsync(string userId, string role, CancellationToken ct)
        {
            var tenantId = RequireTenant();
            var user = await Users.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null || user.TenantId != tenantId || user.IsDeleted)
                throw new NotFoundException("User not found.");
            var roles = await Users.GetRolesAsync(user);
            return (user, roles.Contains(role));
        }

        // Phase 13 — preference-aware staging (mandatory categories are never suppressed) with honest
        // delivery-state, via the shared NotificationStaging helper. Real-time push happens after commit.
        protected Task StageNotificationAsync(string tenantId, string userId, string title, string body,
            NotificationCategory category = NotificationCategory.General) =>
            NotificationStaging.StageAsync(UnitOfWork, tenantId, userId, title, body, category,
                NotificationType.User, actorUserId: Tenant.UserId);

        protected static ApiResponse<T> Ok<T>(T data, int status, string message) =>
            new(data) { Success = true, StatusCode = status, Message = message };
    }
}
