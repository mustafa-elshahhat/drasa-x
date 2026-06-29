using System.IO;
using System.Text;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions.Storage;
using DerasaX.Domain.Enums;
using DerasaX.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 22 PR-1 (BE-03/SEC-01) — the real ClamAv scanner mode is wired and honest. Pure unit tests
/// (no DB / no daemon required): the factory selects the ClamAv client for Mode="ClamAv", and the client
/// returns <see cref="FileScanStatus.ScannerUnavailable"/> (NEVER a faked Clean) when no clamd daemon is
/// reachable — the local reality. Infected/Clean verdicts are covered deterministically by the Stub
/// scanner, and the fail-closed reject path by
/// <c>Phase18SecurityTests.Unavailable_scanner_with_reject_policy_rejects_the_upload_503</c>.
/// </summary>
public class PR1FileScannerTests
{
    private static IFileScanner Build(string mode, bool reject = false) =>
        FileScannerFactory.Create(Options.Create(new FileStorageSettings
        {
            Scanner = new ScannerOptions { Mode = mode, RejectOnUnavailable = reject, Host = "127.0.0.1", Port = 1, TimeoutSeconds = 2 }
        }));

    [Fact]
    public void Factory_wires_the_clamav_scanner_for_clamav_mode()
    {
        var scanner = Build("ClamAv", reject: true);
        Assert.IsType<ClamAvFileScanner>(scanner);
        Assert.Equal("ClamAv", scanner.Mode);
        Assert.True(scanner.IsEnabled);
        Assert.True(scanner.RejectOnUnavailable);
    }

    [Fact]
    public void Factory_defaults_to_disabled_for_unknown_or_default_modes()
    {
        Assert.IsType<DisabledFileScanner>(Build("Disabled"));
        Assert.IsType<DisabledFileScanner>(Build("something-else"));
    }

    [Fact]
    public async Task ClamAv_scanner_reports_unavailable_when_no_daemon_is_reachable()
    {
        // Port 1 has no clamd -> connection refused/timeout -> honest "unavailable", never a faked Clean.
        var scanner = new ClamAvFileScanner("127.0.0.1", 1, timeoutSeconds: 2, rejectOnUnavailable: true, maxScanBytes: 0);
        using var content = new MemoryStream(Encoding.ASCII.GetBytes("harmless content"));
        var result = await scanner.ScanAsync(content, "x.txt", "text/plain");
        Assert.Equal(FileScanStatus.ScannerUnavailable, result.Status);
    }
}
