using System;

namespace DerasaX.Domain.Exceptions
{
    /// <summary>
    /// Phase 16 — the selected durable storage provider is unreachable or not configured
    /// (e.g. the S3 provider is selected locally without credentials/bucket). Surfaced as a
    /// normalized 503 STORAGE_UNAVAILABLE; never silently swallowed and never faked as success.
    /// </summary>
    public class StorageUnavailableException : Exception
    {
        public StorageUnavailableException(string message) : base(message) { }
        public StorageUnavailableException(string message, Exception inner) : base(message, inner) { }
    }
}
