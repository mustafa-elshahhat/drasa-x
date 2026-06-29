using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.Operations
{
    /// <summary>SystemAdmin (platform-scope) administration of tenants and subscriptions.</summary>
    public class TenantAdminService : OperationsServiceBase, ITenantAdminService
    {
        private readonly IPlatformRepository<Tenant> _tenants;
        private readonly IPlatformRepository<SubscriptionPlanDefinition> _plans;
        private readonly UserManager<ApplicationUser> _users;

        public TenantAdminService(IUnitOfWork uow, ITenantContext tenant, IAuditWriter audit,
            IPlatformRepository<Tenant> tenants, IPlatformRepository<SubscriptionPlanDefinition> plans,
            UserManager<ApplicationUser> users) : base(uow, tenant, audit)
        {
            _tenants = tenants;
            _plans = plans;
            _users = users;
        }

        public async Task<PaginationResponse<IEnumerable<TenantDto>>> ListTenantsAsync(TenantParameters p, CancellationToken ct = default)
        {
            var all = await _tenants.ListAsync(t => !p.Status.HasValue || t.Status == p.Status.Value, ct);
            var ordered = all.OrderBy(t => t.Id).ToList();
            var page = ordered.Skip((p.PageNumber - 1) * p.PageSize).Take(p.PageSize).Select(Map).ToList();
            return new PaginationResponse<IEnumerable<TenantDto>>(page, ordered.Count, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Tenants retrieved." };
        }

        public async Task<ApiResponse<TenantDto>> GetTenantAsync(string id, CancellationToken ct = default)
        {
            var tenant = await _tenants.FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException("Tenant not found.");
            return Ok(Map(tenant));
        }

        public async Task<ApiResponse<TenantDto>> CreateTenantAsync(CreateTenantDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.Name))
                throw new BadRequestException("Id and Name are required.");
            if (await _tenants.FirstOrDefaultAsync(t => t.Id == dto.Id, ct) is not null)
                throw new ConflictException("A tenant with this id already exists.");

            var tenant = new Tenant { Id = dto.Id, Name = dto.Name, Domain = dto.Domain ?? string.Empty, Type = dto.Type, Status = TenantStatus.Active };
            await _tenants.AddAsync(tenant, ct);
            // Persist the tenant first so the audit row's tenant FK is satisfiable, then audit it.
            await UnitOfWork.SaveChangesAsync(ct);
            await Audit.StageAsync(AuditActionType.Create, nameof(Tenant), tenant.Id, ct: ct, tenantOverride: tenant.Id);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(tenant), 201, "Tenant created.");
        }

        public async Task<ApiResponse<TenantDto>> SetStatusAsync(string id, TenantStatus status, CancellationToken ct = default)
        {
            var tenant = await _tenants.FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException("Tenant not found.");
            tenant.Status = status; // integrates with the Phase 3 login/tenant gate (only Active tenants may access)
            _tenants.Update(tenant);
            await Audit.StageAsync(AuditActionType.Update, nameof(Tenant), tenant.Id, $"{{\"status\":\"{status}\"}}", ct, tenantOverride: tenant.Id);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(tenant), 200, $"Tenant {status}.");
        }

        public async Task<ApiResponse<IEnumerable<PlanDto>>> ListPlansAsync(CancellationToken ct = default)
        {
            var plans = await _plans.ListAsync(p => true, ct);
            return Ok<IEnumerable<PlanDto>>(plans.Select(MapPlan).ToList());
        }

        public async Task<ApiResponse<SubscriptionDto>> AssignPlanAsync(AssignPlanDto dto, CancellationToken ct = default)
        {
            var tenant = await _tenants.FirstOrDefaultAsync(t => t.Id == dto.TenantId, ct) ?? throw new NotFoundException("Tenant not found.");
            var plan = await _plans.FirstOrDefaultAsync(p => p.Id == dto.PlanDefinitionId, ct) ?? throw new NotFoundException("Plan not found.");

            var subscription = new TenantSubscription
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenant.Id,
                PlanDefinitionId = plan.Id,
                Status = dto.IsTrial ? SubscriptionStatus.Trial : SubscriptionStatus.Active,
                StartsAt = DateTime.UtcNow,
                ExpiresAt = AsUtc(dto.ExpiresAt),
                IsTrial = dto.IsTrial,
                AutoRenew = true
            };
            await UnitOfWork.Repository<TenantSubscription, string>().AddAsync(subscription);
            await Audit.StageAsync(AuditActionType.Create, nameof(TenantSubscription), subscription.Id, $"{{\"tenant\":\"{tenant.Id}\"}}", ct, tenantOverride: tenant.Id);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(MapSub(subscription), 201, "Plan assigned.");
        }

        public async Task<ApiResponse<SubscriptionDto>> GetSubscriptionAsync(string tenantId, CancellationToken ct = default)
        {
            var sub = await LatestSubscription(tenantId) ?? throw new NotFoundException("No subscription found for this tenant.");
            return Ok(MapSub(sub));
        }

        public async Task<ApiResponse<RenewalDto>> ProcessRenewalAsync(string renewalId, ProcessRenewalDto dto, CancellationToken ct = default)
        {
            var renewal = await UnitOfWork.Repository<SubscriptionRenewal, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<SubscriptionRenewal, string>(r => r.Id == renewalId))
                ?? throw new NotFoundException("Renewal not found.");
            if (renewal.Status != RenewalStatus.Requested)
                throw new ConflictException("Only a requested renewal can be processed.");

            renewal.Status = dto.Status;
            renewal.ProcessedAt = DateTime.UtcNow;
            renewal.NewExpiresAt = AsUtc(dto.NewExpiresAt);
            renewal.Notes = dto.Notes;
            UnitOfWork.Repository<SubscriptionRenewal, string>().Update(renewal);

            if (dto.Status == RenewalStatus.Applied && renewal.NewExpiresAt is { } newExpiry)
            {
                var sub = await UnitOfWork.Repository<TenantSubscription, string>().GetByIdWithSpecAsync(
                    new CriteriaSpecification<TenantSubscription, string>(s => s.Id == renewal.TenantSubscriptionId));
                if (sub is not null)
                {
                    sub.ExpiresAt = newExpiry;
                    sub.Status = SubscriptionStatus.Active;
                    UnitOfWork.Repository<TenantSubscription, string>().Update(sub);
                }
            }
            await Audit.StageAsync(AuditActionType.Update, nameof(SubscriptionRenewal), renewal.Id, $"{{\"status\":\"{dto.Status}\"}}", ct, tenantOverride: renewal.TenantId);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(MapRenewal(renewal));
        }

        public async Task<ApiResponse<UsageSummaryDto>> TenantUsageAsync(string tenantId, CancellationToken ct = default)
        {
            var tenant = await _tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct) ?? throw new NotFoundException("Tenant not found.");
            return Ok(await BuildUsageAsync(tenant.Id, ct));
        }

        // ---- shared usage builder (also used by self-service) ----

        internal async Task<UsageSummaryDto> BuildUsageAsync(string tenantId, CancellationToken ct)
        {
            var students = await _users.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted && u is Student, ct);
            var teachers = await _users.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted && u is Teacher, ct);
            var aiUsed = await UnitOfWork.Repository<AiUsageRecord, string>().CountAsync(
                new CriteriaSpecification<AiUsageRecord, string>(a => a.TenantId == tenantId));

            var sub = await LatestSubscription(tenantId);
            SubscriptionPlanDefinition? plan = sub is null ? null
                : await _plans.FirstOrDefaultAsync(p => p.Id == sub.PlanDefinitionId, ct);

            return new UsageSummaryDto
            {
                TenantId = tenantId,
                StudentsCount = students,
                TeachersCount = teachers,
                AiGenerationsUsed = aiUsed,
                MaxStudents = plan?.MaxStudents,
                MaxAiGenerationsPerMonth = plan?.MaxAiGenerationsPerMonth,
                OverStudentLimit = plan?.MaxStudents is int max && students > max
            };
        }

        internal async Task<TenantSubscription?> LatestSubscription(string tenantId)
        {
            var subs = await UnitOfWork.Repository<TenantSubscription, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<TenantSubscription, string>(s => s.TenantId == tenantId));
            return subs.OrderByDescending(s => s.StartsAt).FirstOrDefault();
        }

        private static DateTime? AsUtc(DateTime? v) => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)) : null;

        private static TenantDto Map(Tenant t) => new() { Id = t.Id, Name = t.Name, Domain = t.Domain, Status = t.Status, Type = t.Type };
        private static PlanDto MapPlan(SubscriptionPlanDefinition p) => new()
        {
            Id = p.Id, Code = p.Code, Name = p.Name, Tier = p.Tier, Price = p.Price, Currency = p.Currency,
            MaxStudents = p.MaxStudents, MaxTeachers = p.MaxTeachers, MaxAiGenerationsPerMonth = p.MaxAiGenerationsPerMonth, IsActive = p.IsActive
        };
        private static SubscriptionDto MapSub(TenantSubscription s) => new()
        {
            Id = s.Id, PlanDefinitionId = s.PlanDefinitionId, Status = s.Status, StartsAt = s.StartsAt,
            ExpiresAt = s.ExpiresAt, IsTrial = s.IsTrial, AutoRenew = s.AutoRenew
        };
        private static RenewalDto MapRenewal(SubscriptionRenewal r) => new()
        {
            Id = r.Id, TenantSubscriptionId = r.TenantSubscriptionId, Status = r.Status, RequestedAt = r.RequestedAt, NewExpiresAt = r.NewExpiresAt
        };
    }
}
