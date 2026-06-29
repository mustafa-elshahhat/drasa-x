using System;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AiDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Ai;
using DerasaX.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace DerasaX.Application.Services.Ai
{
    /// <summary>
    /// Curriculum ingestion orchestration. Validates the caller's tenant context,
    /// forwards trusted document + curriculum metadata to school-ai-rag over the
    /// authenticated internal contract, and returns the index result. Tenant is
    /// taken from the access token, never the request body.
    /// </summary>
    public class AiDocumentService : IAiDocumentService
    {
        private const int MaxContentChars = 1_000_000;

        private readonly IAiRagClient _ai;
        private readonly ITenantContext _tenant;
        private readonly ILogger<AiDocumentService> _logger;

        public AiDocumentService(IAiRagClient ai, ITenantContext tenant, ILogger<AiDocumentService> logger)
        {
            _ai = ai;
            _tenant = tenant;
            _logger = logger;
        }

        public async Task<IngestResultDto> IngestAsync(IngestCurriculumDocumentDto request, CancellationToken ct = default)
        {
            var tenantId = _tenant.TenantId
                ?? throw new UnauthorizedException("Tenant context is required to ingest curriculum.");
            var userId = _tenant.UserId;

            if (string.IsNullOrWhiteSpace(request.DocumentId))
                throw new BadRequestException("DocumentId is required.");
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new BadRequestException("Document content is required.");
            if (request.Content.Length > MaxContentChars)
                throw new BadRequestException("Document content is too large.");
            if (request.Version < 1)
                throw new BadRequestException("Version must be >= 1.");

            var correlationId = Guid.NewGuid().ToString("N");
            var aiReq = new AiIngestRequest
            {
                CorrelationId = correlationId,
                DocumentId = request.DocumentId,
                Version = request.Version,
                Content = request.Content,
                Language = string.Equals(request.Language, "ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en",
                MaterialType = string.IsNullOrWhiteSpace(request.MaterialType) ? "other" : request.MaterialType,
                Title = request.Title,
                FileId = request.FileId,
                AcademicYear = request.AcademicYear,
                Grade = request.Grade,
                Subject = request.Subject,
                Unit = request.Unit,
                Lesson = request.Lesson,
            };

            var res = await _ai.IngestDocumentAsync(aiReq, tenantId, userId, ct);
            _logger.LogInformation("AI ingest ok. documentId={DocumentId} status={Status} chunks={Chunks} correlationId={CorrelationId}",
                res.DocumentId, res.Status, res.ChunkCount, correlationId);

            return new IngestResultDto
            {
                DocumentId = res.DocumentId, Version = res.Version, Status = res.Status,
                ChunkCount = res.ChunkCount, RemovedChunks = res.RemovedChunks, Checksum = res.Checksum,
                Language = res.Language, IndexedAt = res.IndexedAt, CorrelationId = res.CorrelationId,
            };
        }

        public async Task<DeleteResultDto> DeleteAsync(string documentId, CancellationToken ct = default)
        {
            var tenantId = _tenant.TenantId
                ?? throw new UnauthorizedException("Tenant context is required to delete curriculum.");
            if (string.IsNullOrWhiteSpace(documentId))
                throw new BadRequestException("documentId is required.");

            var correlationId = Guid.NewGuid().ToString("N");
            var res = await _ai.DeleteDocumentAsync(documentId, correlationId, tenantId, _tenant.UserId, ct);
            return new DeleteResultDto
            {
                DocumentId = res.DocumentId, DeletedChunks = res.DeletedChunks, Status = res.Status,
            };
        }
    }
}
