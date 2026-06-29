using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.TeacherDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.TeacherPortal;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.TeacherPortal
{
    /// <summary>
    /// Phase 9 — Teacher Portal aggregations. All reads are scoped to the
    /// teacher's ACTIVE assignments (TeacherClassAssignment for classes/students,
    /// TeacherSubjectAssignment for subjects/quizzes) and the trusted tenant claim.
    /// SchoolAdmin sees the whole tenant. Cross-tenant ids resolve to 404 via the
    /// global tenant query filter; same-tenant-but-unassigned resolves to 403.
    /// </summary>
    public class TeacherPortalService : ITeacherPortalService
    {
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;
        private readonly UserManager<ApplicationUser> _users;

        public TeacherPortalService(IUnitOfWork uow, ITenantContext tenant, UserManager<ApplicationUser> users)
        {
            _uow = uow;
            _tenant = tenant;
            _users = users;
        }

        public async Task<ApiResponse<TeacherDashboardDto>> DashboardAsync(CancellationToken ct = default)
        {
            RequireTenant();
            var classIds = await AssignedClassIdsAsync();
            var subjectIds = await AssignedSubjectIdsAsync();
            var studentIds = await EnrolledStudentIdsAsync(classIds);

            var quizzes = subjectIds.Count == 0
                ? new List<Quiz>()
                : (await _uow.Repository<Quiz, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<Quiz, string>(q => q.SubjectId != null && subjectIds.Contains(q.SubjectId)))).ToList();

            var publishedQuizIds = quizzes.Where(q => q.Status == QuizStatus.Published).Select(q => q.Id).ToList();
            var pendingGrading = publishedQuizIds.Count == 0
                ? 0
                : await _uow.Repository<QuizSubmission, string>().CountAsync(
                    new CriteriaSpecification<QuizSubmission, string>(s =>
                        publishedQuizIds.Contains(s.QuizId) && s.submissionStatus == SubmissionStatus.Submitted));

            var dto = new TeacherDashboardDto
            {
                TeacherId = _tenant.UserId ?? string.Empty,
                AssignedClassCount = classIds.Count,
                AssignedSubjectCount = subjectIds.Count,
                StudentCount = studentIds.Count,
                DraftQuizCount = quizzes.Count(q => q.Status != QuizStatus.Published && q.Status != QuizStatus.Archived),
                PublishedQuizCount = publishedQuizIds.Count,
                PendingGradingCount = pendingGrading,
                GeneratedAt = DateTime.UtcNow
            };
            return Ok(dto);
        }

        public async Task<ApiResponse<IEnumerable<TeacherClassDto>>> MyClassesAsync(CancellationToken ct = default)
        {
            RequireTenant();
            var classIds = await AssignedClassIdsAsync();
            if (classIds.Count == 0) return Ok(Enumerable.Empty<TeacherClassDto>());

            var classes = (await _uow.Repository<SchoolClass, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<SchoolClass, string>(c => classIds.Contains(c.Id)))).ToList();

            var enrollments = (await _uow.Repository<Enrollment, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Enrollment, string>(e =>
                    classIds.Contains(e.SchoolClassId) && e.Status == EnrollmentStatus.Active))).ToList();

            var rows = classes.Select(c => new TeacherClassDto
            {
                ClassId = c.Id,
                Name = c.Name ?? string.Empty,
                Code = c.Code,
                GradeId = c.GradeId,
                StudentCount = enrollments.Count(e => e.SchoolClassId == c.Id)
            }).OrderBy(c => c.Name).ToList();
            return Ok<IEnumerable<TeacherClassDto>>(rows);
        }

        public async Task<ApiResponse<IEnumerable<TeacherSubjectDto>>> MySubjectsAsync(CancellationToken ct = default)
        {
            RequireTenant();
            var subjectIds = await AssignedSubjectIdsAsync();
            if (subjectIds.Count == 0) return Ok(Enumerable.Empty<TeacherSubjectDto>());

            var subjects = (await _uow.Repository<Subject, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Subject, string>(s => subjectIds.Contains(s.Id)))).ToList();

            var quizzes = (await _uow.Repository<Quiz, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Quiz, string>(q => q.SubjectId != null && subjectIds.Contains(q.SubjectId)))).ToList();

            var rows = subjects.Select(s => new TeacherSubjectDto
            {
                SubjectId = s.Id,
                Name = s.Name ?? string.Empty,
                GradeId = s.GradeId,
                QuizCount = quizzes.Count(q => q.SubjectId == s.Id)
            }).OrderBy(s => s.Name).ToList();
            return Ok<IEnumerable<TeacherSubjectDto>>(rows);
        }

        public async Task<ApiResponse<IEnumerable<TeacherStudentDto>>> ClassStudentsAsync(string classId, CancellationToken ct = default)
        {
            RequireTenant();

            // Same-tenant existence first (cross-tenant / unknown → 404, no leak).
            var schoolClass = await _uow.Repository<SchoolClass, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<SchoolClass, string>(c => c.Id == classId))
                ?? throw new NotFoundException("Class not found.");

            await AuthorizeClassAsync(schoolClass.Id);

            var enrollments = (await _uow.Repository<Enrollment, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Enrollment, string>(e =>
                    e.SchoolClassId == classId && e.Status == EnrollmentStatus.Active))).ToList();
            var ids = enrollments.Select(e => e.StudentId).Distinct().ToList();
            if (ids.Count == 0) return Ok(Enumerable.Empty<TeacherStudentDto>());

            var students = await _users.Users.Where(u => ids.Contains(u.Id) && !u.IsDeleted).ToListAsync(ct);
            var rows = students.Select(s => new TeacherStudentDto
            {
                StudentId = s.Id,
                FullName = s.FullName ?? string.Empty,
                ClassId = classId
            }).OrderBy(s => s.FullName).ToList();
            return Ok<IEnumerable<TeacherStudentDto>>(rows);
        }

        // ---- assignment scope resolvers ----

        private async Task<List<string>> AssignedClassIdsAsync()
        {
            if (_tenant.Role == Roles.SchoolAdmin)
            {
                var all = await _uow.Repository<SchoolClass, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<SchoolClass, string>(c => c.Id != null));
                return all.Select(c => c.Id).Distinct().ToList();
            }
            var assignments = await _uow.Repository<TeacherClassAssignment, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<TeacherClassAssignment, string>(a => a.TeacherId == _tenant.UserId && a.IsActive));
            return assignments.Select(a => a.SchoolClassId).Distinct().ToList();
        }

        private async Task<List<string>> AssignedSubjectIdsAsync()
        {
            if (_tenant.Role == Roles.SchoolAdmin)
            {
                var all = await _uow.Repository<Subject, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<Subject, string>(s => s.Id != null));
                return all.Select(s => s.Id).Distinct().ToList();
            }
            var assignments = await _uow.Repository<TeacherSubjectAssignment, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<TeacherSubjectAssignment, string>(a => a.TeacherId == _tenant.UserId && a.IsActive));
            return assignments.Select(a => a.SubjectId).Distinct().ToList();
        }

        private async Task<List<string>> EnrolledStudentIdsAsync(List<string> classIds)
        {
            if (classIds.Count == 0) return new List<string>();
            var enrolled = await _uow.Repository<Enrollment, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Enrollment, string>(e =>
                    classIds.Contains(e.SchoolClassId) && e.Status == EnrollmentStatus.Active));
            return enrolled.Select(e => e.StudentId).Distinct().ToList();
        }

        private async Task AuthorizeClassAsync(string classId)
        {
            if (_tenant.Role == Roles.SchoolAdmin) return;
            if (_tenant.Role == Roles.Teacher)
            {
                var ok = await _uow.Repository<TeacherClassAssignment, string>().CountAsync(
                    new CriteriaSpecification<TeacherClassAssignment, string>(a =>
                        a.TeacherId == _tenant.UserId && a.SchoolClassId == classId && a.IsActive));
                if (ok > 0) return;
            }
            throw new ForbiddenException("You are not assigned to this class.");
        }

        private string RequireTenant() =>
            _tenant.TenantId ?? throw new UnauthorizedException("Tenant context is required for this operation.");

        private static ApiResponse<T> Ok<T>(T data) => new(data) { Success = true, StatusCode = 200, Message = "Records retrieved successfully." };
    }
}
