using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Authorization;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.Authorization
{
    /// <inheritdoc />
    public class StudentAccessAuthorizer : IStudentAccessAuthorizer
    {
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;
        private readonly UserManager<ApplicationUser> _users;

        public StudentAccessAuthorizer(IUnitOfWork uow, ITenantContext tenant, UserManager<ApplicationUser> users)
        {
            _uow = uow;
            _tenant = tenant;
            _users = users;
        }

        public async Task EnsureCanAccessStudentAsync(string studentId, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();

            // Same-tenant existence check first (cross-tenant / unknown → 404, no leak).
            var student = await _users.Users.FirstOrDefaultAsync(u => u.Id == studentId, ct);
            if (student is null || student.TenantId != tenantId || student.IsDeleted || student is not Student)
                throw new NotFoundException("Student not found.");

            var role = _tenant.Role;
            var caller = _tenant.UserId;

            if (role == Roles.SchoolAdmin) return;

            if (role == Roles.Student)
            {
                if (caller == studentId) return;
                throw new ForbiddenException("You can only access your own records.");
            }

            if (role == Roles.Teacher)
            {
                if (await TeacherTeachesStudentAsync(caller!, studentId)) return;
                throw new ForbiddenException("You can only access students in classes you are assigned to.");
            }

            if (role == Roles.Parent)
            {
                if (await ParentLinkedToStudentAsync(caller!, studentId)) return;
                throw new ForbiddenException("You can only access your linked children.");
            }

            throw new ForbiddenException("You are not permitted to access student records.");
        }

        public async Task<StudentScope> ResolveScopeAsync(CancellationToken ct = default)
        {
            RequireTenant();
            var role = _tenant.Role;
            var caller = _tenant.UserId;

            if (role == Roles.SchoolAdmin)
                return new StudentScope(AllTenant: true, StudentIds: System.Array.Empty<string>());

            if (role == Roles.Student)
                return new StudentScope(false, new[] { caller! });

            if (role == Roles.Teacher)
                return new StudentScope(false, await TeacherStudentIdsAsync(caller!));

            if (role == Roles.Parent)
                return new StudentScope(false, await ParentChildIdsAsync(caller!));

            throw new ForbiddenException("You are not permitted to access student records.");
        }

        // ---- relationship resolvers ----

        private async Task<bool> TeacherTeachesStudentAsync(string teacherId, string studentId)
        {
            var classIds = await TeacherClassIdsAsync(teacherId);
            if (classIds.Count == 0) return false;
            var count = await _uow.Repository<Enrollment, string>().CountAsync(
                new CriteriaSpecification<Enrollment, string>(e =>
                    e.StudentId == studentId && e.Status == EnrollmentStatus.Active && classIds.Contains(e.SchoolClassId)));
            return count > 0;
        }

        private async Task<IReadOnlyList<string>> TeacherStudentIdsAsync(string teacherId)
        {
            var classIds = await TeacherClassIdsAsync(teacherId);
            if (classIds.Count == 0) return System.Array.Empty<string>();
            var enrolled = await _uow.Repository<Enrollment, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Enrollment, string>(e =>
                    e.Status == EnrollmentStatus.Active && classIds.Contains(e.SchoolClassId)));
            return enrolled.Select(e => e.StudentId).Distinct().ToList();
        }

        private async Task<List<string>> TeacherClassIdsAsync(string teacherId)
        {
            var assignments = await _uow.Repository<TeacherClassAssignment, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<TeacherClassAssignment, string>(a => a.TeacherId == teacherId && a.IsActive));
            return assignments.Select(a => a.SchoolClassId).Distinct().ToList();
        }

        private async Task<bool> ParentLinkedToStudentAsync(string parentId, string studentId)
        {
            var count = await _uow.Repository<ParentStudentRelationship, string>().CountAsync(
                new CriteriaSpecification<ParentStudentRelationship, string>(r =>
                    r.ParentId == parentId && r.StudentId == studentId && r.IsActive && r.CanViewProgress));
            return count > 0;
        }

        private async Task<IReadOnlyList<string>> ParentChildIdsAsync(string parentId)
        {
            var links = await _uow.Repository<ParentStudentRelationship, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<ParentStudentRelationship, string>(r =>
                    r.ParentId == parentId && r.IsActive && r.CanViewProgress));
            return links.Select(r => r.StudentId).Distinct().ToList();
        }

        private string RequireTenant() =>
            _tenant.TenantId ?? throw new UnauthorizedException("Tenant context is required for this operation.");
    }
}
