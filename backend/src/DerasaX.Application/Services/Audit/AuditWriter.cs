using System;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DerasaX.Application.Services.Audit
{
    /// <inheritdoc />
    public class AuditWriter : IAuditWriter
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantContext _tenant;
        private readonly IHttpContextAccessor _http;

        public AuditWriter(IUnitOfWork unitOfWork, ITenantContext tenant, IHttpContextAccessor http)
        {
            _unitOfWork = unitOfWork;
            _tenant = tenant;
            _http = http;
        }

        public async Task StageAsync(AuditActionType action, string entityType, string? entityId,
            string? metadataJson = null, CancellationToken ct = default, string? tenantOverride = null)
        {
            var ctx = _http.HttpContext;
            string? correlationId = null;
            if (ctx is not null && ctx.Response.Headers.TryGetValue("X-Correlation-Id", out var cid))
                correlationId = cid;
            correlationId ??= ctx?.TraceIdentifier;

            // A platform-scope SystemAdmin (no tenant claim) acting on a specific tenant attributes
            // the audit to that tenant via tenantOverride. The Phase 4 same-tenant trigger requires
            // ActorUserId's tenant to equal the row tenant, which a tenant-less platform user can
            // never satisfy — so for that case the dedicated ActorUserId column is left NULL (the
            // trigger then skips) and the platform actor id is preserved in the metadata instead.
            var isPlatformOverride = tenantOverride is not null && _tenant.TenantId is null;
            var actorUserId = isPlatformOverride ? null : _tenant.UserId;
            var metadata = isPlatformOverride ? WithPlatformActor(metadataJson, _tenant.UserId) : metadataJson;

            var entry = new AuditLog
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantOverride ?? _tenant.TenantId,
                ActorUserId = actorUserId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                CorrelationId = Truncate(correlationId, 128),
                IpAddress = Truncate(ctx?.Connection?.RemoteIpAddress?.ToString(), 64),
                UserAgent = Truncate(ctx?.Request?.Headers["User-Agent"].ToString(), 512),
                MetadataJson = metadata,
                OccurredAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<AuditLog, string>().AddAsync(entry);
        }

        private static string? Truncate(string? value, int max) =>
            string.IsNullOrEmpty(value) ? value : (value.Length <= max ? value : value[..max]);

        // Preserves the platform actor id inside the audit metadata when the dedicated
        // ActorUserId column must be left null (see same-tenant-trigger note above).
        private static string WithPlatformActor(string? metadataJson, string? actor)
        {
            var pair = $"\"platformActorUserId\":\"{actor}\"";
            if (string.IsNullOrWhiteSpace(metadataJson)) return "{" + pair + "}";
            var trimmed = metadataJson.TrimStart();
            return trimmed.StartsWith("{") ? "{" + pair + "," + trimmed[1..] : metadataJson;
        }
    }
}
