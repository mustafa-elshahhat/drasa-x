using DerasaX.Application.Services.Abstractions.Storage;
using DerasaX.Domain.Entities.Models;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Infrastructure.Storage
{
    /// <summary>
    /// Phase 16 — file lookup that bypasses the tenant query filter for the anonymous
    /// signed-download path only (see <see cref="IFileRecordLookup"/>). The HMAC token validation
    /// in the service is the authorization gate; this just resolves the row so the tenant embedded
    /// in the token can be re-verified against it.
    /// </summary>
    public sealed class FileRecordLookup : IFileRecordLookup
    {
        private readonly DerasaXDbContext _db;
        public FileRecordLookup(DerasaXDbContext db) => _db = db;

        public async Task<FileRecord?> FindGlobalAsync(string id, CancellationToken ct = default) =>
            await _db.fileRecords.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == id, ct);
    }
}
