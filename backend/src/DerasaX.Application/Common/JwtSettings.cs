namespace DerasaX.Application.Common
{
    /// <summary>
    /// Strongly-typed access-token (browser) signing/validation settings, bound from
    /// the <c>Jwt</c> configuration section. The signing key is read separately from
    /// <c>SecretKey</c> for backward compatibility with the existing configuration.
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = "derasax-backend";
        public string Audience { get; set; } = "derasax-frontend";

        /// <summary>Access-token lifetime in minutes (short-lived). Phase 2 target: ~15 min.</summary>
        public int AccessTokenMinutes { get; set; } = 15;

        /// <summary>Refresh-token lifetime in days (sliding via rotation).</summary>
        public int RefreshTokenDays { get; set; } = 10;

        /// <summary>Allowed clock skew (seconds) on access-token validation.</summary>
        public int ClockSkewSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Internal service-to-service (backend → school-ai-rag) token settings, bound
    /// from the <c>ServiceAuth</c> section (Phase 2 SERVICE_AUTHENTICATION).
    /// </summary>
    public class ServiceAuthSettings
    {
        public const string SectionName = "ServiceAuth";

        public string Issuer { get; set; } = "derasax-backend";
        public string Audience { get; set; } = "school-ai-rag";
        public string Subject { get; set; } = "svc:ai-orchestrator";

        /// <summary>Service-token TTL in seconds. MUST be ≤ 300 (5 min).</summary>
        public int TtlSeconds { get; set; } = 120;

        /// <summary>Key id advertised in the JWT header for rotation.</summary>
        public string KeyId { get; set; } = "ai-local";

        /// <summary>HMAC signing key (env <c>ServiceAuth__SigningKey</c> / <c>AI__ServiceSigningKey</c>). Never logged.</summary>
        public string SigningKey { get; set; } = string.Empty;
    }
}
