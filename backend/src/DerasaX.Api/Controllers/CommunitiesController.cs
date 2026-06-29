using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.EngagementDto;
using DerasaX.Application.Services.Abstractions.Engagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 §13 (Increment 6) — communities: lifecycle, membership, posts, comments, reporting
    /// and moderation. Tenant-scoped; membership gates posting/commenting; owners/moderators (and
    /// SchoolAdmin) moderate; authors own their content. Archive is a safe soft-delete.
    /// </summary>
    [ApiController]
    [Route("api/v1")]
    [Authorize(Policy = Policies.TenantMember)]
    public class CommunitiesController : ControllerBase
    {
        private readonly ICommunityService _service;
        public CommunitiesController(ICommunityService service) => _service = service;

        [HttpGet("communities")]
        public async Task<IActionResult> List([FromQuery] CommunityParameters p, CancellationToken ct) => R(await _service.ListAsync(p, ct));

        [HttpGet("communities/{id}")]
        public async Task<IActionResult> Get(string id, CancellationToken ct) => R(await _service.GetAsync(id, ct));

        [HttpPost("communities")]
        public async Task<IActionResult> Create([FromBody] CreateCommunityDto dto, CancellationToken ct) => R(await _service.CreateAsync(dto, ct));

        [HttpPut("communities/{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateCommunityDto dto, CancellationToken ct) => R(await _service.UpdateAsync(id, dto, ct));

        [HttpPost("communities/{id}/archive")]
        public async Task<IActionResult> Archive(string id, CancellationToken ct) => R(await _service.ArchiveAsync(id, ct));

        [HttpGet("communities/{id}/members")]
        public async Task<IActionResult> Members(string id, CancellationToken ct) => R(await _service.MembersAsync(id, ct));

        [HttpPost("communities/{id}/join")]
        public async Task<IActionResult> Join(string id, CancellationToken ct) => R(await _service.JoinAsync(id, ct));

        [HttpPost("communities/{id}/leave")]
        public async Task<IActionResult> Leave(string id, CancellationToken ct) => R(await _service.LeaveAsync(id, ct));

        [HttpPost("communities/{id}/members")]
        public async Task<IActionResult> AddMember(string id, [FromBody] AddMemberDto dto, CancellationToken ct) => R(await _service.AddMemberAsync(id, dto, ct));

        [HttpPost("communities/{id}/posts")]
        public async Task<IActionResult> CreatePost(string id, [FromBody] CreatePostDto dto, CancellationToken ct) => R(await _service.CreatePostAsync(id, dto, ct));

        [HttpGet("communities/{id}/posts")]
        public async Task<IActionResult> ListPosts(string id, [FromQuery] CommunityParameters p, CancellationToken ct) => R(await _service.ListPostsAsync(id, p, ct));

        [HttpDelete("posts/{postId}")]
        public async Task<IActionResult> DeletePost(string postId, CancellationToken ct) => R(await _service.DeletePostAsync(postId, ct));

        [HttpPost("posts/{postId}/comments")]
        public async Task<IActionResult> Comment(string postId, [FromBody] CreateCommentDto dto, CancellationToken ct) => R(await _service.CommentAsync(postId, dto, ct));

        [HttpDelete("comments/{commentId}")]
        public async Task<IActionResult> DeleteComment(string commentId, CancellationToken ct) => R(await _service.DeleteCommentAsync(commentId, ct));

        [HttpPost("posts/{postId}/reports")]
        public async Task<IActionResult> Report(string postId, [FromBody] ReportPostDto dto, CancellationToken ct) => R(await _service.ReportPostAsync(postId, dto, ct));

        [HttpPost("posts/{postId}/moderate")]
        public async Task<IActionResult> Moderate(string postId, [FromBody] ModeratePostDto dto, CancellationToken ct) => R(await _service.ModeratePostAsync(postId, dto, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
