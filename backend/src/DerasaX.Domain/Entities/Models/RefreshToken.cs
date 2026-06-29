using Microsoft.EntityFrameworkCore;
using System;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// A rotating refresh-token record. The raw token value is NEVER persisted —
    /// only a SHA-256 hash (<see cref="TokenHash"/>) is stored, so a database leak
    /// cannot be replayed. Tokens belong to a <see cref="FamilyId"/> (session); on
    /// reuse of a rotated token the whole family is revoked (reuse detection).
    /// </summary>
    [Owned]
    public class RefreshToken
    {
        /// <summary>SHA-256 (base64) hash of the raw refresh token. The raw value is only ever sent to the client cookie.</summary>
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>Session/token-family identifier. All rotations of one login share it.</summary>
        public string FamilyId { get; set; } = string.Empty;

        /// <summary>Unique id of this token instance (audit / reuse correlation).</summary>
        public string Jti { get; set; } = string.Empty;

        public DateTime ExpiresOn { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? RevokedOn { get; set; }

        /// <summary>Why the token was revoked (rotated, logout, reuse-detected, password-change).</summary>
        public string? RevokedReason { get; set; }

        /// <summary>Hash of the token that replaced this one on rotation (audit chain).</summary>
        public string? ReplacedByTokenHash { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresOn;
        public bool IsActive => RevokedOn == null && !IsExpired;
    }
}
