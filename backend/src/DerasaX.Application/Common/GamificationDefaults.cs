using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;

namespace DerasaX.Application.Common
{
    /// <summary>
    /// Phase 14 — built-in default point values for automatic awards, plus a resolver that lets a
    /// school administrator override them per tenant via an enabled <see cref="GamificationRule"/>.
    /// Keeping defaults in code means gamification keeps working when no rules are configured, while
    /// the resolver keeps award point values tenant-configurable and deterministic.
    /// </summary>
    public static class GamificationDefaults
    {
        public const int OfficeHourAttended = 10;
        public const int CompetitionTopRank = 50;
        public const int CompetitionParticipation = 10;

        /// <summary>Manual awards are bounded to discourage abuse / fat-finger ledger inflation.</summary>
        public const int ManualAwardMin = -1000;
        public const int ManualAwardMax = 1000;

        /// <summary>
        /// Returns the effective point value for an automatic trigger: the enabled tenant rule's value
        /// if one exists, otherwise <paramref name="fallback"/>. Also returns the rule id (if any) so
        /// the ledger row can be linked to the configuration that produced it.
        /// </summary>
        public static async Task<(int points, string? ruleId)> ResolveAsync(
            IUnitOfWork uow, GamificationTrigger trigger, int fallback, CancellationToken ct = default)
        {
            var rules = await uow.Repository<GamificationRule, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<GamificationRule, string>(r => r.Trigger == trigger && r.Enabled));
            var rule = rules.FirstOrDefault();
            return rule is null ? (fallback, null) : (rule.Points, rule.Id);
        }
    }
}
