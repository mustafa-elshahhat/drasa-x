using System.Threading;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;

namespace DerasaX.Application.Services.Abstractions.Storage
{
    /// <summary>
    /// Phase 16 — direct file-record lookup that bypasses the tenant query filter. Used ONLY by
    /// the anonymous signed-download path, AFTER the HMAC token signature + expiry have been
    /// validated (the signed token itself proves issuance for a specific file+tenant). The caller
    /// MUST re-check the tenant embedded in the token against the returned record's tenant.
    /// </summary>
    public interface IFileRecordLookup
    {
        Task<FileRecord?> FindGlobalAsync(string id, CancellationToken ct = default);
    }
}
