using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.ResourceCommentDto;
using DerasaX.Domain.Common;

namespace DerasaX.Application.Services.Abstractions.Engagement
{
    /// <summary>
    /// Phase 5 closure — comments on lesson resources (curriculum materials), distinct from
    /// community post comments. Tenant members read/post; authors edit/delete their own;
    /// Teacher/SchoolAdmin moderate (delete any). Cross-tenant material/comment ids resolve to 404.
    /// </summary>
    public interface IResourceCommentService
    {
        Task<ApiResponse<ResourceCommentDto>> CreateAsync(string materialId, CreateResourceCommentDto dto, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<ResourceCommentDto>>> ListAsync(string materialId, ResourceCommentParameters p, CancellationToken ct = default);
        Task<ApiResponse<ResourceCommentDto>> UpdateAsync(string materialId, string commentId, UpdateResourceCommentDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> DeleteAsync(string materialId, string commentId, CancellationToken ct = default);
    }
}
