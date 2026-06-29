using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DerasaX.Application.Services.ServiceAuth
{
    /// <summary>
    /// Issues HMAC-SHA256 signed service tokens for school-ai-rag. The signing key is
    /// read from configuration (ServiceAuth__SigningKey) and is NEVER logged. Each call
    /// gets a fresh jti and an expiry of at most 5 minutes (clamped).
    /// </summary>
    public class AiServiceTokenProvider : IAiServiceTokenProvider
    {
        private const int MaxTtlSeconds = 300;

        private readonly ServiceAuthSettings _settings;
        private readonly ILogger<AiServiceTokenProvider> _logger;

        public AiServiceTokenProvider(IOptions<ServiceAuthSettings> options, ILogger<AiServiceTokenProvider> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public ServiceTokenResult CreateToken(string? tenantId, string? actorUserId, string scope)
        {
            if (string.IsNullOrEmpty(_settings.SigningKey))
                throw new InvalidOperationException("ServiceAuth signing key (ServiceAuth__SigningKey) is not configured.");

            var ttl = Math.Min(_settings.TtlSeconds <= 0 ? 120 : _settings.TtlSeconds, MaxTtlSeconds);
            var now = DateTime.UtcNow;
            var expires = now.AddSeconds(ttl);
            var jti = Guid.NewGuid().ToString("N");

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, _settings.Subject),
                new(JwtRegisteredClaimNames.Jti, jti),
                new("scope", scope ?? string.Empty)
            };
            if (!string.IsNullOrEmpty(tenantId))
                claims.Add(new Claim("tenantId", tenantId));
            if (!string.IsNullOrEmpty(actorUserId))
                claims.Add(new Claim("uid", actorUserId));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: creds);

            // kid header for key rotation (no secret material logged).
            jwt.Header["kid"] = _settings.KeyId;

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);

            _logger.LogInformation("AUDIT service_token.issued aud={Aud} iss={Iss} jti={Jti} scope={Scope} tenant={Tenant} ttl={Ttl}s",
                _settings.Audience, _settings.Issuer, jti, scope, tenantId, ttl);

            return new ServiceTokenResult
            {
                Token = token,
                Jti = jti,
                ExpiresOn = expires,
                Audience = _settings.Audience,
                Issuer = _settings.Issuer
            };
        }
    }
}
