namespace DerasaX.Application.Services.Abstractions
{
    /// <summary>
    /// Mints short-lived signed internal service tokens for backend → school-ai-rag
    /// calls (Phase 2 SERVICE_AUTHENTICATION). The tenant travels as a signed claim,
    /// never as a request-body field. Tokens are single-purpose and ≤ 5 minutes.
    /// </summary>
    public interface IAiServiceTokenProvider
    {
        /// <param name="tenantId">Trusted tenant for tenant-scoped AI calls; null for platform calls.</param>
        /// <param name="actorUserId">Opaque actor id for audit (never PII).</param>
        /// <param name="scope">Required scope/permission for the call, e.g. "ai:chat".</param>
        ServiceTokenResult CreateToken(string? tenantId, string? actorUserId, string scope);
    }

    public class ServiceTokenResult
    {
        public string Token { get; init; } = string.Empty;
        public string Jti { get; init; } = string.Empty;
        public System.DateTime ExpiresOn { get; init; }
        public string Audience { get; init; } = string.Empty;
        public string Issuer { get; init; } = string.Empty;
    }
}
