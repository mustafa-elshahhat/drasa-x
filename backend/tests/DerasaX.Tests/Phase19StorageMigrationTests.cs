using DerasaX.Application.Services.Abstractions.Storage;
using DerasaX.Application.Services.Image.FileServices;
using DerasaX.Application.Services.Image.GenericResolver;
using DerasaX.Application.Services.Storage;
using DerasaX.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 19 — legacy image migration. Proves the legacy IFileService is now backed by the durable
/// storage adapter (new uploads no longer touch wwwroot), and that the picture URL resolver maps a
/// durable FileRecord id to the backend-mediated download endpoint while keeping pre-existing legacy
/// "{guid}.ext" filenames resolving to the old static /Images path (backward compatible).
/// </summary>
public class Phase19StorageMigrationTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public Phase19StorageMigrationTests(IntegrationFactory factory) => _factory = factory;

    private sealed class Img : IHasImageUrl { public string? ImageUrl { get; set; } = ""; }

    private static GenericPictureUrlResolver<Img, object> Resolver(string folder = "Subjects")
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        ctx.Request.Host = new HostString("localhost:5155");
        var accessor = new HttpContextAccessor { HttpContext = ctx };
        return new GenericPictureUrlResolver<Img, object>(accessor, folder);
    }

    [Fact]
    public void IFileService_resolves_to_durable_adapter_not_legacy_wwwroot()
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IFileService>();
        Assert.IsType<DurableImageFileService>(svc);
    }

    [Fact]
    public void Resolver_maps_durable_file_record_id_to_backend_mediated_url()
    {
        var r = Resolver();
        var url = r.Resolve(new Img { ImageUrl = "ab12cd34ef56" }, new object(), "PictureUrl", null!);
        Assert.Equal("https://localhost:5155/api/v1/files/ab12cd34ef56/download", url);
    }

    [Fact]
    public void Resolver_keeps_legacy_filename_on_static_path()
    {
        var r = Resolver();
        var url = r.Resolve(new Img { ImageUrl = "9f8e7d6c.png" }, new object(), "PictureUrl", null!);
        Assert.Equal("https://localhost:5155/Images/Subjects/9f8e7d6c.png", url);
    }

    [Fact]
    public void Resolver_returns_empty_for_missing_image()
    {
        var r = Resolver();
        Assert.Equal(string.Empty, r.Resolve(new Img { ImageUrl = "" }, new object(), "PictureUrl", null!));
    }
}
