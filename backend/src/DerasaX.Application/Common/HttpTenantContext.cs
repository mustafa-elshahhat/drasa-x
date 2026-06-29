using System.Linq;
using System.Security.Claims;
using DerasaX.Application.Services.Abstractions;
using Microsoft.AspNetCore.Http;

namespace DerasaX.Application.Common
{
    /// <summary>
    /// Reads the trusted tenant/identity context from the validated access token on
    /// the current request. Tenant comes ONLY from the signed <c>tenantId</c> claim;
    /// client-supplied tenant values are never consulted here.
    /// </summary>
    public class HttpTenantContext : ITenantContext
    {
        private readonly IHttpContextAccessor _accessor;

        public HttpTenantContext(IHttpContextAccessor accessor) => _accessor = accessor;

        private ClaimsPrincipal? User => _accessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

        public string? TenantId
        {
            get
            {
                var value = User?.FindFirst("tenantId")?.Value;
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }

        public string? UserId =>
            User?.FindFirst("uid")?.Value ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public string? Role
        {
            get
            {
                var value = User?.FindFirst("role")?.Value ?? User?.FindFirst(ClaimTypes.Role)?.Value;
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }

        public bool HasTenant => TenantId != null;

        public bool IsPlatformScope =>
            IsAuthenticated && !HasTenant && IsInRole(Roles.SystemAdmin);

        private bool IsInRole(string role) =>
            User != null &&
            (User.IsInRole(role) ||
             User.Claims.Any(c => (c.Type == "role" || c.Type == ClaimTypes.Role) && c.Value == role));
    }
}
