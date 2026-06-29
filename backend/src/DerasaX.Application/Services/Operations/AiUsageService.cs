using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;

namespace DerasaX.Application.Services.Operations
{
    /// <summary>
    /// AI usage recording + reporting. This is the persistence contract Phase 6 orchestration will
    /// call to record each AI call's cost/tokens/latency/failure — Phase 5 does NOT execute any AI.
    /// Failure/latency metadata is captured in the audit metadata (no schema change); cost/token
    /// usage is persisted on the usage record for tenant-scoped reporting and plan-limit visibility.
    /// </summary>
    public class AiUsageService : OperationsServiceBase, IAiUsageService
    {
        private readonly IPlatformRepository<SubscriptionPlanDefinition> _plans;

        public AiUsageService(IUnitOfWork uow, ITenantContext tenant, IAuditWriter audit,
            IPlatformRepository<SubscriptionPlanDefinition> plans) : base(uow, tenant, audit)
        {
            _plans = plans;
        }

        public async Task<ApiResponse<AiUsageDto>> RecordAsync(RecordAiUsageDto dto, CancellationToken ct = default)
        {
            if (!IsSchoolAdmin) throw new ForbiddenException("Only a school administrator (or the internal AI orchestrator) may record AI usage.");
            var record = await PersistAsync(dto, ct);
            return Ok(Map(record), 201, "AI usage recorded.");
        }

        public async Task<AiUsageDto> RecordInternalAsync(RecordAiUsageDto dto, CancellationToken ct = default)
        {
            // Trusted server-side orchestration path: no SchoolAdmin gate, still
            // strictly tenant-scoped (RequireTenant). Used by the AI tutor/quiz/etc.
            var record = await PersistAsync(dto, ct);
            return Map(record);
        }

        private async Task<AiUsageRecord> PersistAsync(RecordAiUsageDto dto, CancellationToken ct)
        {
            var tenantId = RequireTenant();
            if (string.IsNullOrWhiteSpace(dto.Provider)) throw new BadRequestException("Provider is required.");

            var record = new AiUsageRecord
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, UserId = Tenant.UserId, Kind = dto.Kind,
                Provider = dto.Provider, Model = dto.Model, PromptTokens = dto.PromptTokens,
                CompletionTokens = dto.CompletionTokens, TotalTokens = dto.TotalTokens, Cost = dto.Cost,
                CorrelationId = dto.CorrelationId, UsedAt = DateTime.UtcNow
            };
            await UnitOfWork.Repository<AiUsageRecord, string>().AddAsync(record);
            // Failure + latency metadata captured in the audit trail (no schema change in Phase 5).
            await Audit.StageAsync(AuditActionType.System, nameof(AiUsageRecord), record.Id,
                $"{{\"failed\":{dto.Failed.ToString().ToLowerInvariant()},\"latencyMs\":{dto.LatencyMs ?? 0}}}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return record;
        }

        public async Task<PaginationResponse<IEnumerable<AiUsageDto>>> ListAsync(AiUsageParameters p, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            DateTime? from = p.From.HasValue ? AsUtc(p.From.Value) : null;
            DateTime? to = p.To.HasValue ? AsUtc(p.To.Value) : null;
            if (from.HasValue && to.HasValue && to < from) throw new BadRequestException("'To' must be on or after 'From'.");

            Expression<Func<AiUsageRecord, bool>> criteria = a =>
                (!p.Kind.HasValue || a.Kind == p.Kind.Value) &&
                (string.IsNullOrEmpty(p.Provider) || a.Provider == p.Provider) &&
                (from == null || a.UsedAt >= from) && (to == null || a.UsedAt <= to);
            var repo = UnitOfWork.Repository<AiUsageRecord, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<AiUsageRecord, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<AiUsageRecord, string>(criteria, a => a.UsedAt, p.PageNumber, p.PageSize, descending: true));
            return new PaginationResponse<IEnumerable<AiUsageDto>>(items.Select(Map).ToList(), total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "AI usage retrieved." };
        }

        public async Task<ApiResponse<AiUsageSummaryDto>> SummaryAsync(CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var records = await UnitOfWork.Repository<AiUsageRecord, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<AiUsageRecord, string>(a => true));
            var list = records.ToList();

            var subs = await UnitOfWork.Repository<TenantSubscription, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<TenantSubscription, string>(s => true));
            var latest = subs.OrderByDescending(s => s.StartsAt).FirstOrDefault();
            var plan = latest is null ? null : await _plans.FirstOrDefaultAsync(p => p.Id == latest.PlanDefinitionId, ct);

            var tokens = list.Sum(a => a.TotalTokens ?? 0);
            return Ok(new AiUsageSummaryDto
            {
                TenantId = tenantId, Records = list.Count, TotalTokens = tokens,
                TotalCost = list.Sum(a => a.Cost ?? 0m),
                MonthlyLimit = plan?.MaxAiGenerationsPerMonth,
                OverLimit = plan?.MaxAiGenerationsPerMonth is int max && list.Count > max
            });
        }

        private static DateTime AsUtc(DateTime v) => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc);

        private static AiUsageDto Map(AiUsageRecord a) => new()
        {
            Id = a.Id, Kind = a.Kind, Provider = a.Provider, Model = a.Model, TotalTokens = a.TotalTokens, Cost = a.Cost, UsedAt = a.UsedAt
        };
    }
}
