using System;
using System.Linq;
using System.Threading.Tasks;
using DerasaX.Api.Errors;
using DerasaX.Application.Common;
using Microsoft.AspNetCore.Http;

namespace DerasaX.Api.Security
{
    /// <summary>
    /// When the authenticated principal's <c>mustChangePassword</c> claim is <c>true</c>, every API
    /// request is rejected except the small allowlist needed to change the password or leave the
    /// session (change-password, logout, revoke, refresh). Placed between UseAuthentication and
    /// UseAuthorization so it sees validated claims but runs before any [Authorize] policy check —
    /// catching every current and future controller uniformly.
    /// </summary>
    public sealed class MustChangePasswordGateMiddleware
    {
        private static readonly string[] Allowlist =
        {
            "/api/v1/account/change-password",
            "/api/v1/account/logout",
            "/api/v1/account/revoke",
            "/api/v1/account/refresh",
        };

        private readonly RequestDelegate _next;

        public MustChangePasswordGateMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path;
            if (context.User.Identity?.IsAuthenticated == true
                && path.StartsWithSegments("/api")
                && !Allowlist.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase))
                && context.User.HasClaim(c => c.Type == "mustChangePassword" && c.Value == "true"))
            {
                var pd = ProblemResultFactory.Build(
                    context, StatusCodes.Status403Forbidden, ErrorCodes.PasswordChangeRequired,
                    "Password change required.",
                    "You must change your temporary password before continuing.");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(pd, pd.GetType());
                return;
            }

            await _next(context);
        }
    }
}
