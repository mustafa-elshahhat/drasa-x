using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.SchoolAdminDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Application.Services.Abstractions.SchoolAdminPortal;
using DerasaX.Application.Services.Communication;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.SchoolAdminPortal
{
    /// <summary>
    /// Phase 11 — School Admin Portal service. Adds ONLY the genuinely-missing admin contracts
    /// (aggregate dashboard, parent↔student relationship management, teacher↔class assignment
    /// management); every other admin page reuses the existing Phase 5 contracts. Derives from
    /// <see cref="CommunicationServiceBase"/> to reuse the trusted-tenant/identity resolution,
    /// role helpers and the same-tenant-or-404 user lookup. Tenant is always taken from the
    /// trusted token; cross-tenant ids resolve to 404 (no existence leak); duplicate active links
    /// are rejected with 409; every mutation stages an audit row in the same transaction.
    /// No metric on the dashboard is fabricated — an empty tenant returns zeros.
    /// </summary>
    public class SchoolAdminPortalService : CommunicationServiceBase, ISchoolAdminPortalService
    {
        private readonly ITenantSelfService _tenantSelf;
        private readonly IReportService _reports;
        private readonly IAiUsageService _aiUsage;

        public SchoolAdminPortalService(
            IUnitOfWork unitOfWork,
            ITenantContext tenant,
            IAuditWriter audit,
            UserManager<ApplicationUser> users,
            ITenantSelfService tenantSelf,
            IReportService reports,
            IAiUsageService aiUsage)
            : base(unitOfWork, tenant, audit, users)
        {
            _tenantSelf = tenantSelf;
            _reports = reports;
            _aiUsage = aiUsage;
        }

        // ---------------------------------------------------------------
        // Dashboard — real aggregate of the caller's OWN tenant.
        // ---------------------------------------------------------------
        public async Task<ApiResponse<SchoolAdminDashboardDto>> DashboardAsync(CancellationToken ct = default)
        {
            var tenantId = RequireTenant();

            var profile = (await _tenantSelf.CurrentTenantAsync(ct)).Data;
            var users = (await _reports.TenantUsersAsync(ct)).Data ?? new Dto.OperationsDto.TenantUsersReportDto();
            var ai = (await _aiUsage.SummaryAsync(ct)).Data;

            var dto = new SchoolAdminDashboardDto
            {
                TenantId = tenantId,
                TenantName = profile?.Name ?? string.Empty,
                TenantStatus = profile?.Status ?? TenantStatus.Active,
                TenantType = profile?.Type.ToString() ?? string.Empty,

                Students = users.Students,
                Teachers = users.Teachers,
                Parents = users.Parents,
                Admins = users.Admins,

                Grades = await CountAsync<Grade>(g => true),
                Subjects = await CountAsync<Subject>(s => true),
                Classes = await CountAsync<SchoolClass>(c => true),
                AcademicYears = await CountAsync<AcademicYear>(y => true),
                Terms = await CountAsync<Term>(t => true),

                ParentStudentLinks = await CountAsync<ParentStudentRelationship>(r => r.IsActive),
                TeacherClassAssignments = await CountAsync<TeacherClassAssignment>(a => a.IsActive),

                ActiveAnnouncements = await CountAsync<Announcement>(a => a.IsActive),
                OpenParentRequests = await CountAsync<ParentRequest>(r =>
                    r.Status == ParentRequestStatus.Open || r.Status == ParentRequestStatus.InProgress),
                OpenSupportRequests = await CountAsync<SupportRequest>(s => s.Status == RequestStatus.Pending),

                AiUsageRecords = ai?.Records ?? 0,
                AiTotalTokens = ai?.TotalTokens ?? 0,

                GeneratedAt = DateTime.UtcNow
            };

            return Ok(dto, 200, "Tenant summary retrieved.");
        }

        // ---------------------------------------------------------------
        // Parent ↔ student relationships
        // ---------------------------------------------------------------
        public async Task<ApiResponse<IEnumerable<SchoolAdminRelationshipDto>>> ListRelationshipsAsync(RelationshipParameters p, CancellationToken ct = default)
        {
            RequireTenant();

            var parentId = string.IsNullOrWhiteSpace(p.ParentId) ? null : p.ParentId;
            var studentId = string.IsNullOrWhiteSpace(p.StudentId) ? null : p.StudentId;

            var spec = new PagedSpecification<ParentStudentRelationship, string>(
                r => (parentId == null || r.ParentId == parentId)
                     && (studentId == null || r.StudentId == studentId)
                     && (!p.ActiveOnly || r.IsActive),
                r => r.ActiveFrom, p.PageNumber, p.PageSize, descending: true);

            var links = (await UnitOfWork.Repository<ParentStudentRelationship, string>().GetAllWithSpecAsync(spec)).ToList();
            if (links.Count == 0) return Ok<IEnumerable<SchoolAdminRelationshipDto>>(new List<SchoolAdminRelationshipDto>(), 200, "Records retrieved successfully.");

            var names = await NamesByIdAsync(links.SelectMany(l => new[] { l.ParentId, l.StudentId }), ct);
            var rows = links.Select(l => Map(l, names)).ToList();
            return Ok<IEnumerable<SchoolAdminRelationshipDto>>(rows, 200, "Records retrieved successfully.");
        }

        public async Task<ApiResponse<SchoolAdminRelationshipDto>> CreateRelationshipAsync(CreateRelationshipDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            if (string.IsNullOrWhiteSpace(dto.ParentId) || string.IsNullOrWhiteSpace(dto.StudentId))
                throw new BadRequestException("Both a parent and a student are required.");

            // Same-tenant + correct-role validation (404 on cross-tenant/unknown — no leak).
            var parent = await RequireTenantUserAsync(dto.ParentId, Roles.Parent, ct);
            var student = await RequireTenantUserAsync(dto.StudentId, Roles.Student, ct);

            var duplicate = await UnitOfWork.Repository<ParentStudentRelationship, string>().CountAsync(
                new CriteriaSpecification<ParentStudentRelationship, string>(r =>
                    r.ParentId == parent.Id && r.StudentId == student.Id && r.IsActive));
            if (duplicate > 0)
                throw new ConflictException("An active link between this parent and student already exists.");

            var link = new ParentStudentRelationship
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                ParentId = parent.Id,
                StudentId = student.Id,
                Relationship = dto.Relationship,
                IsPrimary = dto.IsPrimary,
                CanViewProgress = dto.CanViewProgress,
                CanRequestDocuments = dto.CanRequestDocuments,
                CanContactTeachers = dto.CanContactTeachers,
                IsActive = true,
                ActiveFrom = DateTime.UtcNow
            };

            await UnitOfWork.Repository<ParentStudentRelationship, string>().AddAsync(link);
            await Audit.StageAsync(AuditActionType.Create, nameof(ParentStudentRelationship), link.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);

            var names = new Dictionary<string, string>
            {
                [parent.Id] = parent.FullName ?? string.Empty,
                [student.Id] = student.FullName ?? string.Empty
            };
            return Ok(Map(link, names), 201, "Parent-student link created.");
        }

        public async Task<ApiResponse<SchoolAdminRelationshipDto>> DeactivateRelationshipAsync(string id, CancellationToken ct = default)
        {
            RequireTenant();
            var link = await UnitOfWork.Repository<ParentStudentRelationship, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<ParentStudentRelationship, string>(r => r.Id == id))
                ?? throw new NotFoundException("Relationship not found.");

            if (link.IsActive)
            {
                link.IsActive = false;
                link.ActiveTo = DateTime.UtcNow;
                await Audit.StageAsync(AuditActionType.Update, nameof(ParentStudentRelationship), link.Id, ct: ct);
                await UnitOfWork.SaveChangesAsync(ct);
            }

            var names = await NamesByIdAsync(new[] { link.ParentId, link.StudentId }, ct);
            return Ok(Map(link, names), 200, "Parent-student link deactivated.");
        }

        // ---------------------------------------------------------------
        // Teacher ↔ class assignments
        // ---------------------------------------------------------------
        public async Task<ApiResponse<IEnumerable<SchoolAdminTeacherClassAssignmentDto>>> ListClassAssignmentsAsync(TeacherClassAssignmentParameters p, CancellationToken ct = default)
        {
            RequireTenant();

            var teacherId = string.IsNullOrWhiteSpace(p.TeacherId) ? null : p.TeacherId;
            var classId = string.IsNullOrWhiteSpace(p.SchoolClassId) ? null : p.SchoolClassId;

            var spec = new PagedSpecification<TeacherClassAssignment, string>(
                a => (teacherId == null || a.TeacherId == teacherId)
                     && (classId == null || a.SchoolClassId == classId)
                     && (!p.ActiveOnly || a.IsActive),
                a => a.ActiveFrom, p.PageNumber, p.PageSize, descending: true);

            var rowsRaw = (await UnitOfWork.Repository<TeacherClassAssignment, string>().GetAllWithSpecAsync(spec)).ToList();
            if (rowsRaw.Count == 0) return Ok<IEnumerable<SchoolAdminTeacherClassAssignmentDto>>(new List<SchoolAdminTeacherClassAssignmentDto>(), 200, "Records retrieved successfully.");

            var teacherNames = await NamesByIdAsync(rowsRaw.Select(a => a.TeacherId), ct);
            var classNames = await ClassNamesByIdAsync(rowsRaw.Select(a => a.SchoolClassId));
            var rows = rowsRaw.Select(a => Map(a, teacherNames, classNames)).ToList();
            return Ok<IEnumerable<SchoolAdminTeacherClassAssignmentDto>>(rows, 200, "Records retrieved successfully.");
        }

        public async Task<ApiResponse<SchoolAdminTeacherClassAssignmentDto>> CreateClassAssignmentAsync(CreateTeacherClassAssignmentDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            if (string.IsNullOrWhiteSpace(dto.TeacherId) || string.IsNullOrWhiteSpace(dto.SchoolClassId))
                throw new BadRequestException("Both a teacher and a class are required.");

            var teacher = await RequireTenantUserAsync(dto.TeacherId, Roles.Teacher, ct);
            var schoolClass = await UnitOfWork.Repository<SchoolClass, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<SchoolClass, string>(c => c.Id == dto.SchoolClassId))
                ?? throw new NotFoundException("Class not found.");

            var duplicate = await UnitOfWork.Repository<TeacherClassAssignment, string>().CountAsync(
                new CriteriaSpecification<TeacherClassAssignment, string>(a =>
                    a.TeacherId == teacher.Id && a.SchoolClassId == schoolClass.Id && a.IsActive));
            if (duplicate > 0)
                throw new ConflictException("An active assignment between this teacher and class already exists.");

            var assignment = new TeacherClassAssignment
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                TeacherId = teacher.Id,
                SchoolClassId = schoolClass.Id,
                SubjectId = string.IsNullOrWhiteSpace(dto.SubjectId) ? null : dto.SubjectId,
                Role = dto.Role,
                IsActive = true,
                ActiveFrom = DateTime.UtcNow
            };

            await UnitOfWork.Repository<TeacherClassAssignment, string>().AddAsync(assignment);
            await Audit.StageAsync(AuditActionType.Create, nameof(TeacherClassAssignment), assignment.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);

            var teacherNames = new Dictionary<string, string> { [teacher.Id] = teacher.FullName ?? string.Empty };
            var classNames = new Dictionary<string, string> { [schoolClass.Id] = schoolClass.Name };
            return Ok(Map(assignment, teacherNames, classNames), 201, "Teacher-class assignment created.");
        }

        public async Task<ApiResponse<SchoolAdminTeacherClassAssignmentDto>> DeactivateClassAssignmentAsync(string id, CancellationToken ct = default)
        {
            RequireTenant();
            var assignment = await UnitOfWork.Repository<TeacherClassAssignment, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<TeacherClassAssignment, string>(a => a.Id == id))
                ?? throw new NotFoundException("Assignment not found.");

            if (assignment.IsActive)
            {
                assignment.IsActive = false;
                assignment.ActiveTo = DateTime.UtcNow;
                await Audit.StageAsync(AuditActionType.Update, nameof(TeacherClassAssignment), assignment.Id, ct: ct);
                await UnitOfWork.SaveChangesAsync(ct);
            }

            var teacherNames = await NamesByIdAsync(new[] { assignment.TeacherId }, ct);
            var classNames = await ClassNamesByIdAsync(new[] { assignment.SchoolClassId });
            return Ok(Map(assignment, teacherNames, classNames), 200, "Teacher-class assignment deactivated.");
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------
        private Task<int> CountAsync<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate) where T : Domain.Entities.Base.BaseEntity<string>
            => UnitOfWork.Repository<T, string>().CountAsync(new CriteriaSpecification<T, string>(predicate));

        private async Task<Dictionary<string, string>> NamesByIdAsync(IEnumerable<string> ids, CancellationToken ct)
        {
            var distinct = ids.Where(i => !string.IsNullOrEmpty(i)).Distinct().ToList();
            if (distinct.Count == 0) return new Dictionary<string, string>();
            var users = await Users.Users.Where(u => distinct.Contains(u.Id)).ToListAsync(ct);
            return users.ToDictionary(u => u.Id, u => u.FullName ?? string.Empty);
        }

        private async Task<Dictionary<string, string>> ClassNamesByIdAsync(IEnumerable<string> ids)
        {
            var distinct = ids.Where(i => !string.IsNullOrEmpty(i)).Distinct().ToList();
            if (distinct.Count == 0) return new Dictionary<string, string>();
            var classes = await UnitOfWork.Repository<SchoolClass, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<SchoolClass, string>(c => distinct.Contains(c.Id)));
            return classes.ToDictionary(c => c.Id, c => c.Name);
        }

        private static SchoolAdminRelationshipDto Map(ParentStudentRelationship l, IReadOnlyDictionary<string, string> names) => new()
        {
            Id = l.Id,
            ParentId = l.ParentId,
            ParentName = names.TryGetValue(l.ParentId, out var pn) ? pn : string.Empty,
            StudentId = l.StudentId,
            StudentName = names.TryGetValue(l.StudentId, out var sn) ? sn : string.Empty,
            Relationship = l.Relationship.ToString(),
            IsPrimary = l.IsPrimary,
            CanViewProgress = l.CanViewProgress,
            CanRequestDocuments = l.CanRequestDocuments,
            CanContactTeachers = l.CanContactTeachers,
            IsActive = l.IsActive,
            ActiveFrom = l.ActiveFrom,
            ActiveTo = l.ActiveTo
        };

        private static SchoolAdminTeacherClassAssignmentDto Map(TeacherClassAssignment a,
            IReadOnlyDictionary<string, string> teacherNames, IReadOnlyDictionary<string, string> classNames) => new()
        {
            Id = a.Id,
            TeacherId = a.TeacherId,
            TeacherName = teacherNames.TryGetValue(a.TeacherId, out var tn) ? tn : string.Empty,
            SchoolClassId = a.SchoolClassId,
            SchoolClassName = classNames.TryGetValue(a.SchoolClassId, out var cn) ? cn : string.Empty,
            SubjectId = a.SubjectId,
            Role = a.Role.ToString(),
            IsActive = a.IsActive,
            ActiveFrom = a.ActiveFrom,
            ActiveTo = a.ActiveTo
        };
    }
}
