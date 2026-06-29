using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;

namespace DerasaX.Application.Services.Operations
{
    /// <summary>
    /// Safe file METADATA contracts only — the Phase 16 production storage provider and signed
    /// delivery URLs are out of scope here. A record is created after an approved upload
    /// initialization; the persisted <c>StorageKey</c> is an opaque, tenant-scoped key, never an
    /// insecure permanent public URL. Records are tenant-isolated and soft-archivable.
    /// </summary>
    public class FileMetadataService : OperationsServiceBase, IFileMetadataService
    {
        private const long MaxFileBytes = 100L * 1024 * 1024; // 100 MB metadata guard

        public FileMetadataService(IUnitOfWork uow, ITenantContext tenant, IAuditWriter audit) : base(uow, tenant, audit) { }

        public async Task<ApiResponse<FileRecordDto>> CreateAsync(CreateFileRecordDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var caller = RequireUser();
            if (string.IsNullOrWhiteSpace(dto.FileName) || string.IsNullOrWhiteSpace(dto.ContentType))
                throw new BadRequestException("FileName and ContentType are required.");
            if (dto.SizeBytes <= 0 || dto.SizeBytes > MaxFileBytes)
                throw new BadRequestException($"SizeBytes must be between 1 and {MaxFileBytes}.");

            // Opaque tenant-scoped storage key — NOT a public URL.
            var storageKey = $"tenants/{tenantId}/{Guid.NewGuid():N}/{dto.FileName}";
            var record = new FileRecord
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, FileName = dto.FileName, ContentType = dto.ContentType,
                SizeBytes = dto.SizeBytes, Type = dto.Type, ChecksumSha256 = dto.ChecksumSha256, StorageKey = storageKey,
                UploadedByUserId = caller
            };
            await UnitOfWork.Repository<FileRecord, string>().AddAsync(record);
            await Audit.StageAsync(AuditActionType.Create, nameof(FileRecord), record.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(record, false), 201, "File record created.");
        }

        public async Task<PaginationResponse<IEnumerable<FileRecordDto>>> ListAsync(FileParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            Expression<Func<FileRecord, bool>> criteria = f => !p.Type.HasValue || f.Type == p.Type.Value;
            var repo = UnitOfWork.Repository<FileRecord, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<FileRecord, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<FileRecord, string>(criteria, f => f.CreatedAt, p.PageNumber, p.PageSize, descending: true));
            return new PaginationResponse<IEnumerable<FileRecordDto>>(items.Select(f => Map(f, false)).ToList(), total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "File records retrieved." };
        }

        public async Task<ApiResponse<FileRecordDto>> GetAsync(string id, CancellationToken ct = default)
        {
            RequireTenant();
            var record = await UnitOfWork.Repository<FileRecord, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<FileRecord, string>(f => f.Id == id)) ?? throw new NotFoundException("File record not found.");
            return Ok(Map(record, false));
        }

        public async Task<ApiResponse<bool>> ArchiveAsync(string id, CancellationToken ct = default)
        {
            RequireTenant();
            var record = await UnitOfWork.Repository<FileRecord, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<FileRecord, string>(f => f.Id == id)) ?? throw new NotFoundException("File record not found.");
            record.IsDeleted = true; // soft archive
            UnitOfWork.Repository<FileRecord, string>().Update(record);
            await Audit.StageAsync(AuditActionType.Delete, nameof(FileRecord), record.Id, "{\"action\":\"archive\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(true, 200, "File record archived.");
        }

        private static FileRecordDto Map(FileRecord f, bool archived) => new()
        {
            Id = f.Id, FileName = f.FileName, ContentType = f.ContentType, SizeBytes = f.SizeBytes,
            Type = f.Type, IsArchived = f.IsDeleted || archived, CreatedAt = f.CreatedAt, StorageKey = f.StorageKey
        };
    }
}
