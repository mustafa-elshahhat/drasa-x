using System.Security.Claims;
using DerasaX.Api.Errors;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.AccountDto;
using DerasaX.Application.Services.Abstractions.Account;
using DerasaX.Api.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Public authentication surface. Login/refresh/logout/forgot/reset are
    /// explicitly anonymous; change-password requires an authenticated user.
    /// Canonical routes: <c>/api/v1/account/*</c> (Phase 2 AUTHENTICATION_FLOW §2).
    /// </summary>
    [ApiController]
    [Route("api/v1/account")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public class AccountController : ControllerBase
    {
        private const string RefreshCookieName = "refreshToken";

        private readonly IAccountServices _accountServices;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public AccountController(IAccountServices accountServices, IConfiguration configuration, IWebHostEnvironment env)
        {
            _accountServices = accountServices;
            _configuration = configuration;
            _env = env;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _accountServices.Login(loginDto, clientIp);

            if (!result.Succeeded)
                return MapFailure(result.Outcome);

            SetRefreshCookie(result.RawRefreshToken!, result.RefreshTokenExpiration);
            return Ok(result.Model);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies[RefreshCookieName];
            var result = await _accountServices.RefreshTokenAsync(refreshToken);

            if (!result.Succeeded)
            {
                ClearRefreshCookie();
                return MapFailure(result.Outcome);
            }

            SetRefreshCookie(result.RawRefreshToken!, result.RefreshTokenExpiration);
            return Ok(result.Model);
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies[RefreshCookieName];
            await _accountServices.LogoutAsync(refreshToken);
            ClearRefreshCookie();
            return Ok(new { message = "Logged out." });
        }

        // Self-account: any authenticated user (incl. platform SystemAdmin with no
        // tenant) may revoke their OWN session. Identity comes from the trusted JWT;
        // a client-supplied token is only honoured if it belongs to the caller.
        [HttpPost("revoke")]
        [Authorize(Policy = Policies.SelfAccount)]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenDto revokeTokenDto)
        {
            var userId = User.FindFirst("uid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return ProblemResultFactory.Result(HttpContext, StatusCodes.Status401Unauthorized, ErrorCodes.Unauthenticated, "Unauthenticated.");

            var token = revokeTokenDto.Token ?? Request.Cookies[RefreshCookieName];

            var result = await _accountServices.RevokeOwnTokenAsync(userId, token);
            if (!result.Success)
                return ProblemResultFactory.Result(HttpContext, StatusCodes.Status400BadRequest, ErrorCodes.BadRequest, result.Message ?? "Token is invalid.");
            return Ok(new { message = result.Message });
        }

        // Self-account: any authenticated user (incl. platform SystemAdmin) may change
        // their OWN password. Changing it bumps the security stamp and revokes all
        // refresh sessions. Note: already-issued access tokens remain valid until their
        // normal 15-minute lifetime expires — they are not retroactively invalidated.
        [HttpPost("change-password")]
        [Authorize(Policy = Policies.SelfAccount)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = User.FindFirst("uid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return ProblemResultFactory.Result(HttpContext, StatusCodes.Status401Unauthorized, ErrorCodes.Unauthenticated, "Unauthenticated.");

            var result = await _accountServices.ChangePasswordAsync(userId, dto);
            if (!result.Success)
                return ProblemResultFactory.Result(HttpContext, StatusCodes.Status400BadRequest, ErrorCodes.BadRequest, "Unable to change password.", result.Message);

            ClearRefreshCookie();
            return Ok(new { message = result.Message });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _accountServices.ForgotPasswordAsync(dto, _env.IsDevelopment());
            // Always the same response shape — never discloses account existence.
            return Ok(new { message = result.Message, devToken = result.DevToken });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _accountServices.ResetPasswordAsync(dto);
            if (!result.Success)
                return ProblemResultFactory.Result(HttpContext, StatusCodes.Status400BadRequest, ErrorCodes.BadRequest, "Invalid or expired reset token.");
            return Ok(new { message = result.Message });
        }

        // ---------------------------------------------------------------- helpers
        private IActionResult MapFailure(AuthOutcome outcome) => outcome switch
        {
            AuthOutcome.TenantSuspended =>
                ProblemResultFactory.Result(HttpContext, StatusCodes.Status403Forbidden, ErrorCodes.TenantSuspended, "Tenant is suspended."),
            AuthOutcome.AccountLocked =>
                ProblemResultFactory.Result(HttpContext, StatusCodes.Status401Unauthorized, ErrorCodes.AccountLocked, "Account is temporarily locked. Try again later."),
            AuthOutcome.AccountDisabled =>
                ProblemResultFactory.Result(HttpContext, StatusCodes.Status401Unauthorized, ErrorCodes.Unauthenticated, "Login failed."),
            // InvalidCredentials / InvalidToken / TokenReuseDetected all map to a
            // single generic 401 with no account-existence signal.
            _ => ProblemResultFactory.Result(HttpContext, StatusCodes.Status401Unauthorized, ErrorCodes.Unauthenticated, "Invalid credentials.")
        };

        private void SetRefreshCookie(string refreshToken, System.DateTime expires)
        {
            Response.Cookies.Append(RefreshCookieName, refreshToken, BuildCookieOptions(expires));
        }

        private void ClearRefreshCookie()
        {
            var options = BuildCookieOptions(System.DateTime.UtcNow.AddDays(-1));
            Response.Cookies.Delete(RefreshCookieName, options);
        }

        private CookieOptions BuildCookieOptions(System.DateTime expires)
        {
            // Secure cookies require HTTPS; local development runs over HTTP, so Secure
            // is enabled outside Development. SameSite/Path are configurable.
            var sameSite = _configuration.GetValue("Cookie:SameSite", "Lax");
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = sameSite.ToLowerInvariant() switch
                {
                    "strict" => SameSiteMode.Strict,
                    "none" => SameSiteMode.None,
                    _ => SameSiteMode.Lax
                },
                // Scope the cookie to the account routes that consume it (refresh/logout)
                // — limits CSRF surface and avoids sending it on every API call.
                Path = "/api/v1/account",
                Expires = expires
            };
        }
    }
}
