using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DerasaX.Application.Services.Abstractions.Storage
{
    /// <summary>
    /// Phase 16 — pluggable binary store behind the durable-file service. Implementations move
    /// only opaque bytes keyed by an already-validated, tenant-scoped storage key; all metadata,
    /// authorization, audit and validation are the responsibility of <see cref="IFileStorageService"/>.
    /// A provider must NEVER fake success: an unconfigured/unreachable backend throws
    /// <see cref="DerasaX.Domain.Exceptions.StorageUnavailableException"/>.
    /// </summary>
    public interface IFileStorageProvider
    {
        /// <summary>Stable provider name persisted on the file record (e.g. "Local", "S3").</summary>
        string Name { get; }

        Task SaveAsync(string storageKey, Stream content, string contentType, CancellationToken ct = default);

        /// <summary>Opens a readable stream for the stored object; throws NotFound if absent.</summary>
        Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default);

        Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default);

        Task DeleteAsync(string storageKey, CancellationToken ct = default);
    }
}
