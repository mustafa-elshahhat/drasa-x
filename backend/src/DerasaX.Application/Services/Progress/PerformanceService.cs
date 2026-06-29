using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.ProgressDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Authorization;
using DerasaX.Application.Services.Abstractions.Progress;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.Progress
{
    /// <summary>
    /// Cross-student dashboards and performance aggregations. All aggregation runs
    /// server-side from authoritative assessment records (no client computation, no AI),
    /// scoped to whichever students the caller is permitted to see.
    /// </summary>
    public class PerformanceService : IPerformanceService
    {
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;
        private readonly IStudentAccessAuthorizer _access;
        private readonly UserManager<ApplicationUser> _users;

        // Bound for an admin "all-tenant" dashboard list so it can never return unbounded data.
        private const int MaxDashboardStudents = 200;

        public PerformanceService(IUnitOfWork uow, ITenantContext tenant, IStudentAccessAuthorizer access,
            UserManager<ApplicationUser> users)
        {
            _uow = uow;
            _tenant = tenant;
            _access = access;
            _users = users;
        }

        public async Task<ApiResponse<IEnumerable<StudentDashboardRowDto>>> MyStudentsAsync(CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var scope = await _access.ResolveScopeAsync(ct);

            List<ApplicationUser> students;
            if (scope.AllTenant)
            {
                students = await _users.Users
                    .Where(u => u.TenantId == tenantId && !u.IsDeleted && u is Student)
                    .Take(MaxDashboardStudents).ToListAsync(ct);
            }
            else
            {
                var ids = scope.StudentIds.ToList();
                students = ids.Count == 0
                    ? new List<ApplicationUser>()
                    : await _users.Users.Where(u => ids.Contains(u.Id)).ToListAsync(ct);
            }

            var studentIds = students.Select(s => s.Id).ToList();
            var submissions = studentIds.Count == 0
                ? new List<QuizSubmission>()
                : (await _uow.Repository<QuizSubmission, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<QuizSubmission, string>(s =>
                        studentIds.Contains(s.StudentId) && s.submissionStatus != SubmissionStatus.InProgress))).ToList();

            var rows = students.Select(s =>
            {
                var mine = submissions.Where(x => x.StudentId == s.Id).ToList();
                return new StudentDashboardRowDto
                {
                    StudentId = s.Id,
                    FullName = s.FullName ?? string.Empty,
                    QuizAttempts = mine.Count,
                    AverageQuizPercentage = mine.Count == 0 ? 0 : (decimal)Math.Round(mine.Average(m => m.Percentage), 2)
                };
            }).ToList();
            return Ok<IEnumerable<StudentDashboardRowDto>>(rows);
        }

        public async Task<ApiResponse<ClassPerformanceDto>> ClassPerformanceAsync(string classId, CancellationToken ct = default)
        {
            RequireTenant();
            var schoolClass = await _uow.Repository<SchoolClass, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<SchoolClass, string>(c => c.Id == classId))
                ?? throw new NotFoundException("Class not found.");

            await AuthorizeClassAsync(schoolClass.Id);

            var enrolled = await _uow.Repository<Enrollment, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Enrollment, string>(e => e.SchoolClassId == classId && e.Status == EnrollmentStatus.Active));
            var studentIds = enrolled.Select(e => e.StudentId).Distinct().ToList();

            var submissions = studentIds.Count == 0
                ? new List<QuizSubmission>()
                : (await _uow.Repository<QuizSubmission, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<QuizSubmission, string>(s =>
                        studentIds.Contains(s.StudentId) && s.submissionStatus != SubmissionStatus.InProgress))).ToList();

            return Ok(new ClassPerformanceDto
            {
                ClassId = classId,
                StudentCount = studentIds.Count,
                QuizAttempts = submissions.Count,
                AverageQuizPercentage = submissions.Count == 0 ? 0 : (decimal)Math.Round(submissions.Average(s => s.Percentage), 2)
            });
        }

        public async Task<ApiResponse<SubjectPerformanceDto>> SubjectPerformanceAsync(string subjectId, CancellationToken ct = default)
        {
            RequireTenant();
            var subject = await _uow.Repository<Subject, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Subject, string>(s => s.Id == subjectId))
                ?? throw new NotFoundException("Subject not found.");

            await AuthorizeSubjectAsync(subject.Id);

            var quizzes = await _uow.Repository<Quiz, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Quiz, string>(q => q.SubjectId == subjectId));
            var quizIds = quizzes.Select(q => q.Id).ToList();
            var submissions = quizIds.Count == 0
                ? new List<QuizSubmission>()
                : (await _uow.Repository<QuizSubmission, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<QuizSubmission, string>(s =>
                        quizIds.Contains(s.QuizId) && s.submissionStatus != SubmissionStatus.InProgress))).ToList();

            return Ok(new SubjectPerformanceDto
            {
                SubjectId = subjectId,
                QuizCount = quizIds.Count,
                SubmissionCount = submissions.Count,
                AverageQuizPercentage = submissions.Count == 0 ? 0 : (decimal)Math.Round(submissions.Average(s => s.Percentage), 2)
            });
        }

        // ---- authorization helpers ----

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

        private async Task AuthorizeSubjectAsync(string subjectId)
        {
            if (_tenant.Role == Roles.SchoolAdmin) return;
            if (_tenant.Role == Roles.Teacher)
            {
                var ok = await _uow.Repository<TeacherSubjectAssignment, string>().CountAsync(
                    new CriteriaSpecification<TeacherSubjectAssignment, string>(a =>
                        a.TeacherId == _tenant.UserId && a.SubjectId == subjectId && a.IsActive));
                if (ok > 0) return;
            }
            throw new ForbiddenException("You are not assigned to this subject.");
        }

        private string RequireTenant() =>
            _tenant.TenantId ?? throw new UnauthorizedException("Tenant context is required for this operation.");

        private static ApiResponse<T> Ok<T>(T data) => new(data) { Success = true, StatusCode = 200, Message = "Records retrieved successfully." };
    }
}
