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

namespace DerasaX.Application.Services.Assessment
{
    /// <summary>
    /// Shared base for the Phase 5 assessment services. Centralises trusted-tenant
    /// resolution, role helpers, quiz-management authorization (SchoolAdmin, or a
    /// teacher with an active subject assignment), and notification staging so every
    /// assessment service reuses the same primitives. All reads run through the unit of
    /// work, whose DbContext applies the global tenant filter (cross-tenant ids → 404).
    /// </summary>
    public abstract class AssessmentServiceBase
    {
        protected readonly IUnitOfWork UnitOfWork;
        protected readonly ITenantContext Tenant;
        protected readonly IAuditWriter Audit;

        protected AssessmentServiceBase(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit)
        {
            UnitOfWork = unitOfWork;
            Tenant = tenant;
            Audit = audit;
        }

        protected static DateTime AsUtc(DateTime value) =>
            value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        protected static DateTime? AsUtc(DateTime? value) => value.HasValue ? AsUtc(value.Value) : null;

        protected string RequireTenant()
        {
            var tenantId = Tenant.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new UnauthorizedException("Tenant context is required for this operation.");
            return tenantId;
        }

        protected string RequireUser() =>
            Tenant.UserId ?? throw new UnauthorizedException("Authenticated user is required for this operation.");

        protected bool IsSchoolAdmin => Tenant.Role == Roles.SchoolAdmin;
        protected bool IsTeacher => Tenant.Role == Roles.Teacher;
        protected bool IsStudent => Tenant.Role == Roles.Student;

        /// <summary>
        /// Loads a quiz scoped to the caller's tenant (cross-tenant → 404), then authorizes
        /// management: SchoolAdmin may manage any quiz in the tenant; a teacher may manage a
        /// quiz only when it is anchored to a subject they hold an active assignment for, or
        /// when they are the recorded approving/reviewing teacher. Otherwise → 403.
        /// </summary>
        protected async Task<Quiz> LoadManageableQuizAsync(string quizId, CancellationToken ct)
        {
            RequireTenant();
            var quiz = await UnitOfWork.Repository<Quiz, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<Quiz, string>(q => q.Id == quizId))
                ?? throw new NotFoundException("Quiz not found.");

            await AuthorizeManageAsync(quiz, ct);
            return quiz;
        }

        protected async Task AuthorizeManageAsync(Quiz quiz, CancellationToken ct)
        {
            if (IsSchoolAdmin) return;
            if (!IsTeacher)
                throw new ForbiddenException("Only an assigned teacher or a school administrator may manage this quiz.");

            var userId = RequireUser();
            if (quiz.ApprovedByTeacherId == userId || quiz.ReviewedByTeacherId == userId)
                return;

            if (!string.IsNullOrEmpty(quiz.SubjectId))
            {
                var hasSubject = await UnitOfWork.Repository<TeacherSubjectAssignment, string>()
                    .CountAsync(new CriteriaSpecification<TeacherSubjectAssignment, string>(a =>
                        a.TeacherId == userId && a.SubjectId == quiz.SubjectId && a.IsActive));
                if (hasSubject > 0) return;
            }

            throw new ForbiddenException("You are not an assigned teacher for this quiz's subject.");
        }

        /// <summary>
        /// Resolves whether a student is eligible for a quiz via a REAL relationship: a
        /// published quiz assignment targeting the student directly, or a class the student
        /// is actively enrolled in. Returns the assignment window (availability/due) so the
        /// backend — never the client — enforces start/due dates. Returns null when not eligible.
        /// </summary>
        protected async Task<(string assignmentId, DateTime? availableFrom, DateTime? dueDate)?>
            ResolveAssignmentAsync(string quizId, string studentId, CancellationToken ct = default)
        {
            // Classes the student is actively enrolled in (real enrollment relationship).
            var enrolled = await UnitOfWork.Repository<Enrollment, string>()
                .GetAllWithSpecAsync(new CriteriaSpecification<Enrollment, string>(e =>
                    e.StudentId == studentId && e.Status == EnrollmentStatus.Active));
            var classIds = enrolled.Select(e => e.SchoolClassId).ToHashSet();

            var assignments = (await UnitOfWork.Repository<Assignment, string>()
                .GetAllWithSpecAsync(new CriteriaSpecification<Assignment, string>(a =>
                    a.QuizId == quizId && a.Status == AssignmentStatus.Published))).ToList();
            if (assignments.Count == 0) return null;

            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var targets = await UnitOfWork.Repository<AssignmentTarget, string>()
                .GetAllWithSpecAsync(new CriteriaSpecification<AssignmentTarget, string>(t =>
                    assignmentIds.Contains(t.AssignmentId)));

            foreach (var a in assignments)
            {
                var matched = targets.Any(t => t.AssignmentId == a.Id &&
                    ((t.TargetType == AssignmentTargetType.Student && t.StudentId == studentId) ||
                     (t.TargetType == AssignmentTargetType.Class && t.SchoolClassId != null && classIds.Contains(t.SchoolClassId))));
                if (matched)
                    return (a.Id, a.AvailableFrom, a.DueDate);
            }
            return null;
        }

        // Phase 13 — preference-aware staging (mandatory categories are never suppressed) with honest
        // delivery-state, via the shared NotificationStaging helper. Real-time push happens after commit.
        protected Task StageNotificationAsync(string tenantId, string userId, string title, string body,
            NotificationCategory category = NotificationCategory.General) =>
            NotificationStaging.StageAsync(UnitOfWork, tenantId, userId, title, body, category,
                NotificationType.System, actorUserId: Tenant.UserId);

        protected static ApiResponse<T> Ok<T>(T data, int status, string message) =>
            new(data) { Success = true, StatusCode = status, Message = message };
    }
}
