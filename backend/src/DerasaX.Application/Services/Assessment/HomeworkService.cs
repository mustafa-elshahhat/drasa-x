using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
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
    public class HomeworkService : AssessmentServiceBase, IHomeworkService
    {
        private readonly UserManager<ApplicationUser> _users;

        public HomeworkService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit,
            UserManager<ApplicationUser> users) : base(unitOfWork, tenant, audit)
        {
            _users = users;
        }

        // ---------------- Teacher / SchoolAdmin ----------------

        public async Task<ApiResponse<HomeworkDto>> CreateDraftAsync(CreateHomeworkDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            EnsureTeacherOrAdmin();
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new BadRequestException("Title is required.");

            var type = ParseType(dto.Type);
            if (dto.AvailableFrom is { } af && dto.DueDate is { } dd && AsUtc(dd) < AsUtc(af))
                throw new BadRequestException("DueDate must be on or after AvailableFrom.");
            if (dto.MaxScore is { } ms && ms < 0) throw new BadRequestException("MaxScore cannot be negative.");

            if (!string.IsNullOrWhiteSpace(dto.SubjectId))
            {
                var subject = await UnitOfWork.Repository<Subject, string>().GetByIdWithSpecAsync(
                    new CriteriaSpecification<Subject, string>(s => s.Id == dto.SubjectId));
                if (subject is null) throw new BadRequestException("Subject not found in this tenant.");
            }

            var assignment = new Assignment
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                Title = dto.Title.Trim(),
                Description = dto.Description,
                Type = type,
                Status = AssignmentStatus.Draft,
                SubjectId = dto.SubjectId,
                LessonId = dto.LessonId,
                MaxScore = dto.MaxScore,
                AvailableFrom = AsUtc(dto.AvailableFrom),
                DueDate = AsUtc(dto.DueDate),
                AssignedByTeacherId = Tenant.UserId
            };
            await UnitOfWork.Repository<Assignment, string>().AddAsync(assignment);
            await Audit.StageAsync(AuditActionType.Create, nameof(Assignment), assignment.Id, "{\"action\":\"create-homework\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);

            return Ok(Map(assignment, new List<AssignmentTarget>()), 201, "Homework draft created.");
        }

        public async Task<ApiResponse<HomeworkDto>> UpdateAsync(string assignmentId, UpdateHomeworkDto dto, CancellationToken ct = default)
        {
            var assignment = await LoadManageableAssignmentAsync(assignmentId, ct);
            if (assignment.Status != AssignmentStatus.Draft)
                throw new ConflictException("Only a draft assignment can be edited.");

            if (!string.IsNullOrWhiteSpace(dto.Title)) assignment.Title = dto.Title.Trim();
            if (dto.Description is not null) assignment.Description = dto.Description;
            if (dto.MaxScore is { } ms)
            {
                if (ms < 0) throw new BadRequestException("MaxScore cannot be negative.");
                assignment.MaxScore = ms;
            }
            if (dto.AvailableFrom.HasValue) assignment.AvailableFrom = AsUtc(dto.AvailableFrom);
            if (dto.DueDate.HasValue) assignment.DueDate = AsUtc(dto.DueDate);
            if (assignment.AvailableFrom is { } a && assignment.DueDate is { } d && d < a)
                throw new BadRequestException("DueDate must be on or after AvailableFrom.");

            UnitOfWork.Repository<Assignment, string>().Update(assignment);
            await Audit.StageAsync(AuditActionType.Update, nameof(Assignment), assignment.Id, "{\"action\":\"update-homework\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);

            var targets = await LoadTargets(assignment.Id);
            return Ok(Map(assignment, targets), 200, "Homework updated.");
        }

        public async Task<ApiResponse<HomeworkDto>> PublishAsync(string assignmentId, PublishHomeworkDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var assignment = await LoadManageableAssignmentAsync(assignmentId, ct);
            if (assignment.Status != AssignmentStatus.Draft)
                throw new ConflictException("Only a draft assignment can be published.");

            var hasClass = !string.IsNullOrWhiteSpace(dto.SchoolClassId);
            var studentIds = dto.StudentIds.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            if (!hasClass && studentIds.Count == 0)
                throw new BadRequestException("At least one target (SchoolClassId or StudentIds) is required to publish.");

            var targets = new List<AssignmentTarget>();
            var notifyStudentIds = new HashSet<string>();

            if (hasClass)
            {
                var schoolClass = await UnitOfWork.Repository<SchoolClass, string>().GetByIdWithSpecAsync(
                    new CriteriaSpecification<SchoolClass, string>(c => c.Id == dto.SchoolClassId))
                    ?? throw new NotFoundException("Class not found.");
                targets.Add(NewTarget(tenantId, AssignmentTargetType.Class, schoolClassId: schoolClass.Id));

                var enrolled = await UnitOfWork.Repository<Enrollment, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<Enrollment, string>(e =>
                        e.SchoolClassId == schoolClass.Id && e.Status == EnrollmentStatus.Active));
                foreach (var e in enrolled) notifyStudentIds.Add(e.StudentId);
            }

            foreach (var sid in studentIds)
            {
                var student = await _users.Users.FirstOrDefaultAsync(u => u.Id == sid, ct);
                if (student is null || student.TenantId != tenantId || student.IsDeleted || student is not Student)
                    throw new NotFoundException("Student not found.");
                targets.Add(NewTarget(tenantId, AssignmentTargetType.Student, studentId: sid));
                notifyStudentIds.Add(sid);
            }

            assignment.Status = AssignmentStatus.Published;
            UnitOfWork.Repository<Assignment, string>().Update(assignment);
            foreach (var t in targets) { t.AssignmentId = assignment.Id; await UnitOfWork.Repository<AssignmentTarget, string>().AddAsync(t); }

            foreach (var sid in notifyStudentIds)
                await StageNotificationAsync(tenantId, sid, "New homework assigned",
                    $"New {assignment.Type.ToString().ToLowerInvariant()} '{assignment.Title}' has been assigned to you.",
                    NotificationCategory.QuizAssigned);

            await Audit.StageAsync(AuditActionType.Update, nameof(Assignment), assignment.Id, "{\"action\":\"publish-homework\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);

            return Ok(Map(assignment, targets), 200, "Homework published and assigned.");
        }

        public async Task<ApiResponse<IEnumerable<HomeworkDto>>> ListMineAsync(CancellationToken ct = default)
        {
            RequireTenant();
            EnsureTeacherOrAdmin();
            var userId = RequireUser();

            var assignments = (await UnitOfWork.Repository<Assignment, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Assignment, string>(a => a.Type != AssignmentType.Quiz))).ToList();
            // SchoolAdmin sees all non-quiz assignments; a teacher sees the ones they created.
            if (!IsSchoolAdmin)
                assignments = assignments.Where(a => a.AssignedByTeacherId == userId).ToList();

            var ids = assignments.Select(a => a.Id).ToList();
            var targets = ids.Count == 0 ? new List<AssignmentTarget>()
                : (await UnitOfWork.Repository<AssignmentTarget, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<AssignmentTarget, string>(t => ids.Contains(t.AssignmentId)))).ToList();

            var dto = assignments.OrderByDescending(a => a.CreatedAt)
                .Select(a => Map(a, targets.Where(t => t.AssignmentId == a.Id))).ToList();
            return Ok<IEnumerable<HomeworkDto>>(dto, 200, "Homework retrieved.");
        }

        public async Task<ApiResponse<HomeworkDto>> GetAsync(string assignmentId, CancellationToken ct = default)
        {
            var assignment = await LoadManageableAssignmentAsync(assignmentId, ct);
            var targets = await LoadTargets(assignment.Id);
            return Ok(Map(assignment, targets), 200, "Homework retrieved.");
        }

        public async Task<PaginationResponse<IEnumerable<HomeworkSubmissionDto>>> ListSubmissionsAsync(string assignmentId, HomeworkSubmissionParameters p, CancellationToken ct = default)
        {
            var assignment = await LoadManageableAssignmentAsync(assignmentId, ct);
            var repo = UnitOfWork.Repository<AssignmentSubmission, string>();
            System.Linq.Expressions.Expression<Func<AssignmentSubmission, bool>> criteria = s => s.AssignmentId == assignment.Id;
            var total = await repo.CountAsync(new CriteriaSpecification<AssignmentSubmission, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<AssignmentSubmission, string>(criteria, s => s.SubmittedAt, p.PageNumber, p.PageSize, descending: true));
            var dto = items.Select(MapSubmission).ToList();
            return new PaginationResponse<IEnumerable<HomeworkSubmissionDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Submissions retrieved." };
        }

        public async Task<ApiResponse<HomeworkSubmissionDto>> GradeAsync(string submissionId, GradeHomeworkDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var submission = await UnitOfWork.Repository<AssignmentSubmission, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<AssignmentSubmission, string>(s => s.Id == submissionId))
                ?? throw new NotFoundException("Submission not found.");
            var assignment = await LoadManageableAssignmentAsync(submission.AssignmentId, ct);

            if (dto.Score < 0) throw new BadRequestException("Score cannot be negative.");
            if (assignment.MaxScore is { } max && dto.Score > max)
                throw new BadRequestException($"Score cannot exceed the assignment maximum of {max}.");

            submission.Score = dto.Score;
            submission.Feedback = dto.Feedback;
            submission.Status = SubmissionStatus.Graded;
            submission.GradedAt = DateTime.UtcNow;
            submission.GradedByTeacherId = RequireUser();
            UnitOfWork.Repository<AssignmentSubmission, string>().Update(submission);

            await StageNotificationAsync(tenantId, submission.StudentId, "Homework graded",
                $"Your submission for '{assignment.Title}' has been graded.", NotificationCategory.QuizGraded);
            await Audit.StageAsync(AuditActionType.Update, nameof(AssignmentSubmission), submission.Id, "{\"action\":\"grade-homework\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);

            return Ok(MapSubmission(submission), 200, "Submission graded.");
        }

        // ---------------- Student ----------------

        public async Task<ApiResponse<IEnumerable<AssignedHomeworkDto>>> ListAssignedAsync(CancellationToken ct = default)
        {
            RequireTenant();
            if (!IsStudent) throw new ForbiddenException("Only a student may list their assigned homework.");
            var studentId = RequireUser();

            var enrolled = await UnitOfWork.Repository<Enrollment, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Enrollment, string>(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active));
            var classIds = enrolled.Select(e => e.SchoolClassId).ToHashSet();

            var assignments = (await UnitOfWork.Repository<Assignment, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Assignment, string>(a => a.Type != AssignmentType.Quiz && a.Status == AssignmentStatus.Published))).ToList();
            var ids = assignments.Select(a => a.Id).ToList();
            var targets = ids.Count == 0 ? new List<AssignmentTarget>()
                : (await UnitOfWork.Repository<AssignmentTarget, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<AssignmentTarget, string>(t => ids.Contains(t.AssignmentId)))).ToList();

            var eligible = assignments.Where(a => targets.Any(t => t.AssignmentId == a.Id &&
                ((t.TargetType == AssignmentTargetType.Student && t.StudentId == studentId) ||
                 (t.TargetType == AssignmentTargetType.Class && t.SchoolClassId != null && classIds.Contains(t.SchoolClassId))))).ToList();

            var eligibleIds = eligible.Select(a => a.Id).ToList();
            var submissions = eligibleIds.Count == 0 ? new List<AssignmentSubmission>()
                : (await UnitOfWork.Repository<AssignmentSubmission, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<AssignmentSubmission, string>(s => s.StudentId == studentId && eligibleIds.Contains(s.AssignmentId)))).ToList();

            var now = DateTime.UtcNow;
            var result = eligible.OrderByDescending(a => a.DueDate ?? DateTime.MaxValue).Select(a =>
            {
                var sub = submissions.FirstOrDefault(s => s.AssignmentId == a.Id);
                var windowOpen = a.AvailableFrom is null || a.AvailableFrom <= now;
                return new AssignedHomeworkDto
                {
                    AssignmentId = a.Id, Title = a.Title, Description = a.Description, Type = a.Type,
                    AvailableFrom = a.AvailableFrom, DueDate = a.DueDate, MaxScore = a.MaxScore,
                    HasSubmitted = sub is not null, SubmissionId = sub?.Id, SubmissionStatus = sub?.Status,
                    Score = sub?.Score, GradedAt = sub?.GradedAt, AttachmentFileId = sub?.AttachmentFileId,
                    CanSubmit = sub is null && windowOpen
                };
            }).ToList();
            return Ok<IEnumerable<AssignedHomeworkDto>>(result, 200, "Assigned homework retrieved.");
        }

        public async Task<ApiResponse<HomeworkSubmissionDto>> SubmitAsync(string assignmentId, SubmitHomeworkDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            if (!IsStudent) throw new ForbiddenException("Only a student may submit homework.");
            var studentId = RequireUser();

            var assignment = await UnitOfWork.Repository<Assignment, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Assignment, string>(a => a.Id == assignmentId))
                ?? throw new NotFoundException("Homework not found.");
            if (assignment.Type == AssignmentType.Quiz || assignment.Status != AssignmentStatus.Published)
                throw new NotFoundException("Homework not found.");

            if (!await StudentEligibleAsync(assignment.Id, studentId))
                throw new ForbiddenException("This homework is not assigned to you.");

            var now = DateTime.UtcNow;
            if (assignment.AvailableFrom is { } af && af > now)
                throw new ConflictException("This homework is not yet available.");

            var existing = await UnitOfWork.Repository<AssignmentSubmission, string>().CountAsync(
                new CriteriaSpecification<AssignmentSubmission, string>(s => s.AssignmentId == assignment.Id && s.StudentId == studentId));
            if (existing > 0) throw new ConflictException("You have already submitted this homework.");

            var status = assignment.DueDate is { } due && now > due ? SubmissionStatus.Late : SubmissionStatus.Submitted;
            var submission = new AssignmentSubmission
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                AssignmentId = assignment.Id,
                StudentId = studentId,
                Content = dto.Content,
                AttachmentFileId = dto.AttachmentFileId,
                Status = status,
                SubmittedAt = now
            };
            await UnitOfWork.Repository<AssignmentSubmission, string>().AddAsync(submission);

            if (!string.IsNullOrWhiteSpace(assignment.AssignedByTeacherId))
                await StageNotificationAsync(tenantId, assignment.AssignedByTeacherId!, "New homework submission",
                    $"A student submitted '{assignment.Title}'.", NotificationCategory.General);

            await Audit.StageAsync(AuditActionType.Create, nameof(AssignmentSubmission), submission.Id, "{\"action\":\"submit-homework\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);

            return Ok(MapSubmission(submission), 201, "Homework submitted.");
        }

        public async Task<ApiResponse<HomeworkSubmissionDto>> GetMySubmissionAsync(string assignmentId, CancellationToken ct = default)
        {
            RequireTenant();
            if (!IsStudent) throw new ForbiddenException("Only a student may view their submission.");
            var studentId = RequireUser();
            var submission = await UnitOfWork.Repository<AssignmentSubmission, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<AssignmentSubmission, string>(s => s.AssignmentId == assignmentId && s.StudentId == studentId))
                ?? throw new NotFoundException("Submission not found.");
            return Ok(MapSubmission(submission), 200, "Submission retrieved.");
        }

        // ---------------- helpers ----------------

        private void EnsureTeacherOrAdmin()
        {
            if (!IsTeacher && !IsSchoolAdmin)
                throw new ForbiddenException("Only a teacher or school administrator may manage homework.");
        }

        private async Task<Assignment> LoadManageableAssignmentAsync(string assignmentId, CancellationToken ct)
        {
            RequireTenant();
            EnsureTeacherOrAdmin();
            var assignment = await UnitOfWork.Repository<Assignment, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Assignment, string>(a => a.Id == assignmentId))
                ?? throw new NotFoundException("Homework not found.");
            if (assignment.Type == AssignmentType.Quiz)
                throw new NotFoundException("Homework not found."); // quiz assignments are managed via the quiz APIs

            if (IsSchoolAdmin) return assignment;

            var userId = RequireUser();
            if (assignment.AssignedByTeacherId == userId) return assignment;
            if (!string.IsNullOrEmpty(assignment.SubjectId))
            {
                var hasSubject = await UnitOfWork.Repository<TeacherSubjectAssignment, string>().CountAsync(
                    new CriteriaSpecification<TeacherSubjectAssignment, string>(a =>
                        a.TeacherId == userId && a.SubjectId == assignment.SubjectId && a.IsActive));
                if (hasSubject > 0) return assignment;
            }
            throw new ForbiddenException("You may only manage homework you created or for a subject you are assigned to.");
        }

        private async Task<bool> StudentEligibleAsync(string assignmentId, string studentId)
        {
            var targets = await UnitOfWork.Repository<AssignmentTarget, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<AssignmentTarget, string>(t => t.AssignmentId == assignmentId));
            if (targets.Any(t => t.TargetType == AssignmentTargetType.Student && t.StudentId == studentId))
                return true;

            var classTargetIds = targets.Where(t => t.TargetType == AssignmentTargetType.Class && t.SchoolClassId != null)
                .Select(t => t.SchoolClassId!).ToHashSet();
            if (classTargetIds.Count == 0) return false;

            var enrolledCount = await UnitOfWork.Repository<Enrollment, string>().CountAsync(
                new CriteriaSpecification<Enrollment, string>(e =>
                    e.StudentId == studentId && e.Status == EnrollmentStatus.Active && classTargetIds.Contains(e.SchoolClassId)));
            return enrolledCount > 0;
        }

        private async Task<List<AssignmentTarget>> LoadTargets(string assignmentId) =>
            (await UnitOfWork.Repository<AssignmentTarget, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<AssignmentTarget, string>(t => t.AssignmentId == assignmentId))).ToList();

        private static AssignmentTarget NewTarget(string tenantId, AssignmentTargetType type,
            string? schoolClassId = null, string? studentId = null) => new()
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            TargetType = type,
            SchoolClassId = schoolClassId,
            StudentId = studentId
        };

        private static AssignmentType ParseType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type)) return AssignmentType.Homework;
            if (!Enum.TryParse<AssignmentType>(type.Trim(), ignoreCase: true, out var parsed))
                throw new BadRequestException("Invalid assignment type.");
            if (parsed == AssignmentType.Quiz)
                throw new BadRequestException("Use the quiz APIs to assign a quiz; this endpoint creates non-quiz assignments.");
            return parsed;
        }

        private static HomeworkDto Map(Assignment a, IEnumerable<AssignmentTarget> targets) => new()
        {
            Id = a.Id, Title = a.Title, Description = a.Description, Type = a.Type, Status = a.Status,
            SubjectId = a.SubjectId, LessonId = a.LessonId, MaxScore = a.MaxScore,
            AvailableFrom = a.AvailableFrom, DueDate = a.DueDate,
            Targets = targets.Select(t => new AssignmentTargetDto
            {
                Id = t.Id, TargetType = t.TargetType, SchoolClassId = t.SchoolClassId, StudentId = t.StudentId
            }).ToList()
        };

        private static HomeworkSubmissionDto MapSubmission(AssignmentSubmission s) => new()
        {
            Id = s.Id, AssignmentId = s.AssignmentId, StudentId = s.StudentId, Content = s.Content,
            AttachmentFileId = s.AttachmentFileId, Status = s.Status, SubmittedAt = s.SubmittedAt,
            Score = s.Score, Feedback = s.Feedback, GradedAt = s.GradedAt
        };
    }
}
