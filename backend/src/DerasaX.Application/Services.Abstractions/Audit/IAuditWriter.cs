using System.Threading;
using System.Threading.Tasks;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Services.Abstractions.Audit
{
    /// <summary>
    /// Stages an audit record onto the current unit of work so it is persisted in the
    /// SAME transaction as the domain change it describes (Phase 5 §8.6). The actor,
    /// tenant and correlation id are taken from the trusted request context — never
    /// from the request body. Call <c>SaveChangesAsync</c> on the unit of work once,
    /// after staging both the domain entity and its audit record.
    /// </summary>
    public interface IAuditWriter
    {
        /// <param name="tenantOverride">
        /// For platform-scope (SystemAdmin) actions that have no tenant claim but operate on a
        /// specific tenant (e.g. suspend tenant X, assign a plan to tenant X), the affected tenant
        /// id so the audit row is attributed to — and visible under — that tenant. When null the
        /// trusted tenant claim is used (the normal tenant-member path).
        /// </param>
        Task StageAsync(AuditActionType action, string entityType, string? entityId,
            string? metadataJson = null, CancellationToken ct = default, string? tenantOverride = null);
    }
}
