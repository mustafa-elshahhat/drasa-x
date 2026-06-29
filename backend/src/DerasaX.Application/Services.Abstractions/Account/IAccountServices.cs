using DerasaX.Application.Dto.AccountDto;
using System.Threading.Tasks;

namespace DerasaX.Application.Services.Abstractions.Account
{
    public interface IAccountServices
    {
        Task<AuthResult> Login(LoginDto loginDto, string? clientIp = null);

        /// <summary>Rotate the presented refresh token. Detects reuse and revokes the family.</summary>
        Task<AuthResult> RefreshTokenAsync(string? token);

        /// <summary>Revoke the presented refresh token's session (idempotent).</summary>
        Task<OperationResult> LogoutAsync(string? token);

        Task<TokenRevocationResult> RevokeTokenAsync(string token);

        /// <summary>
        /// Self-service revocation scoped to the authenticated user (identity comes from
        /// the trusted JWT, never the client). When <paramref name="token"/> is supplied
        /// it must belong to <paramref name="userId"/>'s own sessions; that session's
        /// whole token family is revoked. When omitted, all of the user's active refresh
        /// sessions are revoked. Works for any authenticated user, including a
        /// platform SystemAdmin with no tenant.
        /// </summary>
        Task<TokenRevocationResult> RevokeOwnTokenAsync(string userId, string? token);

        /// <summary>Authenticated change-password: verifies current password, bumps security stamp, revokes refresh sessions.</summary>
        Task<OperationResult> ChangePasswordAsync(string userId, ChangePasswordDto dto);

        /// <summary>Request a password reset. Never discloses whether the account exists.</summary>
        Task<OperationResult> ForgotPasswordAsync(ForgotPasswordDto dto, bool isDevelopment);

        /// <summary>Complete a password reset with a single-use token; revokes refresh sessions on success.</summary>
        Task<OperationResult> ResetPasswordAsync(ResetPasswordDto dto);
    }
}
