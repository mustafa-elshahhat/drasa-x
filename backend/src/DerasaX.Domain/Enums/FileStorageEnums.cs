namespace DerasaX.Domain.Enums
{
    /// <summary>
    /// Phase 16 — business purpose of a stored file. Drives the server-side content-type
    /// allowlist, the per-purpose size cap, the default sensitivity, and which authorization
    /// rules apply. Never trust a client-supplied purpose for a sensitive operation without the
    /// matching relationship check in the owning workflow service.
    /// </summary>
    public enum FilePurpose
    {
        LessonMaterial = 0,
        MessageAttachment = 1,
        ParentDocumentRequest = 2,
        ParentDocumentResponse = 3,
        CommunityAttachment = 4,
        CompetitionAttachment = 5,
        CvEnrollmentAsset = 6,
        ProfileImage = 7,
        SubmissionAttachment = 8,
        Other = 9
    }

    /// <summary>
    /// Sensitivity classification controlling baseline read authorization on the generic file
    /// endpoints. <see cref="TenantInternal"/> = readable by any member of the owning tenant;
    /// <see cref="Private"/> = owner or tenant/platform admin only; <see cref="Sensitive"/> =
    /// owner/admin only AND every download is audited (parent documents, CV enrollment assets).
    /// </summary>
    public enum FileVisibility
    {
        TenantInternal = 0,
        Private = 1,
        Sensitive = 2
    }

    /// <summary>
    /// Honest virus-scan state. Local/CI has no scanner, so uploads are recorded as
    /// <see cref="NotScanned"/> (or <see cref="ScannerUnavailable"/> when a scanner was expected
    /// but could not be reached) — scanning is never faked.
    /// </summary>
    public enum FileScanStatus
    {
        NotScanned = 0,
        ScannerUnavailable = 1,
        Pending = 2,
        Clean = 3,
        Infected = 4
    }
}
