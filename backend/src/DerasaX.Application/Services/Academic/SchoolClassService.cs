using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AcademicDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Academic;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.Extensions.Logging;

namespace DerasaX.Application.Services.Academic
{
    public class SchoolClassService : AcademicServiceBase, ISchoolClassService
    {
        private readonly ILogger<SchoolClassService> _logger;
        private readonly IPlanLimitEnforcer _limits;

        public SchoolClassService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit,
            ILogger<SchoolClassService> logger, IPlanLimitEnforcer limits) : base(unitOfWork, tenant, audit)
        {
            _logger = logger;
            _limits = limits;
        }

        public async Task<PaginationResponse<IEnumerable<SchoolClassDto>>> ListAsync(SchoolClassParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            var search = p.Search?.Trim();
            System.Linq.Expressions.Expression<Func<SchoolClass, bool>> criteria = c =>
                (string.IsNullOrEmpty(p.GradeId) || c.GradeId == p.GradeId) &&
                (string.IsNullOrEmpty(p.AcademicYearId) || c.AcademicYearId == p.AcademicYearId) &&
                (string.IsNullOrEmpty(search) || c.Name.Contains(search) || c.Code.Contains(search));

            var repo = UnitOfWork.Repository<SchoolClass, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<SchoolClass, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<SchoolClass, string>(criteria, c => c.Name, p.PageNumber, p.PageSize));

            var dto = items.Select(c => Map(c, enrolledCount: 0)).ToList();
            return new PaginationResponse<IEnumerable<SchoolClassDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Classes retrieved successfully." };
        }

        public async Task<ApiResponse<SchoolClassDto>> GetByIdAsync(string id, CancellationToken ct = default)
        {
            RequireTenant();
            var entity = await LoadAsync(id);
            var enrolled = await UnitOfWork.Repository<Enrollment, string>()
                .CountAsync(new CriteriaSpecification<Enrollment, string>(
                    e => e.SchoolClassId == id && e.Status == EnrollmentStatus.Active));
            return Ok(Map(entity, enrolled), 200, "Class retrieved successfully.");
        }

        public async Task<ApiResponse<SchoolClassDto>> CreateAsync(AddSchoolClassDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            ValidateInput(dto.Name, dto.Code, dto.Capacity);

            _ = await UnitOfWork.Repository<Grade, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<Grade, string>(g => g.Id == dto.GradeId))
                ?? throw new NotFoundException("Grade not found.");
            _ = await UnitOfWork.Repository<AcademicYear, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<AcademicYear, string>(y => y.Id == dto.AcademicYearId))
                ?? throw new NotFoundException("Academic year not found.");

            await EnsureCodeUniqueAsync(dto.AcademicYearId, dto.Code, excludeId: null);
            await _limits.EnsureCanAddClassAsync(tenantId, ct);

            var entity = new SchoolClass
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                Name = dto.Name.Trim(),
                Code = dto.Code.Trim(),
                Capacity = dto.Capacity,
                GradeId = dto.GradeId,
                AcademicYearId = dto.AcademicYearId
            };

            await UnitOfWork.Repository<SchoolClass, string>().AddAsync(entity);
            await Audit.StageAsync(AuditActionType.Create, nameof(SchoolClass), entity.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("SchoolClass {Id} created for tenant {Tenant}", entity.Id, tenantId);
            return Ok(Map(entity, 0), 201, "Class created successfully.");
        }

        public async Task<ApiResponse<SchoolClassDto>> UpdateAsync(UpdateSchoolClassDto dto, CancellationToken ct = default)
        {
            RequireTenant();
            ValidateInput(dto.Name, dto.Code, dto.Capacity);
            var entity = await LoadAsync(dto.Id);
            await EnsureCodeUniqueAsync(entity.AcademicYearId, dto.Code, excludeId: entity.Id);

            entity.Name = dto.Name.Trim();
            entity.Code = dto.Code.Trim();
            entity.Capacity = dto.Capacity;

            await Audit.StageAsync(AuditActionType.Update, nameof(SchoolClass), entity.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(entity, 0), 200, "Class updated successfully.");
        }

        public async Task<ApiResponse<bool>> ArchiveAsync(string id, CancellationToken ct = default)
        {
            RequireTenant();
            var entity = await LoadAsync(id);

            // Block archival while active enrollments exist — prevents orphaning students.
            var active = await UnitOfWork.Repository<Enrollment, string>()
                .CountAsync(new CriteriaSpecification<Enrollment, string>(
                    e => e.SchoolClassId == id && e.Status == EnrollmentStatus.Active));
            if (active > 0)
                throw new ConflictException("Cannot archive a class that still has active enrollments.");

            entity.IsDeleted = true;
            await Audit.StageAsync(AuditActionType.Delete, nameof(SchoolClass), entity.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return new ApiResponse<bool>(true) { Success = true, StatusCode = 200, Message = "Class archived successfully." };
        }

        private async Task<SchoolClass> LoadAsync(string id) =>
            await UnitOfWork.Repository<SchoolClass, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<SchoolClass, string>(c => c.Id == id))
            ?? throw new NotFoundException("Class not found.");

        private async Task EnsureCodeUniqueAsync(string yearId, string code, string? excludeId)
        {
            var normalized = code.Trim();
            var count = await UnitOfWork.Repository<SchoolClass, string>()
                .CountAsync(new CriteriaSpecification<SchoolClass, string>(
                    c => c.AcademicYearId == yearId && c.Code == normalized && (excludeId == null || c.Id != excludeId)));
            if (count > 0)
                throw new ConflictException($"A class with code '{normalized}' already exists in this academic year.");
        }

        private static void ValidateInput(string name, string code, int? capacity)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new BadRequestException("Name is required.");
            if (name.Trim().Length > 128) throw new BadRequestException("Name must be 128 characters or fewer.");
            if (string.IsNullOrWhiteSpace(code)) throw new BadRequestException("Code is required.");
            if (code.Trim().Length > 64) throw new BadRequestException("Code must be 64 characters or fewer.");
            if (capacity is < 1 or > 1000) throw new BadRequestException("Capacity must be between 1 and 1000.");
        }

        private static ApiResponse<SchoolClassDto> Ok(SchoolClassDto data, int status, string message) =>
            new(data) { Success = true, StatusCode = status, Message = message };

        private static SchoolClassDto Map(SchoolClass c, int enrolledCount) => new()
        {
            Id = c.Id, Name = c.Name, Code = c.Code, Capacity = c.Capacity, GradeId = c.GradeId,
            AcademicYearId = c.AcademicYearId, EnrolledCount = enrolledCount, CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt
        };
    }
}
