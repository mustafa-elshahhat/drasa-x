namespace DerasaX.Application.Dto.AccountDto
{
    /// <summary>Outcome of an authentication operation, mapped by the controller to the canonical HTTP status / errorCode.</summary>
    public enum AuthOutcome
    {
        Success = 0,
        InvalidCredentials,   // 401 UNAUTHENTICATED (also used for nonexistent user — identical response)
        AccountLocked,        // 401 ACCOUNT_LOCKED
        AccountDisabled,      // 401 ACCOUNT_DISABLED
        TenantSuspended,      // 403 TENANT_SUSPENDED
        InvalidToken,         // 401 UNAUTHENTICATED (missing/expired/revoked/malformed refresh)
        TokenReuseDetected    // 401 UNAUTHENTICATED (family revoked)
    }

    public class AuthResult
    {
        public AuthOutcome Outcome { get; init; }
        public AuthModel? Model { get; init; }

        /// <summary>Raw refresh token to place in the HttpOnly cookie (never serialized to the body).</summary>
        public string? RawRefreshToken { get; init; }
        public System.DateTime RefreshTokenExpiration { get; init; }

        public bool Succeeded => Outcome == AuthOutcome.Success;

        public static AuthResult Fail(AuthOutcome outcome) => new() { Outcome = outcome };
    }
}
