using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.ResourceCommentDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Engagement;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;

namespace DerasaX.Application.Services.Engagement
{
    public class ResourceCommentService : IResourceCommentService
    {
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;
        private readonly IAuditWriter _audit;

        public ResourceCommentService(IUnitOfWork uow, ITenantContext tenant, IAuditWriter audit)
        {
            _uow = uow;
            _tenant = tenant;
            _audit = audit;
        }

        private string RequireTenant() =>
            _tenant.TenantId ?? throw new UnauthorizedException("Tenant context is required for this operation.");
        private string RequireUser() =>
            _tenant.UserId ?? throw new UnauthorizedException("Authenticated user is required for this operation.");
        private bool IsModerator => _tenant.Role == Roles.Teacher || _tenant.Role == Roles.SchoolAdmin;

        public async Task<ApiResponse<ResourceCommentDto>> CreateAsync(string materialId, CreateResourceCommentDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var userId = RequireUser();
            if (string.IsNullOrWhiteSpace(dto.Body)) throw new BadRequestException("Comment body is required.");
            if (dto.Body.Length > 2048) throw new BadRequestException("Comment body exceeds the 2048 character limit.");

            await RequireMaterialAsync(materialId); // cross-tenant/unknown → 404

            var comment = new LessonMaterialComment
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                MaterialId = materialId,
                UserId = userId,
                Body = dto.Body.Trim()
            };
            await _uow.Repository<LessonMaterialComment, string>().AddAsync(comment);
            await _audit.StageAsync(AuditActionType.Create, nameof(LessonMaterialComment), comment.Id,
                $"{{\"materialId\":\"{materialId}\"}}", ct);
            await _uow.SaveChangesAsync(ct);

            return new ApiResponse<ResourceCommentDto>(true, 201, "Comment posted.", Map(comment));
        }

        public async Task<PaginationResponse<IEnumerable<ResourceCommentDto>>> ListAsync(string materialId, ResourceCommentParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            await RequireMaterialAsync(materialId);

            var repo = _uow.Repository<LessonMaterialComment, string>();
            System.Linq.Expressions.Expression<Func<LessonMaterialComment, bool>> criteria = c => c.MaterialId == materialId;
            var total = await repo.CountAsync(new CriteriaSpecification<LessonMaterialComment, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<LessonMaterialComment, string>(criteria, c => c.CreatedAt, p.PageNumber, p.PageSize, descending: true));

            var dto = items.Select(Map).ToList();
            return new PaginationResponse<IEnumerable<ResourceCommentDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Comments retrieved." };
        }

        public async Task<ApiResponse<ResourceCommentDto>> UpdateAsync(string materialId, string commentId, UpdateResourceCommentDto dto, CancellationToken ct = default)
        {
            var userId = RequireUser();
            if (string.IsNullOrWhiteSpace(dto.Body)) throw new BadRequestException("Comment body is required.");
            if (dto.Body.Length > 2048) throw new BadRequestException("Comment body exceeds the 2048 character limit.");

            var comment = await RequireCommentAsync(materialId, commentId);
            // Only the author may edit their own comment (moderators delete, they do not rewrite).
            if (comment.UserId != userId)
                throw new ForbiddenException("You may only edit your own comment.");

            comment.Body = dto.Body.Trim();
            _uow.Repository<LessonMaterialComment, string>().Update(comment);
            await _audit.StageAsync(AuditActionType.Update, nameof(LessonMaterialComment), comment.Id, ct: ct);
            await _uow.SaveChangesAsync(ct);
            return new ApiResponse<ResourceCommentDto>(true, 200, "Comment updated.", Map(comment));
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string materialId, string commentId, CancellationToken ct = default)
        {
            var userId = RequireUser();
            var comment = await RequireCommentAsync(materialId, commentId);
            // Author may delete their own; Teacher/SchoolAdmin may moderate any comment in-tenant.
            if (comment.UserId != userId && !IsModerator)
                throw new ForbiddenException("You may only delete your own comment.");

            comment.IsDeleted = true;
            _uow.Repository<LessonMaterialComment, string>().Update(comment);
            await _audit.StageAsync(AuditActionType.Delete, nameof(LessonMaterialComment), comment.Id,
                comment.UserId != userId ? "{\"action\":\"moderate-delete\"}" : null, ct);
            await _uow.SaveChangesAsync(ct);
            return new ApiResponse<bool>(true, 200, "Comment deleted.", true);
        }

        // ---- helpers ----

        private async Task<LessonMaterial> RequireMaterialAsync(string materialId)
        {
            RequireTenant();
            return await _uow.Repository<LessonMaterial, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<LessonMaterial, string>(m => m.Id == materialId))
                ?? throw new NotFoundException("Lesson material not found.");
        }

        private async Task<LessonMaterialComment> RequireCommentAsync(string materialId, string commentId)
        {
            RequireTenant();
            await RequireMaterialAsync(materialId);
            return await _uow.Repository<LessonMaterialComment, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<LessonMaterialComment, string>(c => c.Id == commentId && c.MaterialId == materialId))
                ?? throw new NotFoundException("Comment not found.");
        }

        private static ResourceCommentDto Map(LessonMaterialComment c) => new()
        {
            Id = c.Id, MaterialId = c.MaterialId, UserId = c.UserId, Body = c.Body,
            CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt
        };
    }
}
