using System.ComponentModel.DataAnnotations;

namespace DerasaX.Application.Dto.AccountDto
{
    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        [Required]
        public string LoginCode { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required]
        public string LoginCode { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>Generic operation result that never discloses account existence.</summary>
    public class OperationResult
    {
        public bool Success { get; init; }
        public string? Message { get; init; }

        /// <summary>Development-only: surfaced reset token when no email provider is configured. Never populated in Production.</summary>
        public string? DevToken { get; init; }

        /// <summary>
        /// Set only by a successful <c>ChangePasswordAsync</c>: a freshly-issued raw refresh token so the
        /// caller (controller) can rotate the refresh cookie immediately, letting the browser refresh its
        /// access token right away instead of waiting on natural expiry. Never serialized directly to a
        /// response body — every controller action projects this type into <c>{ message }</c>.
        /// </summary>
        public string? RawRefreshToken { get; init; }
        public System.DateTime? RefreshTokenExpiration { get; init; }

        public static OperationResult Ok(string? message = null) => new() { Success = true, Message = message };
        public static OperationResult Fail(string? message = null) => new() { Success = false, Message = message };
    }
}
