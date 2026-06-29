using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using Microsoft.Extensions.Logging;

namespace DerasaX.Application.Services.Academic
{
    public class AcademicYearService : AcademicServiceBase, IAcademicYearService
    {
        private readonly ILogger<AcademicYearService> _logger;

        public AcademicYearService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit,
            ILogger<AcademicYearService> logger) : base(unitOfWork, tenant, audit)
        {
            _logger = logger;
        }

        public async Task<PaginationResponse<IEnumerable<AcademicYearDto>>> ListAsync(AcademicYearParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            var search = p.Search?.Trim();

            System.Linq.Expressions.Expression<Func<AcademicYear, bool>> criteria = y =>
                (!p.IsCurrent.HasValue || y.IsCurrent == p.IsCurrent.Value) &&
                (string.IsNullOrEmpty(search) || y.Name.Contains(search) || y.Code.Contains(search));

            var repo = UnitOfWork.Repository<AcademicYear, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<AcademicYear, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<AcademicYear, string>(criteria, y => y.StartDate, p.PageNumber, p.PageSize, descending: true));

            var dto = items.Select(Map).ToList();
            return new PaginationResponse<IEnumerable<AcademicYearDto>>(dto, total, p.PageNumber, p.PageSize)
            {
                Success = true,
                StatusCode = 200,
                Message = "Academic years retrieved successfully."
            };
        }

        public async Task<ApiResponse<AcademicYearDto>> GetByIdAsync(string id, CancellationToken ct = default)
        {
            RequireTenant();
            var entity = await LoadAsync(id);
            return Ok(Map(entity), 200, "Academic year retrieved successfully.");
        }

        public async Task<ApiResponse<AcademicYearDto>> CreateAsync(AddAcademicYearDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            ValidateInput(dto.Name, dto.Code, dto.StartDate, dto.EndDate);
            await EnsureCodeUniqueAsync(dto.Code, excludeId: null);

            var entity = new AcademicYear
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                Name = dto.Name.Trim(),
                Code = dto.Code.Trim(),
                StartDate = AsUtc(dto.StartDate),
                EndDate = AsUtc(dto.EndDate),
                IsCurrent = dto.IsCurrent
            };

            await UnitOfWork.Repository<AcademicYear, string>().AddAsync(entity);
            await Audit.StageAsync(AuditActionType.Create, nameof(AcademicYear), entity.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("AcademicYear {Id} created for tenant {Tenant}", entity.Id, tenantId);
            return Ok(Map(entity), 201, "Academic year created successfully.");
        }

        public async Task<ApiResponse<AcademicYearDto>> UpdateAsync(UpdateAcademicYearDto dto, CancellationToken ct = default)
        {
            RequireTenant();
            ValidateInput(dto.Name, dto.Code, dto.StartDate, dto.EndDate);
            var entity = await LoadAsync(dto.Id);
            await EnsureCodeUniqueAsync(dto.Code, excludeId: entity.Id);

            entity.Name = dto.Name.Trim();
            entity.Code = dto.Code.Trim();
            entity.StartDate = AsUtc(dto.StartDate);
            entity.EndDate = AsUtc(dto.EndDate);
            entity.IsCurrent = dto.IsCurrent;

            await Audit.StageAsync(AuditActionType.Update, nameof(AcademicYear), entity.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(entity), 200, "Academic year updated successfully.");
        }

        public async Task<ApiResponse<bool>> ArchiveAsync(string id, CancellationToken ct = default)
        {
            RequireTenant();
            var entity = await LoadAsync(id);
            entity.IsDeleted = true; // soft-delete: hidden by global filter, retained for audit/history
            await Audit.StageAsync(AuditActionType.Delete, nameof(AcademicYear), entity.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return new ApiResponse<bool>(true) { Success = true, StatusCode = 200, Message = "Academic year archived successfully." };
        }

        // --- helpers ---

        private async Task<AcademicYear> LoadAsync(string id)
        {
            var entity = await UnitOfWork.Repository<AcademicYear, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<AcademicYear, string>(y => y.Id == id));
            // 404 (not 403) so cross-tenant existence is never revealed: the global tenant
            // filter means another tenant's year simply isn't found here.
            return entity ?? throw new NotFoundException("Academic year not found.");
        }

        private async Task EnsureCodeUniqueAsync(string code, string? excludeId)
        {
            var normalized = code.Trim();
            var count = await UnitOfWork.Repository<AcademicYear, string>()
                .CountAsync(new CriteriaSpecification<AcademicYear, string>(
                    y => y.Code == normalized && (excludeId == null || y.Id != excludeId)));
            if (count > 0)
                throw new ConflictException($"An academic year with code '{normalized}' already exists.");
        }

        private static void ValidateInput(string name, string code, DateTime start, DateTime end)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new BadRequestException("Name is required.");
            if (name.Trim().Length > 128) throw new BadRequestException("Name must be 128 characters or fewer.");
            if (string.IsNullOrWhiteSpace(code)) throw new BadRequestException("Code is required.");
            if (code.Trim().Length > 64) throw new BadRequestException("Code must be 64 characters or fewer.");
            if (end <= start) throw new BadRequestException("EndDate must be after StartDate.");
        }

        private static ApiResponse<AcademicYearDto> Ok(AcademicYearDto data, int status, string message) =>
            new(data) { Success = true, StatusCode = status, Message = message };

        private static AcademicYearDto Map(AcademicYear y) => new()
        {
            Id = y.Id, Name = y.Name, Code = y.Code, StartDate = y.StartDate, EndDate = y.EndDate,
            IsCurrent = y.IsCurrent, CreatedAt = y.CreatedAt, UpdatedAt = y.UpdatedAt
        };
    }
}
