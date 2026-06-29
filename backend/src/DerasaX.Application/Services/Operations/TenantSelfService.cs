using System;
using System.Linq;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.Operations
{
    /// <summary>
    /// SchoolAdmin self-service over the caller's OWN tenant only. Strictly separated from the
    /// SystemAdmin platform service: every method resolves the tenant from the trusted claim and
    /// can never read or mutate another tenant's data.
    /// </summary>
    public class TenantSelfService : OperationsServiceBase, ITenantSelfService
    {
        private readonly IPlatformRepository<Tenant> _tenants;
        private readonly IPlatformRepository<SubscriptionPlanDefinition> _plans;
        private readonly UserManager<ApplicationUser> _users;

        public TenantSelfService(IUnitOfWork uow, ITenantContext tenant, IAuditWriter audit,
            IPlatformRepository<Tenant> tenants, IPlatformRepository<SubscriptionPlanDefinition> plans,
            UserManager<ApplicationUser> users) : base(uow, tenant, audit)
        {
            _tenants = tenants;
            _plans = plans;
            _users = users;
        }

        public async Task<ApiResponse<TenantDto>> CurrentTenantAsync(CancellationToken ct = default)
        {
            var id = RequireTenant();
            var tenant = await _tenants.FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException("Tenant not found.");
            return Ok(new TenantDto { Id = tenant.Id, Name = tenant.Name, Domain = tenant.Domain, Status = tenant.Status, Type = tenant.Type });
        }

        public async Task<ApiResponse<SubscriptionDto>> CurrentSubscriptionAsync(CancellationToken ct = default)
        {
            var sub = await LatestSubscription(RequireTenant()) ?? throw new NotFoundException("No subscription found.");
            return Ok(new SubscriptionDto
            {
                Id = sub.Id, PlanDefinitionId = sub.PlanDefinitionId, Status = sub.Status, StartsAt = sub.StartsAt,
                ExpiresAt = sub.ExpiresAt, IsTrial = sub.IsTrial, AutoRenew = sub.AutoRenew
            });
        }

        public async Task<ApiResponse<UsageSummaryDto>> CurrentUsageAsync(CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var students = await _users.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted && u is Student, ct);
            var teachers = await _users.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted && u is Teacher, ct);
            var aiUsed = await UnitOfWork.Repository<AiUsageRecord, string>().CountAsync(
                new CriteriaSpecification<AiUsageRecord, string>(a => a.TenantId == tenantId));
            var sub = await LatestSubscription(tenantId);
            var plan = sub is null ? null : await _plans.FirstOrDefaultAsync(p => p.Id == sub.PlanDefinitionId, ct);
            return Ok(new UsageSummaryDto
            {
                TenantId = tenantId, StudentsCount = students, TeachersCount = teachers, AiGenerationsUsed = aiUsed,
                MaxStudents = plan?.MaxStudents, MaxAiGenerationsPerMonth = plan?.MaxAiGenerationsPerMonth,
                OverStudentLimit = plan?.MaxStudents is int max && students > max
            });
        }

        public async Task<ApiResponse<RenewalDto>> RequestRenewalAsync(RequestRenewalDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var sub = await LatestSubscription(tenantId) ?? throw new ConflictException("No active subscription to renew.");
            var renewal = new SubscriptionRenewal
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                TenantSubscriptionId = sub.Id,
                Status = RenewalStatus.Requested,
                RequestedAt = DateTime.UtcNow,
                PreviousExpiresAt = sub.ExpiresAt,
                NewExpiresAt = dto.RequestedExpiresAt.HasValue ? DateTime.SpecifyKind(dto.RequestedExpiresAt.Value, DateTimeKind.Utc) : null,
                Notes = dto.Notes
            };
            await UnitOfWork.Repository<SubscriptionRenewal, string>().AddAsync(renewal);
            await Audit.StageAsync(AuditActionType.Create, nameof(SubscriptionRenewal), renewal.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(new RenewalDto { Id = renewal.Id, TenantSubscriptionId = sub.Id, Status = renewal.Status, RequestedAt = renewal.RequestedAt, NewExpiresAt = renewal.NewExpiresAt }, 201, "Renewal requested.");
        }

        private async Task<TenantSubscription?> LatestSubscription(string tenantId)
        {
            var subs = await UnitOfWork.Repository<TenantSubscription, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<TenantSubscription, string>(s => s.TenantId == tenantId));
            return subs.OrderByDescending(s => s.StartsAt).FirstOrDefault();
        }
    }
}
