using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Services.Storage
{
    /// <summary>
    /// Phase 16 — the server-side, per-purpose validation policy: allowed extensions + content
    /// types, the maximum size, the default sensitivity, and whether explicit consent is required.
    /// Allowlists are deny-by-default, so executables/scripts are rejected unless a purpose ever
    /// explicitly permits them (none does). The client-declared content type is cross-checked
    /// against the extension's allowed types — never trusted on its own.
    /// </summary>
    public sealed class FilePurposeRule
    {
        public required long MaxBytes { get; init; }
        public required IReadOnlySet<string> Extensions { get; init; }
        public required IReadOnlySet<string> ContentTypes { get; init; }
        public required FileVisibility DefaultVisibility { get; init; }
        public bool RequiresConsent { get; init; }
    }

    public static class FilePurposePolicy
    {
        private const long MB = 1024L * 1024L;

        // --- shared type groups ---
        private static readonly string[] DocExt = { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".txt", ".csv", ".odt", ".rtf" };
        private static readonly string[] DocCt =
        {
            "application/pdf", "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.oasis.opendocument.text",
            "application/rtf", "text/plain", "text/csv"
        };
        private static readonly string[] ImgExt = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private static readonly string[] ImgCt = { "image/jpeg", "image/png", "image/gif", "image/webp" };
        private static readonly string[] MediaExt = { ".mp4", ".webm", ".mp3", ".wav", ".m4a" };
        private static readonly string[] MediaCt = { "video/mp4", "video/webm", "audio/mpeg", "audio/wav", "audio/mp4", "audio/x-m4a" };

        private static IReadOnlySet<string> Set(params string[][] groups) =>
            new HashSet<string>(groups.SelectMany(g => g), StringComparer.OrdinalIgnoreCase);

        private static readonly IReadOnlyDictionary<FilePurpose, FilePurposeRule> Rules =
            new Dictionary<FilePurpose, FilePurposeRule>
            {
                [FilePurpose.LessonMaterial] = new() { MaxBytes = 100 * MB, Extensions = Set(DocExt, ImgExt, MediaExt), ContentTypes = Set(DocCt, ImgCt, MediaCt), DefaultVisibility = FileVisibility.TenantInternal },
                [FilePurpose.MessageAttachment] = new() { MaxBytes = 25 * MB, Extensions = Set(DocExt, ImgExt), ContentTypes = Set(DocCt, ImgCt), DefaultVisibility = FileVisibility.Private },
                [FilePurpose.ParentDocumentRequest] = new() { MaxBytes = 25 * MB, Extensions = Set(DocExt, ImgExt), ContentTypes = Set(DocCt, ImgCt), DefaultVisibility = FileVisibility.Sensitive },
                [FilePurpose.ParentDocumentResponse] = new() { MaxBytes = 25 * MB, Extensions = Set(DocExt, ImgExt), ContentTypes = Set(DocCt, ImgCt), DefaultVisibility = FileVisibility.Sensitive },
                [FilePurpose.CommunityAttachment] = new() { MaxBytes = 25 * MB, Extensions = Set(DocExt, ImgExt), ContentTypes = Set(DocCt, ImgCt), DefaultVisibility = FileVisibility.TenantInternal },
                [FilePurpose.CompetitionAttachment] = new() { MaxBytes = 50 * MB, Extensions = Set(DocExt, ImgExt, MediaExt), ContentTypes = Set(DocCt, ImgCt, MediaCt), DefaultVisibility = FileVisibility.TenantInternal },
                [FilePurpose.CvEnrollmentAsset] = new() { MaxBytes = 10 * MB, Extensions = Set(ImgExt), ContentTypes = Set(ImgCt), DefaultVisibility = FileVisibility.Sensitive, RequiresConsent = true },
                [FilePurpose.ProfileImage] = new() { MaxBytes = 5 * MB, Extensions = Set(ImgExt), ContentTypes = Set(ImgCt), DefaultVisibility = FileVisibility.TenantInternal },
                [FilePurpose.SubmissionAttachment] = new() { MaxBytes = 50 * MB, Extensions = Set(DocExt, ImgExt), ContentTypes = Set(DocCt, ImgCt), DefaultVisibility = FileVisibility.Private },
                [FilePurpose.Other] = new() { MaxBytes = 10 * MB, Extensions = Set(DocExt, ImgExt), ContentTypes = Set(DocCt, ImgCt), DefaultVisibility = FileVisibility.Private },
            };

        public static FilePurposeRule For(FilePurpose purpose) =>
            Rules.TryGetValue(purpose, out var r) ? r : Rules[FilePurpose.Other];

        /// <summary>Maps a purpose onto the legacy <see cref="FileRecordType"/> for backward compatibility.</summary>
        public static FileRecordType ToLegacyType(FilePurpose purpose) => purpose switch
        {
            FilePurpose.LessonMaterial => FileRecordType.LessonMaterial,
            FilePurpose.MessageAttachment => FileRecordType.MessageAttachment,
            FilePurpose.SubmissionAttachment => FileRecordType.SubmissionAttachment,
            FilePurpose.ProfileImage => FileRecordType.ProfileImage,
            _ => FileRecordType.Other
        };
    }
}
