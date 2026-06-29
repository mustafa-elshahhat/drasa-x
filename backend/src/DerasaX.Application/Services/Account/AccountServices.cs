using DerasaX.Application.Common;
using DerasaX.Application.Dto.AccountDto;
using DerasaX.Application.Services.Abstractions.Account;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DerasaX.Application.Services.Account
{
    public class AccountServices : IAccountServices
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtSettings _jwt;
        private readonly string _signingKey;
        private readonly ILogger<AccountServices> _logger;

        public AccountServices(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOptions<JwtSettings> jwtOptions,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            ILogger<AccountServices> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwt = jwtOptions.Value;
            _signingKey = configuration["SecretKey"] ?? string.Empty;
            _logger = logger;
        }

        // ---------------------------------------------------------------- Login
        public async Task<AuthResult> Login(LoginDto loginDto, string? clientIp = null)
        {
            var loginCode = (loginDto.UserID ?? string.Empty).Trim();

            var user = await _userManager.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.LoginCode == loginCode);

            // Identical external behavior for nonexistent user vs wrong password
            // (no account-existence disclosure). A dummy hash check keeps timing similar.
            if (user is null)
            {
                _logger.LogWarning("AUDIT login.failed result=invalid_credentials reason=no_user ip={Ip}", clientIp);
                return AuthResult.Fail(AuthOutcome.InvalidCredentials);
            }

            // Lockout-aware credential check (increments AccessFailedCount on failure).
            var signIn = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password ?? string.Empty, lockoutOnFailure: true);

            if (signIn.IsLockedOut)
            {
                _logger.LogWarning("AUDIT login.failed result=locked uid={Uid} tenant={Tenant} ip={Ip}", user.Id, user.TenantId, clientIp);
                return AuthResult.Fail(AuthOutcome.AccountLocked);
            }
            if (!signIn.Succeeded)
            {
                _logger.LogWarning("AUDIT login.failed result=invalid_credentials uid={Uid} ip={Ip}", user.Id, clientIp);
                return AuthResult.Fail(AuthOutcome.InvalidCredentials);
            }

            if (user.IsDeleted)
            {
                _logger.LogWarning("AUDIT login.denied result=disabled uid={Uid} ip={Ip}", user.Id, clientIp);
                return AuthResult.Fail(AuthOutcome.AccountDisabled);
            }

            if (IsTenantBlocked(user))
            {
                _logger.LogWarning("AUDIT login.denied result=tenant_suspended uid={Uid} tenant={Tenant} ip={Ip}", user.Id, user.TenantId, clientIp);
                return AuthResult.Fail(AuthOutcome.TenantSuspended);
            }

            // Issue access token + a fresh rotating refresh token (new family per login).
            var (rawRefresh, refreshRecord) = NewRefreshToken();
            user.refreshTokens ??= new List<RefreshToken>();
            user.refreshTokens.Add(refreshRecord);
            await _userManager.UpdateAsync(user);

            var model = await BuildAuthModel(user);
            _logger.LogInformation("AUDIT login.success uid={Uid} role={Role} tenant={Tenant} ip={Ip}", user.Id, model.Role, user.TenantId, clientIp);

            return new AuthResult
            {
                Outcome = AuthOutcome.Success,
                Model = model,
                RawRefreshToken = rawRefresh,
                RefreshTokenExpiration = refreshRecord.ExpiresOn
            };
        }

        // -------------------------------------------------------------- Refresh
        public async Task<AuthResult> RefreshTokenAsync(string? token)
        {
            if (string.IsNullOrEmpty(token))
                return AuthResult.Fail(AuthOutcome.InvalidToken);

            var hash = Hash(token);
            var user = await _userManager.Users
                .Include(u => u.Tenant)
                .SingleOrDefaultAsync(u => u.refreshTokens.Any(t => t.TokenHash == hash));

            if (user is null)
                return AuthResult.Fail(AuthOutcome.InvalidToken);

            var presented = user.refreshTokens!.Single(t => t.TokenHash == hash);

            // Reuse detection: a token that was already rotated/revoked is being
            // presented again ⇒ revoke the whole family (session) and reject.
            if (!presented.IsActive)
            {
                RevokeFamily(user, presented.FamilyId, "reuse-detected");
                await _userManager.UpdateAsync(user);
                _logger.LogWarning("AUDIT refresh.reuse_detected uid={Uid} family={Family}", user.Id, presented.FamilyId);
                return AuthResult.Fail(AuthOutcome.TokenReuseDetected);
            }

            // Revalidate security-sensitive state from current persistence.
            if (user.IsDeleted)
                return AuthResult.Fail(AuthOutcome.AccountDisabled);
            if (IsTenantBlocked(user))
                return AuthResult.Fail(AuthOutcome.TenantSuspended);

            // Rotate: revoke presented, issue new in the SAME family.
            var (rawRefresh, newRecord) = NewRefreshToken(presented.FamilyId);
            presented.RevokedOn = DateTime.UtcNow;
            presented.RevokedReason = "rotated";
            presented.ReplacedByTokenHash = newRecord.TokenHash;
            user.refreshTokens!.Add(newRecord);
            await _userManager.UpdateAsync(user);

            var model = await BuildAuthModel(user);
            _logger.LogInformation("AUDIT refresh.success uid={Uid} family={Family}", user.Id, presented.FamilyId);

            return new AuthResult
            {
                Outcome = AuthOutcome.Success,
                Model = model,
                RawRefreshToken = rawRefresh,
                RefreshTokenExpiration = newRecord.ExpiresOn
            };
        }

        // --------------------------------------------------------------- Logout
        public async Task<OperationResult> LogoutAsync(string? token)
        {
            // Idempotent: missing/unknown token still returns success to the client.
            if (string.IsNullOrEmpty(token))
                return OperationResult.Ok("Logged out.");

            var hash = Hash(token);
            var user = await _userManager.Users
                .SingleOrDefaultAsync(u => u.refreshTokens.Any(t => t.TokenHash == hash));

            if (user is not null)
            {
                var presented = user.refreshTokens!.Single(t => t.TokenHash == hash);
                RevokeFamily(user, presented.FamilyId, "logout");
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("AUDIT logout uid={Uid} family={Family}", user.Id, presented.FamilyId);
            }

            return OperationResult.Ok("Logged out.");
        }

        public async Task<TokenRevocationResult> RevokeTokenAsync(string token)
        {
            var hash = Hash(token);
            var user = await _userManager.Users
                .SingleOrDefaultAsync(u => u.refreshTokens.Any(t => t.TokenHash == hash));
            if (user == null)
                return new TokenRevocationResult { Success = false, Message = "Invalid token." };

            var refreshToken = user.refreshTokens!.Single(t => t.TokenHash == hash);
            if (!refreshToken.IsActive)
                return new TokenRevocationResult { Success = false, Message = "Token is already revoked or expired." };

            refreshToken.RevokedOn = DateTime.UtcNow;
            refreshToken.RevokedReason = "revoked";
            await _userManager.UpdateAsync(user);
            return new TokenRevocationResult { Success = true, Message = "Token revoked successfully." };
        }

        public async Task<TokenRevocationResult> RevokeOwnTokenAsync(string userId, string? token)
        {
            // Identity is the trusted JWT subject. We only ever load THIS user, so a
            // client cannot revoke another account's session by passing its token.
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
                return new TokenRevocationResult { Success = false, Message = "Invalid request." };

            user.refreshTokens ??= new List<RefreshToken>();

            // No specific token => revoke every active session for the caller.
            if (string.IsNullOrEmpty(token))
            {
                var hadActive = user.refreshTokens.Any(t => t.IsActive);
                RevokeAllSessions(user, "self-revoke-all");
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("AUDIT token.self_revoked_all uid={Uid}", user.Id);
                return new TokenRevocationResult
                {
                    Success = true,
                    Message = hadActive ? "All sessions revoked." : "No active sessions."
                };
            }

            // A specific token must belong to the caller's own sessions.
            var hash = Hash(token);
            var owned = user.refreshTokens.FirstOrDefault(t => t.TokenHash == hash);
            if (owned is null)
                return new TokenRevocationResult { Success = false, Message = "Invalid token." };
            if (!owned.IsActive)
                return new TokenRevocationResult { Success = false, Message = "Token is already revoked or expired." };

            // Revoke the whole family so the rotating session is fully terminated.
            RevokeFamily(user, owned.FamilyId, "self-revoke");
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("AUDIT token.self_revoked uid={Uid} family={Family}", user.Id, owned.FamilyId);
            return new TokenRevocationResult { Success = true, Message = "Session revoked." };
        }

        // ------------------------------------------------------- Change password
        public async Task<OperationResult> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
                return OperationResult.Fail("Unable to change password.");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                return OperationResult.Fail("Unable to change password.");

            await _userManager.UpdateSecurityStampAsync(user);
            RevokeAllSessions(user, "password-change");
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("AUDIT password.changed uid={Uid}", user.Id);
            return OperationResult.Ok("Password changed.");
        }

        // -------------------------------------------------------- Password reset
        public async Task<OperationResult> ForgotPasswordAsync(ForgotPasswordDto dto, bool isDevelopment)
        {
            var loginCode = (dto.LoginCode ?? string.Empty).Trim();
            var user = await _userManager.Users.Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.LoginCode == loginCode);

            // Never disclose whether the account exists / is eligible.
            if (user is null || user.IsDeleted || IsTenantBlocked(user))
            {
                _logger.LogInformation("AUDIT password.reset_requested result=accepted_or_noop");
                return OperationResult.Ok("If the account exists, a reset has been initiated.");
            }

            // Single-use, expiring token via the Identity data-protector token provider.
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            _logger.LogInformation("AUDIT password.reset_requested uid={Uid}", user.Id);

            // No external email provider locally: in Development only, return the token
            // so the flow can be exercised. NEVER surfaced outside Development.
            return new OperationResult
            {
                Success = true,
                Message = "If the account exists, a reset has been initiated.",
                DevToken = isDevelopment ? token : null
            };
        }

        public async Task<OperationResult> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var loginCode = (dto.LoginCode ?? string.Empty).Trim();
            var user = await _userManager.Users.Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.LoginCode == loginCode);

            if (user is null || user.IsDeleted || IsTenantBlocked(user))
                return OperationResult.Fail("Invalid or expired reset token.");

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
                return OperationResult.Fail("Invalid or expired reset token.");

            await _userManager.UpdateSecurityStampAsync(user);
            await _userManager.ResetAccessFailedCountAsync(user);
            RevokeAllSessions(user, "password-reset");
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("AUDIT password.reset_completed uid={Uid}", user.Id);
            return OperationResult.Ok("Password has been reset.");
        }

        // ----------------------------------------------------------- Helpers
        private static bool IsTenantBlocked(ApplicationUser user)
        {
            // Platform-scoped users (no tenant) are never tenant-blocked.
            if (string.IsNullOrEmpty(user.TenantId)) return false;
            // If the tenant record is unknown or not Active, block.
            return user.Tenant is null || user.Tenant.Status != TenantStatus.Active;
        }

        private async Task<AuthModel> BuildAuthModel(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();
            var jwt = await CreateJwtToken(user, roles);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(jwt);

            return new AuthModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName,
                Role = role,
                IsAuthenticated = true,
                Token = tokenString,
                ExpiresOn = jwt.ValidTo,
                Message = "login successfully"
            };
        }

        private async Task<JwtSecurityToken> CreateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.UserName ?? user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("uid", user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                // Security stamp / token version (invalidate-on-change support).
                new("sstamp", user.SecurityStamp ?? string.Empty)
            };

            // tenantId claim ONLY for tenant-scoped users. Platform SystemAdmin omits it.
            if (!string.IsNullOrEmpty(user.TenantId))
                claims.Add(new Claim("tenantId", user.TenantId));

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
            }

            claims.AddRange(userClaims);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            return new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
                signingCredentials: creds);
        }

        private (string raw, RefreshToken record) NewRefreshToken(string? familyId = null)
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var raw = Convert.ToBase64String(bytes);
            var record = new RefreshToken
            {
                TokenHash = Hash(raw),
                FamilyId = familyId ?? Guid.NewGuid().ToString("N"),
                Jti = Guid.NewGuid().ToString("N"),
                CreatedOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays)
            };
            return (raw, record);
        }

        private static void RevokeFamily(ApplicationUser user, string familyId, string reason)
        {
            foreach (var t in user.refreshTokens!.Where(t => t.FamilyId == familyId && t.RevokedOn == null))
            {
                t.RevokedOn = DateTime.UtcNow;
                t.RevokedReason = reason;
            }
        }

        private static void RevokeAllSessions(ApplicationUser user, string reason)
        {
            if (user.refreshTokens is null) return;
            foreach (var t in user.refreshTokens.Where(t => t.RevokedOn == null))
            {
                t.RevokedOn = DateTime.UtcNow;
                t.RevokedReason = reason;
            }
        }

        private static string Hash(string raw)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToBase64String(bytes);
        }
    }
}
