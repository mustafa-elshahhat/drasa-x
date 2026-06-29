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
    public class TermService : AcademicServiceBase, ITermService
    {
        private readonly ILogger<TermService> _logger;

        public TermService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit,
            ILogger<TermService> logger) : base(unitOfWork, tenant, audit)
        {
            _logger = logger;
        }

        public async Task<PaginationResponse<IEnumerable<TermDto>>> ListAsync(TermParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            var search = p.Search?.Trim();
            System.Linq.Expressions.Expression<Func<Term, bool>> criteria = t =>
                (string.IsNullOrEmpty(p.AcademicYearId) || t.AcademicYearId == p.AcademicYearId) &&
                (string.IsNullOrEmpty(search) || t.Name.Contains(search) || t.Code.Contains(search));

            var repo = UnitOfWork.Repository<Term, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<Term, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<Term, string>(criteria, t => t.Order, p.PageNumber, p.PageSize));

            var dto = items.Select(Map).ToList();
            return new PaginationResponse<IEnumerable<TermDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Terms retrieved successfully." };
        }

        public async Task<ApiResponse<TermDto>> GetByIdAsync(string id, CancellationToken ct = default)
        {
            RequireTenant();
            return Ok(Map(await LoadAsync(id)), 200, "Term retrieved successfully.");
        }

        public async Task<ApiResponse<TermDto>> CreateAsync(AddTermDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            ValidateInput(dto.Name, dto.Code, dto.StartDate, dto.EndDate);

            // Same-tenant parent integrity: the year must exist within this tenant. The
            // global filter means a cross-tenant year id resolves to "not found".
            var year = await UnitOfWork.Repository<AcademicYear, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<AcademicYear, string>(y => y.Id == dto.AcademicYearId))
                ?? throw new NotFoundException("Academic year not found.");

            var start = AsUtc(dto.StartDate);
            var end = AsUtc(dto.EndDate);
            if (start < year.StartDate || end > year.EndDate.AddDays(1))
                throw new BadRequestException("Term dates must fall within the academic year.");

            await EnsureCodeUniqueAsync(dto.AcademicYearId, dto.Code, excludeId: null);

            var entity = new Term
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                Name = dto.Name.Trim(),
                Code = dto.Code.Trim(),
                Order = dto.Order,
                StartDate = start,
                EndDate = end,
                AcademicYearId = dto.AcademicYearId
            };

            await UnitOfWork.Repository<Term, string>().AddAsync(entity);
            await Audit.StageAsync(AuditActionType.Create, nameof(Term), entity.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Term {Id} created for tenant {Tenant}", entity.Id, tenantId);
            return Ok(Map(entity), 201, "Term created successfully.");
        }

        public async Task<ApiResponse<TermDto>> UpdateAsync(UpdateTermDto dto, CancellationToken ct = default)
        {
            RequireTenant();
            ValidateInput(dto.Name, dto.Code, dto.StartDate, dto.EndDate);
            var entity = await LoadAsync(dto.Id);
            await EnsureCodeUniqueAsync(entity.AcademicYearId, dto.Code, excludeId: entity.Id);

            entity.Name = dto.Name.Trim();
            entity.Code = dto.Code.Trim();
            entity.Order = dto.Order;
            entity.StartDate = AsUtc(dto.StartDate);
            entity.EndDate = AsUtc(dto.EndDate);

            await Audit.StageAsync(AuditActionType.Update, nameof(Term), entity.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(entity), 200, "Term updated successfully.");
        }

        public async Task<ApiResponse<bool>> ArchiveAsync(string id, CancellationToken ct = default)
        {
            RequireTenant();
            var entity = await LoadAsync(id);
            entity.IsDeleted = true;
            await Audit.StageAsync(AuditActionType.Delete, nameof(Term), entity.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return new ApiResponse<bool>(true) { Success = true, StatusCode = 200, Message = "Term archived successfully." };
        }

        private async Task<Term> LoadAsync(string id) =>
            await UnitOfWork.Repository<Term, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<Term, string>(t => t.Id == id))
            ?? throw new NotFoundException("Term not found.");

        private async Task EnsureCodeUniqueAsync(string yearId, string code, string? excludeId)
        {
            var normalized = code.Trim();
            var count = await UnitOfWork.Repository<Term, string>()
                .CountAsync(new CriteriaSpecification<Term, string>(
                    t => t.AcademicYearId == yearId && t.Code == normalized && (excludeId == null || t.Id != excludeId)));
            if (count > 0)
                throw new ConflictException($"A term with code '{normalized}' already exists in this academic year.");
        }

        private static void ValidateInput(string name, string code, DateTime start, DateTime end)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new BadRequestException("Name is required.");
            if (name.Trim().Length > 128) throw new BadRequestException("Name must be 128 characters or fewer.");
            if (string.IsNullOrWhiteSpace(code)) throw new BadRequestException("Code is required.");
            if (code.Trim().Length > 64) throw new BadRequestException("Code must be 64 characters or fewer.");
            if (end <= start) throw new BadRequestException("EndDate must be after StartDate.");
        }

        private static ApiResponse<TermDto> Ok(TermDto data, int status, string message) =>
            new(data) { Success = true, StatusCode = status, Message = message };

        private static TermDto Map(Term t) => new()
        {
            Id = t.Id, Name = t.Name, Code = t.Code, Order = t.Order, StartDate = t.StartDate, EndDate = t.EndDate,
            AcademicYearId = t.AcademicYearId, CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt
        };
    }
}
