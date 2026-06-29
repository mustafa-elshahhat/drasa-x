using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions.Storage;
using DerasaX.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace DerasaX.Infrastructure.Storage
{
    /// <summary>
    /// Phase 16 — local/dev durable provider. Stores bytes under a single deterministic, configured
    /// root (default <c>App_Data/FileStorage</c> beside the app, never wwwroot and never an arbitrary
    /// per-call folder). Every resolved path is canonicalized and confirmed to stay under the root,
    /// so a crafted storage key can never escape via traversal.
    /// </summary>
    public sealed class LocalFileStorageProvider : IFileStorageProvider
    {
        private readonly string _root;

        public LocalFileStorageProvider(IOptions<FileStorageSettings> settings)
        {
            var configured = settings.Value.Local.RootPath;
            if (string.IsNullOrWhiteSpace(configured))
                configured = "App_Data/FileStorage";
            _root = Path.IsPathRooted(configured)
                ? Path.GetFullPath(configured)
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configured));
            Directory.CreateDirectory(_root);
        }

        public string Name => "Local";

        public async Task SaveAsync(string storageKey, Stream content, string contentType, CancellationToken ct = default)
        {
            var path = ResolveUnderRoot(storageKey);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await content.CopyToAsync(fs, ct);
        }

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
        {
            var path = ResolveUnderRoot(storageKey);
            if (!File.Exists(path))
                throw new NotFoundException("Stored file content not found.");
            Stream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(fs);
        }

        public Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default) =>
            Task.FromResult(File.Exists(ResolveUnderRoot(storageKey)));

        public Task DeleteAsync(string storageKey, CancellationToken ct = default)
        {
            var path = ResolveUnderRoot(storageKey);
            if (File.Exists(path)) File.Delete(path);
            return Task.CompletedTask;
        }

        /// <summary>Maps an opaque storage key to a path that is provably inside the root.</summary>
        private string ResolveUnderRoot(string storageKey)
        {
            if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains("..") || Path.IsPathRooted(storageKey))
                throw new BadRequestException("Unsafe storage key.");

            var relative = storageKey.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            var full = Path.GetFullPath(Path.Combine(_root, relative));

            var rootWithSep = _root.EndsWith(Path.DirectorySeparatorChar)
                ? _root
                : _root + Path.DirectorySeparatorChar;
            if (!full.StartsWith(rootWithSep, StringComparison.Ordinal))
                throw new BadRequestException("Storage key resolves outside the storage root.");

            return full;
        }
    }
}
