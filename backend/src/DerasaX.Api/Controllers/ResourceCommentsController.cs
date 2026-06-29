using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.ResourceCommentDto;
using DerasaX.Application.Services.Abstractions.Engagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 closure — comments on lesson resources (curriculum materials). Any tenant member may
    /// read/post; authors edit/delete their own; Teacher/SchoolAdmin moderate. Cross-tenant ids 404.
    /// </summary>
    [ApiController]
    [Route("api/v1/lesson-materials/{materialId}/comments")]
    [Authorize(Policy = Policies.TenantMember)]
    public class ResourceCommentsController : ControllerBase
    {
        private readonly IResourceCommentService _service;
        public ResourceCommentsController(IResourceCommentService service) => _service = service;

        [HttpPost]
        public async Task<IActionResult> Create(string materialId, [FromBody] CreateResourceCommentDto dto, CancellationToken ct)
            => R(await _service.CreateAsync(materialId, dto, ct));

        [HttpGet]
        public async Task<IActionResult> List(string materialId, [FromQuery] ResourceCommentParameters p, CancellationToken ct)
        {
            var r = await _service.ListAsync(materialId, p, ct);
            return StatusCode(r.StatusCode, r);
        }

        [HttpPut("{commentId}")]
        public async Task<IActionResult> Update(string materialId, string commentId, [FromBody] UpdateResourceCommentDto dto, CancellationToken ct)
            => R(await _service.UpdateAsync(materialId, commentId, dto, ct));

        [HttpDelete("{commentId}")]
        public async Task<IActionResult> Delete(string materialId, string commentId, CancellationToken ct)
            => R(await _service.DeleteAsync(materialId, commentId, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
