namespace DerasaX.Api.RateLimiting
{
    /// <summary>
    /// Canonical rate-limiting policy names (Phase 5 cross-cutting requirement).
    /// Apply with <c>[EnableRateLimiting(RateLimitPolicies.X)]</c> on the controller
    /// or action that owns an abuse-sensitive surface. Each policy partitions by the
    /// trusted identity so one tenant/user can never consume another's quota.
    /// </summary>
    public static class RateLimitPolicies
    {
        /// <summary>Authentication-sensitive endpoints (login, refresh, password flows). Partitioned by client IP because the caller is anonymous.</summary>
        public const string Auth = "auth";

        /// <summary>AI-generation request entry points exposed by the main backend (AI-usage recording, AI-backed reads). Partitioned by tenant+user.</summary>
        public const string Ai = "ai";

        /// <summary>Quiz submission and other abuse-sensitive write commands. Partitioned by tenant+user.</summary>
        public const string Submission = "submission";

        /// <summary>Messaging and suggestion/feedback submission. Partitioned by tenant+user.</summary>
        public const string Messaging = "messaging";

        /// <summary>File metadata create/update endpoints. Partitioned by tenant+user.</summary>
        public const string Files = "files";

        /// <summary>Expensive reports and analytics. Partitioned by tenant+user.</summary>
        public const string Reports = "reports";
    }
}
