using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.Operations
{
    /// <inheritdoc cref="IPlanLimitEnforcer"/>
    public class PlanLimitEnforcer : IPlanLimitEnforcer
    {
        private readonly IUnitOfWork _uow;
        private readonly IPlatformRepository<SubscriptionPlanDefinition> _plans;
        private readonly UserManager<ApplicationUser> _users;

        public PlanLimitEnforcer(IUnitOfWork uow, IPlatformRepository<SubscriptionPlanDefinition> plans, UserManager<ApplicationUser> users)
        {
            _uow = uow;
            _plans = plans;
            _users = users;
        }

        public async Task EnsureCanAddUserAsync(string tenantId, string role, CancellationToken ct = default)
        {
            var plan = await ResolvePlanAsync(tenantId, ct);
            if (plan is null) return;

            int? max = role switch
            {
                Roles.Student => plan.MaxStudents,
                Roles.Teacher => plan.MaxTeachers,
                Roles.Parent => plan.MaxParents,
                Roles.SchoolAdmin => plan.MaxSchoolAdmins,
                _ => null
            };
            if (max is not int limit) return;

            var count = role switch
            {
                Roles.Student => await _users.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted && u is Student, ct),
                Roles.Teacher => await _users.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted && u is Teacher, ct),
                Roles.Parent => await _users.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted && u is Parent, ct),
                Roles.SchoolAdmin => await _users.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted && u is SchoolAdmin, ct),
                _ => 0
            };
            if (count >= limit)
                throw new PlanLimitExceededException($"The '{plan.Name}' plan allows a maximum of {limit} {role}(s).");
        }

        public async Task EnsureCanAddClassAsync(string tenantId, CancellationToken ct = default)
        {
            var plan = await ResolvePlanAsync(tenantId, ct);
            if (plan?.MaxClasses is not int max) return;
            var count = await _uow.Repository<SchoolClass, string>().CountAsync(
                new CriteriaSpecification<SchoolClass, string>(c => c.TenantId == tenantId && !c.IsDeleted));
            if (count >= max)
                throw new PlanLimitExceededException($"The '{plan.Name}' plan allows a maximum of {max} classes.");
        }

        public async Task EnsureCanAddSubjectAsync(string tenantId, CancellationToken ct = default)
        {
            var plan = await ResolvePlanAsync(tenantId, ct);
            if (plan?.MaxSubjects is not int max) return;
            var count = await _uow.Repository<Subject, string>().CountAsync(
                new CriteriaSpecification<Subject, string>(s => s.TenantId == tenantId && !s.IsDeleted));
            if (count >= max)
                throw new PlanLimitExceededException($"The '{plan.Name}' plan allows a maximum of {max} subjects.");
        }

        public async Task EnsureCanAddLessonMaterialAsync(string tenantId, CancellationToken ct = default)
        {
            var plan = await ResolvePlanAsync(tenantId, ct);
            if (plan?.MaxLessonMaterials is not int max) return;
            var count = await _uow.Repository<LessonMaterial, string>().CountAsync(
                new CriteriaSpecification<LessonMaterial, string>(m => m.TenantId == tenantId && !m.IsDeleted));
            if (count >= max)
                throw new PlanLimitExceededException($"The '{plan.Name}' plan allows a maximum of {max} lesson materials.");
        }

        public async Task EnsureCanUploadBytesAsync(string tenantId, long additionalBytes, CancellationToken ct = default)
        {
            var plan = await ResolvePlanAsync(tenantId, ct);
            if (plan?.MaxStorageMb is not int maxMb) return;
            var files = await _uow.Repository<FileRecord, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<FileRecord, string>(f => f.TenantId == tenantId && !f.IsDeleted), asNoTracking: true);
            var usedBytes = files.Sum(f => f.SizeBytes);
            var maxBytes = (long)maxMb * 1024 * 1024;
            if (usedBytes + additionalBytes > maxBytes)
                throw new PlanLimitExceededException($"The '{plan.Name}' plan allows a maximum of {maxMb} MB of storage.");
        }

        public async Task EnsureWithinAiMonthlyQuotaAsync(string tenantId, CancellationToken ct = default)
        {
            var plan = await ResolvePlanAsync(tenantId, ct);
            if (plan is null || (plan.MaxAiGenerationsPerMonth is null && plan.MaxAiTokensPerMonth is null)) return;

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var records = await _uow.Repository<AiUsageRecord, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<AiUsageRecord, string>(a => a.TenantId == tenantId && a.UsedAt >= startOfMonth), asNoTracking: true);
            var list = records.ToList();

            if (plan.MaxAiGenerationsPerMonth is int maxRequests && list.Count >= maxRequests)
                throw new PlanLimitExceededException($"The '{plan.Name}' plan allows a maximum of {maxRequests} AI requests per month.");

            if (plan.MaxAiTokensPerMonth is int maxTokens)
            {
                var tokensUsed = list.Sum(a => a.TotalTokens ?? 0);
                if (tokensUsed >= maxTokens)
                    throw new PlanLimitExceededException($"The '{plan.Name}' plan allows a maximum of {maxTokens} AI tokens per month.");
            }
        }

        /// <summary>Resolves the tenant's current plan (latest subscription by StartsAt), or null if unassigned.</summary>
        private async Task<SubscriptionPlanDefinition?> ResolvePlanAsync(string tenantId, CancellationToken ct)
        {
            var subs = await _uow.Repository<TenantSubscription, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<TenantSubscription, string>(s => s.TenantId == tenantId), asNoTracking: true);
            var latest = subs.OrderByDescending(s => s.StartsAt).FirstOrDefault();
            if (latest is null) return null;
            return await _plans.FirstOrDefaultAsync(p => p.Id == latest.PlanDefinitionId, ct);
        }
    }
}
