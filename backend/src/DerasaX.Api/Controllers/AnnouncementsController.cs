using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.CommunicationDto;
using DerasaX.Application.Services.Abstractions.Communication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 §12 (Increment 5) — school announcements. SchoolAdmin authors/publishes; every
    /// tenant member lists the active announcements targeting their audience. Tenant-scoped.
    /// </summary>
    [ApiController]
    [Route("api/v1/announcements")]
    [Authorize(Policy = Policies.TenantMember)]
    public class AnnouncementsController : ControllerBase
    {
        private readonly IAnnouncementService _service;
        public AnnouncementsController(IAnnouncementService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] AnnouncementParameters p, CancellationToken ct)
            => R(await _service.ListAsync(p, ct));

        [HttpPost]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> Create([FromBody] CreateAnnouncementDto dto, CancellationToken ct)
            => R(await _service.CreateAsync(dto, ct));

        [HttpPost("{id}/publish")]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> Publish(string id, [FromQuery] bool publish, CancellationToken ct)
            => R(await _service.PublishAsync(id, publish, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
