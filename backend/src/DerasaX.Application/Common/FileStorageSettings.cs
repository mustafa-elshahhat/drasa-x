namespace DerasaX.Application.Common
{
    /// <summary>
    /// Phase 16 durable-storage configuration (<c>FileStorage</c> section). All production values
    /// (bucket, endpoint, credentials, signing key) come from configuration/environment — never
    /// hardcoded. The local default provider needs no secrets.
    /// </summary>
    public class FileStorageSettings
    {
        public const string SectionName = "FileStorage";

        /// <summary>Active provider: "Local" (default) or "S3".</summary>
        public string Provider { get; set; } = "Local";

        /// <summary>Lifetime of an issued signed-download token, in seconds.</summary>
        public int SignedUrlTtlSeconds { get; set; } = 300;

        /// <summary>
        /// HMAC key used to sign download tokens. Falls back to the app <c>SecretKey</c> when empty.
        /// Local/dev value lives in appsettings.Development.json; production supplies it via env.
        /// </summary>
        public string? SigningKey { get; set; }

        public LocalStorageOptions Local { get; set; } = new();
        public S3StorageOptions S3 { get; set; } = new();

        /// <summary>Phase 18 — malware-scan policy for uploaded files.</summary>
        public ScannerOptions Scanner { get; set; } = new();

        /// <summary>Phase 19 — retention/purge job policy.</summary>
        public RetentionOptions Retention { get; set; } = new();
    }

    /// <summary>
    /// Phase 19 — file-retention/purge policy. The background sweep is DISABLED by default
    /// (so it never mutates a shared local/test database); the core sweep logic is invoked
    /// directly by tests and can be enabled per-environment. Hard-purge (deleting bytes + rows
    /// of files soft-deleted past a grace window) is independently gated and OFF by default;
    /// soft-delete-on-expiry runs whenever the job runs. Production values come from config.
    /// </summary>
    public class RetentionOptions
    {
        /// <summary>Enables the interval-driven background retention sweep. Default OFF (local-safe).</summary>
        public bool Enabled { get; set; } = false;

        /// <summary>Minutes between background sweeps when enabled.</summary>
        public int ScanIntervalMinutes { get; set; } = 60;

        /// <summary>Minutes to wait after startup before the first sweep (avoids boot contention).</summary>
        public int InitialDelaySeconds { get; set; } = 30;

        /// <summary>
        /// When true, files soft-deleted longer ago than <see cref="HardPurgeGraceDays"/> have their
        /// bytes + metadata row permanently removed. Default OFF (soft-delete-only is the safe posture).
        /// </summary>
        public bool HardPurgeEnabled { get; set; } = false;

        /// <summary>Grace window (days) after soft-delete before a hard purge is eligible.</summary>
        public int HardPurgeGraceDays { get; set; } = 30;
    }

    /// <summary>
    /// Phase 18 — virus/malware scanner policy. Honest by construction: the default
    /// <c>Disabled</c> mode records uploads as <c>NotScanned</c> (never a faked <c>Clean</c>).
    /// A real scanner (ClamAV/cloud) is wired in staging/production via a future
    /// <c>ClamAv</c> mode; locally only <c>Disabled</c>, <c>Unavailable</c> and the
    /// deterministic <c>Stub</c> test scanner exist.
    /// </summary>
    public class ScannerOptions
    {
        /// <summary>
        /// "Disabled" (default, local/CI), "Unavailable", "Stub" (deterministic EICAR test scanner), or
        /// "ClamAv" (real clamd INSTREAM client for staging/production). Staging/prod example:
        /// Mode=ClamAv, Host=clamav, Port=3310, RejectOnUnavailable=true. (Phase 22 PR-1.)
        /// </summary>
        public string Mode { get; set; } = "Disabled";

        /// <summary>
        /// When true, an upload is rejected (503) if the scanner is enabled but cannot
        /// reach/produce a verdict. Recommended <c>true</c> in production once a real scanner
        /// is wired; default <c>false</c> locally so honest "ScannerUnavailable" is recorded.
        /// </summary>
        public bool RejectOnUnavailable { get; set; } = false;

        /// <summary>Upper bound (bytes) the scanner will read into memory; 0 = no extra cap.</summary>
        public long MaxScanBytes { get; set; } = 0;

        /// <summary>ClamAv daemon host (clamd). Default "localhost"; staging/prod set the service hostname.</summary>
        public string Host { get; set; } = "localhost";

        /// <summary>ClamAv daemon TCP port (clamd default 3310).</summary>
        public int Port { get; set; } = 3310;

        /// <summary>Connect/IO timeout (seconds) for the ClamAv daemon. Default 10.</summary>
        public int TimeoutSeconds { get; set; } = 10;
    }

    public class LocalStorageOptions
    {
        /// <summary>
        /// Deterministic root directory for the local provider. Relative paths resolve against the
        /// application base directory. Defaults to <c>App_Data/FileStorage</c> — never wwwroot and
        /// never an arbitrary per-call folder.
        /// </summary>
        public string RootPath { get; set; } = "App_Data/FileStorage";
    }

    public class S3StorageOptions
    {
        /// <summary>Custom endpoint for S3-compatible stores (MinIO, R2, Spaces); empty = AWS.</summary>
        public string? ServiceUrl { get; set; }
        public string? Region { get; set; }
        public string? Bucket { get; set; }
        public string? AccessKeyId { get; set; }
        public string? SecretAccessKey { get; set; }
        /// <summary>Path-style addressing (required by most S3-compatible non-AWS endpoints).</summary>
        public bool ForcePathStyle { get; set; } = true;

        /// <summary>True only when bucket + credentials are present — gates honest availability.</summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Bucket) &&
            !string.IsNullOrWhiteSpace(AccessKeyId) &&
            !string.IsNullOrWhiteSpace(SecretAccessKey);
    }
}
