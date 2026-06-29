using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.ParentDto;
using DerasaX.Application.Dto.ProgressDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Authorization;
using DerasaX.Application.Services.Abstractions.ParentPortal;
using DerasaX.Application.Services.Abstractions.Progress;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.ParentPortal
{
    /// <summary>
    /// Phase 10 — Parent Portal aggregations. Listing reads are scoped to the
    /// parent's ACTIVE, progress-permitted links (ParentStudentRelationship); every
    /// per-child read is additionally routed through the shared
    /// <see cref="IStudentAccessAuthorizer"/> so the same-tenant-unlinked → 403 and
    /// cross-tenant/unknown → 404 rules are enforced in exactly one place. Deeper
    /// per-child reads (lessons, attempts, insights, grades) reuse the existing
    /// relationship-authorized <c>api/v1/students/{id}/...</c> endpoints. Attendance
    /// is exposed here because no parent-readable attendance path existed before.
    /// No AI runs on read; empty data returns an empty result, never fabricated rows.
    /// </summary>
    public class ParentPortalService : IParentPortalService
    {
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;
        private readonly IStudentAccessAuthorizer _access;
        private readonly IStudentProgressService _progress;
        private readonly UserManager<ApplicationUser> _users;

        public ParentPortalService(
            IUnitOfWork uow,
            ITenantContext tenant,
            IStudentAccessAuthorizer access,
            IStudentProgressService progress,
            UserManager<ApplicationUser> users)
        {
            _uow = uow;
            _tenant = tenant;
            _access = access;
            _progress = progress;
            _users = users;
        }

        public async Task<ApiResponse<ParentDashboardDto>> DashboardAsync(CancellationToken ct = default)
        {
            RequireTenant();
            var parentId = RequireParent();
            var links = await ActiveLinksAsync(parentId);

            return Ok(new ParentDashboardDto
            {
                ParentId = parentId,
                LinkedChildrenCount = links.Select(l => l.StudentId).Distinct().Count(),
                GeneratedAt = DateTime.UtcNow
            });
        }

        public async Task<ApiResponse<IEnumerable<ParentChildDto>>> ChildrenAsync(CancellationToken ct = default)
        {
            RequireTenant();
            var parentId = RequireParent();
            var links = await ActiveLinksAsync(parentId);
            if (links.Count == 0) return Ok(Enumerable.Empty<ParentChildDto>());

            var studentIds = links.Select(l => l.StudentId).Distinct().ToList();
            var students = await _users.Users.Where(u => studentIds.Contains(u.Id) && !u.IsDeleted).ToListAsync(ct);

            var enrollments = (await _uow.Repository<Enrollment, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Enrollment, string>(e =>
                    studentIds.Contains(e.StudentId) && e.Status == EnrollmentStatus.Active))).ToList();
            var classIds = enrollments.Select(e => e.SchoolClassId).Distinct().ToList();
            var classes = classIds.Count == 0
                ? new List<SchoolClass>()
                : (await _uow.Repository<SchoolClass, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<SchoolClass, string>(c => classIds.Contains(c.Id)))).ToList();

            var rows = new List<ParentChildDto>();
            foreach (var student in students)
            {
                var link = links.First(l => l.StudentId == student.Id);
                var enrollment = enrollments.FirstOrDefault(e => e.StudentId == student.Id);
                var schoolClass = enrollment is null ? null : classes.FirstOrDefault(c => c.Id == enrollment.SchoolClassId);

                // Reuse the relationship-authorized progress summary (re-verifies the link).
                var summary = (await _progress.SummaryAsync(student.Id, ct)).Data ?? new ProgressSummaryDto();

                rows.Add(new ParentChildDto
                {
                    StudentId = student.Id,
                    FullName = student.FullName ?? string.Empty,
                    GradeId = (student as Student)?.GradeId,
                    Relationship = link.Relationship.ToString(),
                    IsPrimary = link.IsPrimary,
                    CanViewProgress = link.CanViewProgress,
                    CanRequestDocuments = link.CanRequestDocuments,
                    CanContactTeachers = link.CanContactTeachers,
                    ClassId = schoolClass?.Id,
                    ClassName = schoolClass?.Name,
                    LessonsTracked = summary.LessonsTracked,
                    LessonsCompleted = summary.LessonsCompleted,
                    AverageLessonCompletion = summary.AverageLessonCompletion,
                    QuizAttempts = summary.QuizAttempts,
                    AverageQuizPercentage = summary.AverageQuizPercentage,
                    SubjectsTracked = summary.SubjectsTracked
                });
            }

            return Ok<IEnumerable<ParentChildDto>>(rows.OrderBy(r => r.FullName).ToList());
        }

        public async Task<ApiResponse<ParentChildOverviewDto>> ChildOverviewAsync(string childId, CancellationToken ct = default)
        {
            RequireTenant();
            var parentId = RequireParent();

            // Centralized relationship + tenant rule: 404 cross-tenant/unknown, 403 unlinked.
            await _access.EnsureCanAccessStudentAsync(childId, ct);

            var student = await _users.Users.FirstOrDefaultAsync(u => u.Id == childId, ct)
                ?? throw new NotFoundException("Student not found.");

            var link = await _uow.Repository<ParentStudentRelationship, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<ParentStudentRelationship, string>(r =>
                    r.ParentId == parentId && r.StudentId == childId && r.IsActive && r.CanViewProgress))
                ?? throw new ForbiddenException("You can only access your linked children.");

            var enrollment = (await _uow.Repository<Enrollment, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Enrollment, string>(e =>
                    e.StudentId == childId && e.Status == EnrollmentStatus.Active))).FirstOrDefault();
            SchoolClass? schoolClass = null;
            if (enrollment is not null)
            {
                schoolClass = await _uow.Repository<SchoolClass, string>().GetByIdWithSpecAsync(
                    new CriteriaSpecification<SchoolClass, string>(c => c.Id == enrollment.SchoolClassId));
            }

            var summary = (await _progress.SummaryAsync(childId, ct)).Data ?? new ProgressSummaryDto();

            return Ok(new ParentChildOverviewDto
            {
                StudentId = student.Id,
                FullName = student.FullName ?? string.Empty,
                GradeId = (student as Student)?.GradeId,
                Relationship = link.Relationship.ToString(),
                IsPrimary = link.IsPrimary,
                CanViewProgress = link.CanViewProgress,
                CanRequestDocuments = link.CanRequestDocuments,
                CanContactTeachers = link.CanContactTeachers,
                ClassId = schoolClass?.Id,
                ClassName = schoolClass?.Name,
                AcademicYearId = enrollment?.AcademicYearId,
                Summary = summary
            });
        }

        public async Task<ApiResponse<StudentAttendanceDto>> ChildAttendanceAsync(string childId, ProgressParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            RequireParent();

            // Centralized relationship + tenant rule: 404 cross-tenant/unknown, 403 unlinked.
            await _access.EnsureCanAccessStudentAsync(childId, ct);

            var (from, to) = ValidateRange(p);
            Expression<Func<StudentAttendanceRecord, bool>> criteria = x =>
                x.StudentId == childId &&
                (from == null || x.AttendanceDate >= from) &&
                (to == null || x.AttendanceDate <= to);

            var rows = (await _uow.Repository<StudentAttendanceRecord, string>().GetAllWithSpecAsync(
                new PagedSpecification<StudentAttendanceRecord, string>(criteria, x => x.AttendanceDate, p.PageNumber, p.PageSize, descending: true))).ToList();

            var summaryRows = (await _uow.Repository<StudentAttendanceRecord, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<StudentAttendanceRecord, string>(criteria))).ToList();

            var present = summaryRows.Count(x => x.Status == AttendanceStatus.Present);
            var late = summaryRows.Count(x => x.Status == AttendanceStatus.Late);
            var total = summaryRows.Count;

            var dto = new StudentAttendanceDto
            {
                Summary = new AttendanceSummaryDto
                {
                    Total = total,
                    Present = present,
                    Absent = summaryRows.Count(x => x.Status == AttendanceStatus.Absent),
                    Late = late,
                    Excused = summaryRows.Count(x => x.Status == AttendanceStatus.Excused),
                    AttendancePercentage = total == 0 ? 0 : Math.Round((decimal)(present + late) / total * 100m, 2)
                },
                Records = rows.Select(x => new AttendanceRecordDto
                {
                    Id = x.Id,
                    AttendanceDate = x.AttendanceDate,
                    Status = x.Status.ToString(),
                    RecordedAt = x.RecordedAt,
                    Source = x.Source.ToString(),
                    SessionKey = x.SessionKey,
                    SchoolClassId = x.SchoolClassId,
                    Notes = x.Notes
                }).ToList()
            };

            return Ok(dto);
        }

        // ---- helpers ----

        private async Task<List<ParentStudentRelationship>> ActiveLinksAsync(string parentId)
        {
            var links = await _uow.Repository<ParentStudentRelationship, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<ParentStudentRelationship, string>(r =>
                    r.ParentId == parentId && r.IsActive && r.CanViewProgress));
            return links.ToList();
        }

        private static (DateTime? from, DateTime? to) ValidateRange(ProgressParameters p)
        {
            DateTime? from = p.From.HasValue ? AsUtc(p.From.Value) : null;
            DateTime? to = p.To.HasValue ? AsUtc(p.To.Value) : null;
            if (from.HasValue && to.HasValue && to < from)
                throw new BadRequestException("'To' must be on or after 'From'.");
            return (from, to);
        }

        private static DateTime AsUtc(DateTime v) => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc);

        private string RequireTenant() =>
            _tenant.TenantId ?? throw new UnauthorizedException("Tenant context is required for this operation.");

        private string RequireParent() =>
            _tenant.UserId ?? throw new UnauthorizedException("Authenticated parent context is required.");

        private static ApiResponse<T> Ok<T>(T data) => new(data) { Success = true, StatusCode = 200, Message = "Records retrieved successfully." };
    }
}
