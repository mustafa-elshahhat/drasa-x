namespace DerasaX.Application.Services.Abstractions
{
    /// <summary>
    /// Request-scoped trusted tenant/identity context. The ONLY trusted source of
    /// tenant is the validated access-token claim set — never a body, header, query
    /// or route value supplied by the browser (Phase 2 TENANT_ISOLATION §1).
    /// </summary>
    public interface ITenantContext
    {
        /// <summary>Tenant id from the trusted <c>tenantId</c> claim, or null for platform-scoped SystemAdmin.</summary>
        string? TenantId { get; }

        /// <summary>Authenticated user id (<c>uid</c>/NameIdentifier claim), if any.</summary>
        string? UserId { get; }

        /// <summary>Authenticated role claim, if any.</summary>
        string? Role { get; }

        /// <summary>True when a non-empty tenant claim is present.</summary>
        bool HasTenant { get; }

        /// <summary>True for an authenticated SystemAdmin operating without a tenant claim (platform scope).</summary>
        bool IsPlatformScope { get; }

        /// <summary>True when there is an authenticated principal.</summary>
        bool IsAuthenticated { get; }
    }
}
