using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.CommunicationDto;
using DerasaX.Application.Services.Abstractions.Communication;
using DerasaX.Api.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 §12 (Increment 5) — parent↔teacher conversations and messaging. Who may contact
    /// whom is gated by a real relationship (a linked child enrolled in the teacher's class).
    /// Only participants may read or post; non-participants and cross-tenant ids resolve to 404.
    /// </summary>
    [ApiController]
    [Route("api/v1/conversations")]
    [Authorize(Policy = Policies.TenantMember)]
    [EnableRateLimiting(RateLimitPolicies.Messaging)]
    public class ConversationsController : ControllerBase
    {
        private readonly IConversationService _service;
        public ConversationsController(IConversationService service) => _service = service;

        [HttpPost]
        public async Task<IActionResult> Start([FromBody] StartConversationDto dto, CancellationToken ct)
            => R(await _service.StartAsync(dto, ct));

        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
            => R(await _service.ListAsync(ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, CancellationToken ct)
            => R(await _service.GetAsync(id, ct));

        [HttpGet("{id}/participants")]
        public async Task<IActionResult> Participants(string id, CancellationToken ct)
            => R(await _service.ParticipantsAsync(id, ct));

        [HttpPost("{id}/messages")]
        public async Task<IActionResult> PostMessage(string id, [FromBody] PostMessageDto dto, CancellationToken ct)
            => R(await _service.PostMessageAsync(id, dto, ct));

        [HttpGet("{id}/messages")]
        public async Task<IActionResult> ListMessages(string id, [FromQuery] MessageParameters p, CancellationToken ct)
            => R(await _service.ListMessagesAsync(id, p, ct));

        [HttpPost("{id}/messages/{messageId}/read")]
        public async Task<IActionResult> MarkRead(string id, string messageId, CancellationToken ct)
            => R(await _service.MarkReadAsync(id, messageId, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
