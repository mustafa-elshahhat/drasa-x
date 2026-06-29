using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using DerasaX.Application.Common;
using DerasaX.Application.Services.ServiceAuth;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Unit tests for the backend internal service-token provider (Phase 3 §24/§44).
/// Asserts the issued token matches the Phase 2 SERVICE_AUTHENTICATION contract.
/// </summary>
public class ServiceTokenProviderTests
{
    private static AiServiceTokenProvider CreateProvider() =>
        new(Options.Create(new ServiceAuthSettings
        {
            Issuer = "derasax-backend",
            Audience = "school-ai-rag",
            Subject = "svc:ai-orchestrator",
            TtlSeconds = 120,
            KeyId = "ai-local",
            SigningKey = "unit-test-service-signing-key-0123456789abcdef"
        }), NullLogger<AiServiceTokenProvider>.Instance);

    [Fact]
    public void Token_carries_contract_claims()
    {
        var result = CreateProvider().CreateToken("tenant-1", "u-9", "ai:chat");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal("derasax-backend", jwt.Issuer);
        Assert.Contains("school-ai-rag", jwt.Audiences);
        Assert.Equal("svc:ai-orchestrator", jwt.Claims.First(c => c.Type == "sub").Value);
        Assert.Equal("ai:chat", jwt.Claims.First(c => c.Type == "scope").Value);
        Assert.Equal("tenant-1", jwt.Claims.First(c => c.Type == "tenantId").Value);
        Assert.NotEmpty(jwt.Claims.First(c => c.Type == "jti").Value);
        Assert.Equal("ai-local", jwt.Header["kid"]);
    }

    [Fact]
    public void Token_ttl_never_exceeds_five_minutes()
    {
        var provider = new AiServiceTokenProvider(Options.Create(new ServiceAuthSettings
        {
            SigningKey = "unit-test-service-signing-key-0123456789abcdef",
            TtlSeconds = 99999 // attempt to exceed policy
        }), NullLogger<AiServiceTokenProvider>.Instance);

        var result = provider.CreateToken("tenant-1", "u-9", "ai:chat");
        var lifetime = result.ExpiresOn - System.DateTime.UtcNow;
        Assert.True(lifetime <= System.TimeSpan.FromMinutes(5) + System.TimeSpan.FromSeconds(2), "TTL must be clamped to 5 minutes");
    }

    [Fact]
    public void Platform_token_omits_tenant_claim()
    {
        var result = CreateProvider().CreateToken(null, "u-9", "ai:admin");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "tenantId");
    }

    [Fact]
    public void Each_token_has_a_unique_jti()
    {
        var p = CreateProvider();
        var a = p.CreateToken("tenant-1", "u", "ai:chat");
        var b = p.CreateToken("tenant-1", "u", "ai:chat");
        Assert.NotEqual(a.Jti, b.Jti);
    }
}
