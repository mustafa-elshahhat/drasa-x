using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Services.Abstractions.Storage
{
    /// <summary>
    /// Phase 18 — malware-scan abstraction for uploaded file bytes. Honest by construction:
    /// implementations must NEVER report <see cref="FileScanStatus.Clean"/> without actually
    /// inspecting the content. When no scanner is available the orchestrator records
    /// <see cref="FileScanStatus.NotScanned"/> or <see cref="FileScanStatus.ScannerUnavailable"/>
    /// (never a faked clean verdict).
    /// </summary>
    public interface IFileScanner
    {
        /// <summary>Human-readable mode for diagnostics/audit (e.g. "Disabled", "Unavailable", "Stub").</summary>
        string Mode { get; }

        /// <summary>
        /// When false the orchestrator records <see cref="FileScanStatus.NotScanned"/> WITHOUT
        /// re-reading the stored bytes (the local default — no scanner present).
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// When true, an upload is rejected if the scanner is enabled but cannot produce a
        /// verdict (a "fail-closed" production posture). Default false = record
        /// <see cref="FileScanStatus.ScannerUnavailable"/> honestly and allow the upload.
        /// </summary>
        bool RejectOnUnavailable { get; }

        /// <summary>Inspect the content and return an honest scan verdict.</summary>
        Task<FileScanResult> ScanAsync(Stream content, string fileName, string declaredContentType, CancellationToken ct = default);
    }

    /// <summary>Outcome of a malware scan. <see cref="Signature"/> names the detected threat when infected.</summary>
    public sealed record FileScanResult(FileScanStatus Status, string? Signature = null)
    {
        public static FileScanResult NotScanned() => new(FileScanStatus.NotScanned);
        public static FileScanResult Unavailable() => new(FileScanStatus.ScannerUnavailable);
        public static FileScanResult Clean() => new(FileScanStatus.Clean);
        public static FileScanResult Infected(string signature) => new(FileScanStatus.Infected, signature);
    }
}
