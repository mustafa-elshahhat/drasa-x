using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.AcademicDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Academic;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DerasaX.Application.Services.Academic
{
    public class EnrollmentService : AcademicServiceBase, IEnrollmentService
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly ILogger<EnrollmentService> _logger;

        public EnrollmentService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit,
            UserManager<ApplicationUser> users, ILogger<EnrollmentService> logger) : base(unitOfWork, tenant, audit)
        {
            _users = users;
            _logger = logger;
        }

        public async Task<PaginationResponse<IEnumerable<EnrollmentDto>>> ListAsync(EnrollmentParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            System.Linq.Expressions.Expression<Func<Enrollment, bool>> criteria = e =>
                (string.IsNullOrEmpty(p.SchoolClassId) || e.SchoolClassId == p.SchoolClassId) &&
                (string.IsNullOrEmpty(p.StudentId) || e.StudentId == p.StudentId) &&
                (!p.Status.HasValue || e.Status == p.Status.Value);

            var repo = UnitOfWork.Repository<Enrollment, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<Enrollment, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<Enrollment, string>(criteria, e => e.EnrolledAt, p.PageNumber, p.PageSize, descending: true));

            var dto = items.Select(Map).ToList();
            return new PaginationResponse<IEnumerable<EnrollmentDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Enrollments retrieved successfully." };
        }

        public async Task<ApiResponse<EnrollmentDto>> EnrollAsync(AddEnrollmentDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            if (string.IsNullOrWhiteSpace(dto.StudentId)) throw new BadRequestException("StudentId is required.");
            if (string.IsNullOrWhiteSpace(dto.SchoolClassId)) throw new BadRequestException("SchoolClassId is required.");

            // Same-tenant class integrity (global filter scopes to caller's tenant).
            var schoolClass = await UnitOfWork.Repository<SchoolClass, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<SchoolClass, string>(c => c.Id == dto.SchoolClassId))
                ?? throw new NotFoundException("Class not found.");

            // Same-tenant student integrity. ApplicationUser has no global tenant filter
            // (its tenant is nullable for platform admins), so membership is checked
            // explicitly here — a cross-tenant student id resolves to 404, never leaking
            // that the student exists in another tenant. The DB trigger is the backstop.
            var student = await _users.Users.FirstOrDefaultAsync(u => u.Id == dto.StudentId, ct);
            if (student is null || student.TenantId != tenantId || student.IsDeleted || student is not Student)
                throw new NotFoundException("Student not found.");

            // Idempotency / duplicate protection: one active enrollment per student+class+year.
            var existing = await UnitOfWork.Repository<Enrollment, string>()
                .CountAsync(new CriteriaSpecification<Enrollment, string>(e =>
                    e.StudentId == dto.StudentId && e.SchoolClassId == dto.SchoolClassId &&
                    e.AcademicYearId == schoolClass.AcademicYearId && e.Status == EnrollmentStatus.Active));
            if (existing > 0)
                throw new ConflictException("Student is already actively enrolled in this class for the academic year.");

            // Capacity enforcement.
            if (schoolClass.Capacity is int cap)
            {
                var current = await UnitOfWork.Repository<Enrollment, string>()
                    .CountAsync(new CriteriaSpecification<Enrollment, string>(e =>
                        e.SchoolClassId == dto.SchoolClassId && e.Status == EnrollmentStatus.Active));
                if (current >= cap)
                    throw new ConflictException("Class capacity has been reached.");
            }

            var entity = new Enrollment
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                StudentId = dto.StudentId,
                SchoolClassId = dto.SchoolClassId,
                AcademicYearId = schoolClass.AcademicYearId,
                Status = EnrollmentStatus.Active,
                EnrolledAt = DateTime.UtcNow
            };

            await UnitOfWork.Repository<Enrollment, string>().AddAsync(entity);
            await Audit.StageAsync(AuditActionType.Create, nameof(Enrollment), entity.Id, ct: ct);
            await StageNotificationAsync(tenantId, dto.StudentId, "Enrolled in a class",
                $"You have been enrolled in {schoolClass.Name}.");
            // Single SaveChanges => enrollment, audit and notification commit atomically.
            await UnitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Enrollment {Id} created (student {Student}, class {Class})", entity.Id, dto.StudentId, dto.SchoolClassId);
            return Ok(Map(entity), 201, "Student enrolled successfully.");
        }

        public async Task<ApiResponse<EnrollmentDto>> WithdrawAsync(WithdrawEnrollmentDto dto, CancellationToken ct = default)
        {
            RequireTenant();
            var entity = await UnitOfWork.Repository<Enrollment, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<Enrollment, string>(e => e.Id == dto.Id))
                ?? throw new NotFoundException("Enrollment not found.");

            if (entity.Status != EnrollmentStatus.Active)
                throw new ConflictException("Only an active enrollment can be withdrawn.");

            entity.Status = EnrollmentStatus.Withdrawn;
            entity.WithdrawnAt = DateTime.UtcNow;
            entity.WithdrawalReason = dto.Reason;

            await Audit.StageAsync(AuditActionType.Update, nameof(Enrollment), entity.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(entity), 200, "Enrollment withdrawn successfully.");
        }

        // Phase 13 — preference-aware staging with honest delivery-state, via the shared helper.
        private Task StageNotificationAsync(string tenantId, string userId, string title, string body) =>
            NotificationStaging.StageAsync(UnitOfWork, tenantId, userId, title, body,
                NotificationCategory.Informational, NotificationType.System, actorUserId: Tenant.UserId);

        private static ApiResponse<EnrollmentDto> Ok(EnrollmentDto data, int status, string message) =>
            new(data) { Success = true, StatusCode = status, Message = message };

        private static EnrollmentDto Map(Enrollment e) => new()
        {
            Id = e.Id, StudentId = e.StudentId, SchoolClassId = e.SchoolClassId, AcademicYearId = e.AcademicYearId,
            Status = e.Status, EnrolledAt = e.EnrolledAt, WithdrawnAt = e.WithdrawnAt, WithdrawalReason = e.WithdrawalReason
        };
    }
}
