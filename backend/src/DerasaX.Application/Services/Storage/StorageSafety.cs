using System;
using System.IO;
using System.Linq;
using System.Text;
using DerasaX.Domain.Exceptions;

namespace DerasaX.Application.Services.Storage
{
    /// <summary>
    /// Phase 16 — file-name and storage-key safety. Rejects path traversal and unsafe names, and
    /// produces a sanitized leaf name for display. Storage keys are always opaque GUID-based and
    /// tenant-scoped (built here, never from client input).
    /// </summary>
    public static class StorageSafety
    {
        private static readonly char[] Invalid = Path.GetInvalidFileNameChars()
            .Concat(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }).Distinct().ToArray();

        private static readonly string[] ReservedWindowsNames =
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        /// <summary>
        /// Validates a client-supplied file name and returns a safe, sanitized leaf name. Throws
        /// <see cref="BadRequestException"/> on traversal / control chars / empty / reserved names.
        /// </summary>
        public static string SanitizeFileName(string? original)
        {
            if (string.IsNullOrWhiteSpace(original))
                throw new BadRequestException("A file name is required.");

            // Reject anything that even looks like a path — never accept directory components.
            if (original.Contains('/') || original.Contains('\\') || original.Contains("..") ||
                original.Any(char.IsControl) || original.Contains('\0'))
                throw new BadRequestException("File name contains an unsafe path or character.");

            // Defensive: collapse to the leaf only.
            var leaf = Path.GetFileName(original).Trim();
            if (string.IsNullOrWhiteSpace(leaf) || leaf == "." || leaf == "..")
                throw new BadRequestException("File name is invalid.");

            var nameNoExt = Path.GetFileNameWithoutExtension(leaf);
            if (ReservedWindowsNames.Contains(nameNoExt, StringComparer.OrdinalIgnoreCase))
                throw new BadRequestException("File name uses a reserved system name.");

            var sb = new StringBuilder(leaf.Length);
            foreach (var c in leaf)
                sb.Append(Invalid.Contains(c) ? '_' : c);

            var sanitized = sb.ToString().Trim().TrimStart('.');
            if (sanitized.Length == 0)
                throw new BadRequestException("File name is invalid after sanitization.");

            return sanitized.Length > 200 ? sanitized[^200..] : sanitized;
        }

        /// <summary>Lowercased extension including the dot (e.g. ".pdf"); empty string if none.</summary>
        public static string Extension(string fileName) =>
            (Path.GetExtension(fileName) ?? string.Empty).ToLowerInvariant();

        /// <summary>
        /// Builds an opaque, tenant-scoped storage key. The key is provider-relative and never
        /// echoed as a public URL; the original name is preserved only as metadata.
        /// </summary>
        public static string BuildStorageKey(string tenantId, DerasaX.Domain.Enums.FilePurpose purpose, string extension)
        {
            var safeTenant = tenantId.Replace("/", "_").Replace("\\", "_").Replace("..", "_");
            var ext = string.IsNullOrEmpty(extension) ? string.Empty : extension;
            return $"tenants/{safeTenant}/{purpose.ToString().ToLowerInvariant()}/{Guid.NewGuid():N}{ext}";
        }

        /// <summary>
        /// Guards a storage key before it touches a provider: must be relative, forward-slash,
        /// no traversal, no rooted/drive paths. Defence in depth on top of opaque key generation.
        /// </summary>
        public static void EnsureSafeStorageKey(string storageKey)
        {
            if (string.IsNullOrWhiteSpace(storageKey) ||
                storageKey.Contains("..") ||
                storageKey.Contains('\\') ||
                storageKey.StartsWith('/') ||
                Path.IsPathRooted(storageKey) ||
                storageKey.Any(char.IsControl))
                throw new BadRequestException("Unsafe storage key.");
        }
    }
}
