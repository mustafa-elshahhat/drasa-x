using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions.Storage;
using DerasaX.Domain.Enums;
using Microsoft.Extensions.Options;

namespace DerasaX.Infrastructure.Storage
{
    /// <summary>
    /// Phase 18 — default scanner for environments without a real engine (local/CI). Records
    /// <see cref="FileScanStatus.NotScanned"/>; never re-reads bytes; never fakes a clean verdict.
    /// </summary>
    public sealed class DisabledFileScanner : IFileScanner
    {
        public string Mode => "Disabled";
        public bool IsEnabled => false;
        public bool RejectOnUnavailable => false;

        public Task<FileScanResult> ScanAsync(Stream content, string fileName, string declaredContentType, CancellationToken ct = default)
            => Task.FromResult(FileScanResult.NotScanned());
    }

    /// <summary>
    /// Phase 18 — a scanner that is configured/expected but cannot produce a verdict. Records
    /// <see cref="FileScanStatus.ScannerUnavailable"/> (or, when <see cref="RejectOnUnavailable"/>
    /// is set, the orchestrator rejects the upload). Used to model the honest "engine down" state.
    /// </summary>
    public sealed class UnavailableFileScanner : IFileScanner
    {
        public UnavailableFileScanner(bool rejectOnUnavailable) => RejectOnUnavailable = rejectOnUnavailable;

        public string Mode => "Unavailable";
        public bool IsEnabled => true;
        public bool RejectOnUnavailable { get; }

        public Task<FileScanResult> ScanAsync(Stream content, string fileName, string declaredContentType, CancellationToken ct = default)
            => Task.FromResult(FileScanResult.Unavailable());
    }

    /// <summary>
    /// Phase 18 — DETERMINISTIC test scanner. Flags content that contains the industry-standard
    /// EICAR anti-malware test pattern as <see cref="FileScanStatus.Infected"/>; everything else is
    /// reported <see cref="FileScanStatus.Clean"/> only after the bytes were actually read. This is a
    /// real (deterministic) verdict for tests — it is NOT a substitute for a production engine.
    /// The EICAR pattern is assembled from fragments at runtime so the literal signature never
    /// appears contiguously in source (which would trip a developer's local antivirus).
    /// </summary>
    public sealed class StubSignatureFileScanner : IFileScanner
    {
        private const int DefaultReadCap = 16 * 1024 * 1024; // never load more than 16 MB to scan
        private readonly long _maxScanBytes;

        // EICAR-STANDARD-ANTIVIRUS-TEST-FILE, assembled in pieces (never one literal).
        private static readonly string EicarPattern = string.Concat(
            @"X5O!P%@AP[4\PZX54(P^)7CC)7}$",
            "EICAR-STANDARD-",
            "ANTIVIRUS-TEST-FILE!",
            "$H+H*");

        public StubSignatureFileScanner(bool rejectOnUnavailable, long maxScanBytes)
        {
            RejectOnUnavailable = rejectOnUnavailable;
            _maxScanBytes = maxScanBytes > 0 ? Math.Min(maxScanBytes, DefaultReadCap) : DefaultReadCap;
        }

        public string Mode => "Stub";
        public bool IsEnabled => true;
        public bool RejectOnUnavailable { get; }

        public async Task<FileScanResult> ScanAsync(Stream content, string fileName, string declaredContentType, CancellationToken ct = default)
        {
            using var ms = new MemoryStream();
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                total += read;
                if (total > _maxScanBytes)
                {
                    // Too large to inspect with the in-memory stub — be honest, don't claim clean.
                    return FileScanResult.Unavailable();
                }
                ms.Write(buffer, 0, read);
            }

            var text = Encoding.Latin1.GetString(ms.GetBuffer(), 0, (int)ms.Length);
            return text.Contains(EicarPattern, StringComparison.Ordinal)
                ? FileScanResult.Infected("EICAR-Test-Signature")
                : FileScanResult.Clean();
        }
    }

    /// <summary>
    /// Phase 18 — selects the active <see cref="IFileScanner"/> from
    /// <c>FileStorage:Scanner:Mode</c> ("Disabled" default / "Unavailable" / "Stub").
    /// A real ClamAV/cloud scanner would be added here as a new mode in staging/production.
    /// </summary>
    public static class FileScannerFactory
    {
        public static IFileScanner Create(IOptions<FileStorageSettings> settings)
        {
            var scanner = settings.Value.Scanner ?? new ScannerOptions();
            return (scanner.Mode ?? "Disabled").Trim().ToLowerInvariant() switch
            {
                "stub" => new StubSignatureFileScanner(scanner.RejectOnUnavailable, scanner.MaxScanBytes),
                "unavailable" => new UnavailableFileScanner(scanner.RejectOnUnavailable),
                _ => new DisabledFileScanner(),
            };
        }
    }
}
