using System.Collections.Generic;

namespace DerasaX.Api.RateLimiting
{
    /// <summary>
    /// Bound from the <c>RateLimiting</c> configuration section. Limits are config-driven
    /// so production, local development, and the integration test host can each set
    /// appropriate values without code changes.
    /// </summary>
    public class RateLimitingOptions
    {
        public const string SectionName = "RateLimiting";

        /// <summary>Master switch. When false the limiter middleware is not added and all policy attributes become inert.</summary>
        public bool Enabled { get; set; } = true;

        public Dictionary<string, RateLimitPolicyOptions> Policies { get; set; } = new();

        /// <summary>Returns the configured policy or a sensible production default when the section omits it.</summary>
        public RateLimitPolicyOptions For(string policy, int defaultPermit, int defaultWindowSeconds)
        {
            if (Policies.TryGetValue(policy, out var p) && p is not null)
            {
                if (p.PermitLimit <= 0) p.PermitLimit = defaultPermit;
                if (p.WindowSeconds <= 0) p.WindowSeconds = defaultWindowSeconds;
                return p;
            }
            return new RateLimitPolicyOptions { PermitLimit = defaultPermit, WindowSeconds = defaultWindowSeconds };
        }
    }

    public class RateLimitPolicyOptions
    {
        /// <summary>Maximum permitted requests per partition per window.</summary>
        public int PermitLimit { get; set; }

        /// <summary>Fixed-window length in seconds.</summary>
        public int WindowSeconds { get; set; }

        /// <summary>Requests queued once the permit limit is reached (0 = reject immediately).</summary>
        public int QueueLimit { get; set; }
    }
}
