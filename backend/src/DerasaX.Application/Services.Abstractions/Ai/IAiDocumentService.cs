using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AiDto;

namespace DerasaX.Application.Services.Abstractions.Ai
{
    /// <summary>
    /// Backend-mediated curriculum document lifecycle (Phase 6 §7): authenticated
    /// ingestion / re-index / delete into the tenant-scoped RAG store. Tenant is
    /// derived from the access token; school-ai-rag performs chunking + embedding.
    /// </summary>
    public interface IAiDocumentService
    {
        Task<IngestResultDto> IngestAsync(IngestCurriculumDocumentDto request, CancellationToken ct = default);
        Task<DeleteResultDto> DeleteAsync(string documentId, CancellationToken ct = default);
    }
}
