using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace DerasaX.Api.Helper
{
    /// <summary>
    /// Phase 19 — fail-closed production configuration validation. Runs ONLY outside
    /// Development/Test (so local + integration tests are never affected). Refuses to start
    /// in Production with empty/placeholder secrets, no CORS origins, or an unmet
    /// system-admin MFA requirement. This prevents the deployment from silently claiming
    /// production readiness it does not have. It NEVER prints a secret value.
    /// </summary>
    public static class ProductionConfigValidator
    {
        // Substrings that mark a dev/sample/placeholder value (never valid in production).
        private static readonly string[] PlaceholderMarkers =
        {
            "local-dev", "not-for-production", "changeme", "change_me", "change-me",
            "placeholder", "example", "todo", "sample", "dummy", "0123456789abcdef"
        };

        public static bool AppliesTo(string environmentName) =>
            !string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase);

        /// <summary>Returns the list of fatal configuration problems (empty = OK).</summary>
        public static IReadOnlyList<string> Validate(IConfiguration config, string environmentName)
        {
            var errors = new List<string>();
            if (!AppliesTo(environmentName))
                return errors; // Development / Test: not enforced.

            CheckSecret(config["SecretKey"], "SecretKey", required: true, errors);
            CheckSecret(config["ServiceAuth:SigningKey"], "ServiceAuth:SigningKey", required: true, errors);
            // FileStorage:SigningKey may be empty (it falls back to SecretKey, already validated),
            // but if SET it must not be a placeholder.
            CheckSecret(config["FileStorage:SigningKey"], "FileStorage:SigningKey", required: false, errors);

            var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            if (origins.Length == 0)
                errors.Add("Cors:AllowedOrigins must list the explicit production frontend origin(s) in Production.");
            if (origins.Any(o => string.Equals(o?.Trim(), "*", StringComparison.Ordinal)))
                errors.Add("Cors:AllowedOrigins must not contain '*' (credentials are enabled).");

            // System-admin MFA fail-closed gate: prevents claiming production readiness without MFA.
            var mfaRequired = config.GetValue("Auth:SystemAdminMfaRequired", false);
            var mfaProvider = config["Auth:MfaProvider"];
            if (mfaRequired && string.IsNullOrWhiteSpace(mfaProvider))
            {
                errors.Add(
                    "Auth:SystemAdminMfaRequired=true but no MFA provider is wired (Auth:MfaProvider). " +
                    "System-admin MFA is not implemented yet; production startup is blocked so MFA-protected " +
                    "admin access is never falsely assumed. Implement MFA (TOTP/WebAuthn) or set the flag false " +
                    "with an explicit, documented risk acceptance.");
            }

            return errors;
        }

        private static void CheckSecret(string? value, string name, bool required, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required) errors.Add($"{name} must be set to a strong production secret.");
                return;
            }
            var low = value.ToLowerInvariant();
            if (value.Length < 32)
                errors.Add($"{name} is too short (<32 chars) for a production secret.");
            else if (PlaceholderMarkers.Any(m => low.Contains(m)))
                errors.Add($"{name} looks like a development/placeholder value; set a real production secret.");
        }
    }
}
