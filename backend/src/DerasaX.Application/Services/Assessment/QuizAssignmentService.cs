using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AssessmentDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Assessment;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.Assessment
{
    public class QuizAssignmentService : AssessmentServiceBase, IQuizAssignmentService
    {
        private readonly UserManager<ApplicationUser> _users;

        public QuizAssignmentService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit,
            UserManager<ApplicationUser> users) : base(unitOfWork, tenant, audit)
        {
            _users = users;
        }

        public async Task<ApiResponse<AssignmentDto>> AssignAsync(string quizId, AssignQuizDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var quiz = await LoadManageableQuizAsync(quizId, ct);
            if (quiz.Status != QuizStatus.Published)
                throw new ConflictException("Only a published quiz can be assigned.");

            var hasClass = !string.IsNullOrWhiteSpace(dto.SchoolClassId);
            var studentIds = dto.StudentIds.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            if (!hasClass && studentIds.Count == 0)
                throw new BadRequestException("At least one target (SchoolClassId or StudentIds) is required.");
            if (dto.AvailableFrom is { } af && dto.DueDate is { } dd && AsUtc(dd) < AsUtc(af))
                throw new BadRequestException("DueDate must be on or after AvailableFrom.");

            var targets = new List<AssignmentTarget>();
            var notifyStudentIds = new HashSet<string>();

            if (hasClass)
            {
                var schoolClass = await UnitOfWork.Repository<SchoolClass, string>()
                    .GetByIdWithSpecAsync(new CriteriaSpecification<SchoolClass, string>(c => c.Id == dto.SchoolClassId))
                    ?? throw new NotFoundException("Class not found.");
                targets.Add(NewTarget(tenantId, AssignmentTargetType.Class, schoolClassId: schoolClass.Id));

                var enrolled = await UnitOfWork.Repository<Enrollment, string>()
                    .GetAllWithSpecAsync(new CriteriaSpecification<Enrollment, string>(e =>
                        e.SchoolClassId == schoolClass.Id && e.Status == EnrollmentStatus.Active));
                foreach (var e in enrolled) notifyStudentIds.Add(e.StudentId);
            }

            foreach (var sid in studentIds)
            {
                // Same-tenant student integrity (cross-tenant id → 404, no existence leak).
                var student = await _users.Users.FirstOrDefaultAsync(u => u.Id == sid, ct);
                if (student is null || student.TenantId != tenantId || student.IsDeleted || student is not Student)
                    throw new NotFoundException("Student not found.");
                targets.Add(NewTarget(tenantId, AssignmentTargetType.Student, studentId: sid));
                notifyStudentIds.Add(sid);
            }

            var assignment = new Assignment
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                Title = quiz.Title ?? "Quiz",
                Type = AssignmentType.Quiz,
                Status = AssignmentStatus.Published,
                QuizId = quiz.Id,
                SubjectId = quiz.SubjectId,
                LessonId = quiz.LessonId,
                AvailableFrom = AsUtc(dto.AvailableFrom),
                DueDate = AsUtc(dto.DueDate) ?? AsUtc(quiz.DueDate),
                AssignedByTeacherId = Tenant.UserId
            };
            await UnitOfWork.Repository<Assignment, string>().AddAsync(assignment);
            foreach (var t in targets) { t.AssignmentId = assignment.Id; await UnitOfWork.Repository<AssignmentTarget, string>().AddAsync(t); }

            foreach (var sid in notifyStudentIds)
                await StageNotificationAsync(tenantId, sid, "New quiz assigned",
                    $"A new quiz '{quiz.Title}' has been assigned to you.", NotificationCategory.QuizAssigned);

            await Audit.StageAsync(AuditActionType.Create, nameof(Assignment), assignment.Id, "{\"action\":\"assign-quiz\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);

            return Ok(MapAssignment(assignment, targets), 201, "Quiz assigned successfully.");
        }

        public async Task<ApiResponse<IEnumerable<AssignmentDto>>> ListForQuizAsync(string quizId, CancellationToken ct = default)
        {
            var quiz = await LoadManageableQuizAsync(quizId, ct);
            var assignments = (await UnitOfWork.Repository<Assignment, string>()
                .GetAllWithSpecAsync(new CriteriaSpecification<Assignment, string>(a => a.QuizId == quiz.Id))).ToList();
            var ids = assignments.Select(a => a.Id).ToList();
            var targets = ids.Count == 0
                ? new List<AssignmentTarget>()
                : (await UnitOfWork.Repository<AssignmentTarget, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<AssignmentTarget, string>(t => ids.Contains(t.AssignmentId)))).ToList();

            var dto = assignments.Select(a => MapAssignment(a, targets.Where(t => t.AssignmentId == a.Id))).ToList();
            return Ok<IEnumerable<AssignmentDto>>(dto, 200, "Assignments retrieved successfully.");
        }

        public async Task<ApiResponse<IEnumerable<AssignedQuizDto>>> ListAssignedToMeAsync(CancellationToken ct = default)
        {
            RequireTenant();
            var studentId = RequireUser();

            // Classes the caller is actively enrolled in.
            var enrolled = await UnitOfWork.Repository<Enrollment, string>()
                .GetAllWithSpecAsync(new CriteriaSpecification<Enrollment, string>(e =>
                    e.StudentId == studentId && e.Status == EnrollmentStatus.Active));
            var classIds = enrolled.Select(e => e.SchoolClassId).ToHashSet();

            var assignments = (await UnitOfWork.Repository<Assignment, string>()
                .GetAllWithSpecAsync(new CriteriaSpecification<Assignment, string>(a =>
                    a.Type == AssignmentType.Quiz && a.Status == AssignmentStatus.Published && a.QuizId != null))).ToList();
            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var targets = assignmentIds.Count == 0
                ? new List<AssignmentTarget>()
                : (await UnitOfWork.Repository<AssignmentTarget, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<AssignmentTarget, string>(t => assignmentIds.Contains(t.AssignmentId)))).ToList();

            // Distinct quiz ids the student is eligible for.
            var eligible = assignments.Where(a => targets.Any(t => t.AssignmentId == a.Id &&
                ((t.TargetType == AssignmentTargetType.Student && t.StudentId == studentId) ||
                 (t.TargetType == AssignmentTargetType.Class && t.SchoolClassId != null && classIds.Contains(t.SchoolClassId)))))
                .GroupBy(a => a.QuizId!)
                .Select(g => g.OrderByDescending(a => a.DueDate ?? DateTime.MaxValue).First())
                .ToList();

            var quizIds = eligible.Select(a => a.QuizId!).ToList();
            var quizzes = quizIds.Count == 0
                ? new List<Quiz>()
                : (await UnitOfWork.Repository<Quiz, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<Quiz, string>(q => quizIds.Contains(q.Id) && q.Status == QuizStatus.Published))).ToList();
            var submissions = quizIds.Count == 0
                ? new List<QuizSubmission>()
                : (await UnitOfWork.Repository<QuizSubmission, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<QuizSubmission, string>(s => s.StudentId == studentId && quizIds.Contains(s.QuizId)))).ToList();

            // Question counts per quiz (for the list + tabs) and subject names — resolved in bulk.
            var questionCounts = quizIds.Count == 0
                ? new Dictionary<string, int>()
                : (await UnitOfWork.Repository<Question, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<Question, string>(q => quizIds.Contains(q.QuizId))))
                    .GroupBy(q => q.QuizId).ToDictionary(g => g.Key, g => g.Count());
            var subjectIds = quizzes.Where(q => q.SubjectId != null).Select(q => q.SubjectId!).Distinct().ToList();
            var subjectNames = subjectIds.Count == 0
                ? new Dictionary<string, string>()
                : (await UnitOfWork.Repository<Subject, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<Subject, string>(s => subjectIds.Contains(s.Id))))
                    .ToDictionary(s => s.Id, s => s.Name);

            var now = DateTime.UtcNow;
            var result = new List<AssignedQuizDto>();
            foreach (var quiz in quizzes)
            {
                var a = eligible.First(x => x.QuizId == quiz.Id);
                var quizSubs = submissions.Where(s => s.QuizId == quiz.Id).ToList();
                var used = quizSubs.Count(s => s.submissionStatus != SubmissionStatus.InProgress);
                var withinWindow = (a.AvailableFrom is null || a.AvailableFrom <= now) && (a.DueDate is null || a.DueDate >= now);
                var underLimit = quiz.MaxAttempts is null || used < quiz.MaxAttempts.Value;
                var canAttempt = withinWindow && underLimit;

                var latest = quizSubs.OrderByDescending(s => s.IsLatestAttempt).ThenByDescending(s => s.AttemptNumber).FirstOrDefault();
                int? score = null; double? pct = null;
                if (latest?.submissionStatus == SubmissionStatus.Graded) { score = latest.AchievedScore; pct = latest.Percentage; }

                result.Add(new AssignedQuizDto
                {
                    QuizId = quiz.Id, Title = quiz.Title, Type = quiz.Type,
                    SubjectName = quiz.SubjectId != null && subjectNames.TryGetValue(quiz.SubjectId, out var sn) ? sn : null,
                    TimeLimitMinutes = quiz.TimeLimitMinutes, MaxAttempts = quiz.MaxAttempts,
                    QuestionCount = questionCounts.TryGetValue(quiz.Id, out var qc) ? qc : 0,
                    AvailableFrom = a.AvailableFrom, DueDate = a.DueDate,
                    AttemptsUsed = used, CanAttempt = canAttempt,
                    LatestAttemptId = latest?.Id,
                    LatestAttemptStatus = latest?.submissionStatus,
                    Score = score, Percentage = pct,
                    Status = DeriveStatus(latest?.submissionStatus, canAttempt)
                });
            }
            return Ok<IEnumerable<AssignedQuizDto>>(result, 200, "Assigned quizzes retrieved successfully.");
        }

        /// <summary>Derives the stable student-facing lifecycle status from the latest attempt + attempt
        /// availability. A graded/submitted/in-progress attempt takes precedence over a fresh "available".</summary>
        private static string DeriveStatus(SubmissionStatus? latest, bool canAttempt) => latest switch
        {
            SubmissionStatus.InProgress => "in_progress",
            SubmissionStatus.Graded => "graded",
            SubmissionStatus.Submitted or SubmissionStatus.Late => "submitted",
            _ => canAttempt ? "available" : "closed"
        };

        private static AssignmentTarget NewTarget(string tenantId, AssignmentTargetType type,
            string? schoolClassId = null, string? studentId = null) => new()
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            TargetType = type,
            SchoolClassId = schoolClassId,
            StudentId = studentId
        };

        private static AssignmentDto MapAssignment(Assignment a, IEnumerable<AssignmentTarget> targets) => new()
        {
            Id = a.Id, QuizId = a.QuizId ?? string.Empty, Title = a.Title, Status = a.Status,
            AvailableFrom = a.AvailableFrom, DueDate = a.DueDate,
            Targets = targets.Select(t => new AssignmentTargetDto
            {
                Id = t.Id, TargetType = t.TargetType, SchoolClassId = t.SchoolClassId, StudentId = t.StudentId
            }).ToList()
        };
    }
}
