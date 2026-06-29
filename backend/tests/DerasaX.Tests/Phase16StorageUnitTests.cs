using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Storage;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 16 — pure unit tests for the storage primitives that do not need a host:
/// file-name/key safety, the per-purpose allowlist, the streaming hash, and the honest
/// "S3 selected but unconfigured" behavior of the production provider.
/// </summary>
public class Phase16StorageUnitTests
{
    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("..\\..\\windows\\system32")]
    [InlineData("foo/bar.pdf")]
    [InlineData("a\\b.pdf")]
    [InlineData("with\0null.pdf")]
    [InlineData("")]
    public void SanitizeFileName_rejects_traversal_and_unsafe_names(string name)
    {
        Assert.Throws<BadRequestException>(() => StorageSafety.SanitizeFileName(name));
    }

    [Theory]
    [InlineData("report.pdf", "report.pdf")]
    [InlineData("  spaced .docx", "spaced .docx")]
    public void SanitizeFileName_keeps_safe_leaf(string input, string expected)
    {
        Assert.Equal(expected, StorageSafety.SanitizeFileName(input));
    }

    [Fact]
    public void SanitizeFileName_rejects_reserved_windows_name()
    {
        Assert.Throws<BadRequestException>(() => StorageSafety.SanitizeFileName("CON.txt"));
    }

    [Fact]
    public void BuildStorageKey_is_opaque_tenant_scoped_and_safe()
    {
        var key = StorageSafety.BuildStorageKey("tenant-1", FilePurpose.LessonMaterial, ".pdf");
        Assert.StartsWith("tenants/tenant-1/lessonmaterial/", key);
        Assert.EndsWith(".pdf", key);
        StorageSafety.EnsureSafeStorageKey(key); // must not throw
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("/rooted/key")]
    [InlineData("a\\b")]
    public void EnsureSafeStorageKey_rejects_unsafe(string key)
    {
        Assert.Throws<BadRequestException>(() => StorageSafety.EnsureSafeStorageKey(key));
    }

    [Fact]
    public void FilePurposePolicy_denies_executables_by_default()
    {
        var rule = FilePurposePolicy.For(FilePurpose.LessonMaterial);
        Assert.DoesNotContain(".exe", rule.Extensions);
        Assert.DoesNotContain(".dll", rule.Extensions);
        Assert.DoesNotContain(".sh", rule.Extensions);
    }

    [Fact]
    public void FilePurposePolicy_cv_asset_requires_consent_and_images_only()
    {
        var rule = FilePurposePolicy.For(FilePurpose.CvEnrollmentAsset);
        Assert.True(rule.RequiresConsent);
        Assert.Equal(FileVisibility.Sensitive, rule.DefaultVisibility);
        Assert.Contains(".png", rule.Extensions);
        Assert.DoesNotContain(".pdf", rule.Extensions);
    }

    [Fact]
    public async Task HashingReadStream_computes_sha256_and_counts_bytes()
    {
        var data = Encoding.UTF8.GetBytes("hello phase 16");
        using var src = new MemoryStream(data);
        using var hashing = new HashingReadStream(src);
        using var sink = new MemoryStream();
        await hashing.CopyToAsync(sink);

        // Known SHA-256 of "hello phase 16"
        using var sha = System.Security.Cryptography.SHA256.Create();
        var expected = Convert.ToHexString(sha.ComputeHash(data)).ToLowerInvariant();

        Assert.Equal(expected, hashing.Finish());
        Assert.Equal(data.Length, hashing.BytesRead);
        Assert.Equal(data.Length, sink.Length);
    }

    [Fact]
    public async Task S3Provider_unconfigured_fails_honestly_not_silently()
    {
        var settings = Options.Create(new FileStorageSettings { Provider = "S3", S3 = new S3StorageOptions() });
        var provider = new S3FileStorageProvider(settings);
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        await Assert.ThrowsAsync<StorageUnavailableException>(
            () => provider.SaveAsync("tenants/t/x.bin", stream, "application/octet-stream"));
    }

    [Fact]
    public async Task LocalProvider_round_trips_and_blocks_traversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "derasax-p16-" + Guid.NewGuid().ToString("N"));
        var settings = Options.Create(new FileStorageSettings { Provider = "Local", Local = new LocalStorageOptions { RootPath = root } });
        var provider = new LocalFileStorageProvider(settings);

        var key = "tenants/tenant-1/other/" + Guid.NewGuid().ToString("N") + ".txt";
        var payload = Encoding.UTF8.GetBytes("durable bytes");
        using (var src = new MemoryStream(payload))
            await provider.SaveAsync(key, src, "text/plain");

        Assert.True(await provider.ExistsAsync(key));
        using (var read = await provider.OpenReadAsync(key))
        using (var ms = new MemoryStream())
        {
            await read.CopyToAsync(ms);
            Assert.Equal(payload, ms.ToArray());
        }

        await Assert.ThrowsAsync<BadRequestException>(
            () => provider.OpenReadAsync("../../escape.txt"));

        await provider.DeleteAsync(key);
        Assert.False(await provider.ExistsAsync(key));
        try { Directory.Delete(root, true); } catch { }
    }
}
