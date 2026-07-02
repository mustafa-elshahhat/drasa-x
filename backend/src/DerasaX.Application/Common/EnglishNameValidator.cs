using System.Text.RegularExpressions;
using DerasaX.Domain.Exceptions;

namespace DerasaX.Application.Common
{
    /// <summary>
    /// Enforces the platform-wide rule that account full names must be written in English
    /// letters only (no Arabic, digits-only, emoji, or symbol names). Shared by every account
    /// creation/reset flow so the rule cannot be bypassed by any single caller.
    /// </summary>
    public static class EnglishNameValidator
    {
        // English letters with internal spaces/hyphen/apostrophe/dot as separators. Trimmed
        // before matching; rejects Arabic script, digits-only, emoji, and other symbols.
        private static readonly Regex Pattern =
            new(@"^[A-Za-z]+(?:[ '.\-]+[A-Za-z]+)*$", RegexOptions.Compiled);

        public static void Validate(string? fullName)
        {
            var trimmed = (fullName ?? string.Empty).Trim();
            if (trimmed.Length == 0)
                throw new BadRequestException("FullName is required.");
            if (!Pattern.IsMatch(trimmed))
                throw new ValidationException("Full name must be written in English letters only.");
        }
    }
}
