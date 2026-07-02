using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions.Provisioning;
using DerasaX.Domain.Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.Provisioning
{
    /// <summary>
    /// Centralized credential generation, replacing what used to be two duplicated private
    /// implementations (<c>UserProvisioningService</c> and <c>SystemAdminPortalService</c>).
    /// </summary>
    public class CredentialProvisioningService : ICredentialProvisioningService
    {
        private const int MaxCollisionAttempts = 10;

        private readonly UserManager<ApplicationUser> _users;

        public CredentialProvisioningService(UserManager<ApplicationUser> users)
        {
            _users = users;
        }

        /// <summary>
        /// Strong one-time password meeting the configured Identity policy (upper + lower + digit,
        /// length 14). Generated with a CSPRNG; never logged or persisted in clear text.
        /// </summary>
        public string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string all = upper + lower + digits;
            var chars = new char[14];
            chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
            chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
            chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
            for (var i = 3; i < chars.Length; i++)
                chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
            // Fisher–Yates shuffle so the guaranteed classes are not always in positions 0-2.
            for (var i = chars.Length - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars);
        }

        public async Task<string> GenerateLoginCodeAsync(string fullName, string role, CancellationToken ct = default)
        {
            var baseSlug = RolePrefix(role) + Slugify(fullName);
            for (var attempt = 0; attempt < MaxCollisionAttempts; attempt++)
            {
                var suffix = RandomNumberGenerator.GetInt32(1000, 10000); // 4 digits
                var candidate = $"{baseSlug}.{suffix}";
                if (!await _users.Users.AnyAsync(u => u.LoginCode == candidate, ct))
                    return candidate;
            }
            // Deterministic termination guarantee — never fail provisioning over a collision.
            return $"{baseSlug}.{Guid.NewGuid().ToString("N")[..8]}";
        }

        private static string RolePrefix(string role) => role switch
        {
            Roles.Teacher => "teacher.",
            Roles.Parent => "parent.",
            Roles.SchoolAdmin => "admin.",
            _ => string.Empty, // Student: no prefix
        };

        private static string Slugify(string fullName)
        {
            var lowered = fullName.Trim().ToLowerInvariant();
            var cleaned = new string(lowered.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c)).ToArray());
            var tokens = cleaned.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return "user";
            var first = tokens[0];
            var last = tokens[^1];
            return first == last ? first : $"{first}.{last}";
        }
    }
}
