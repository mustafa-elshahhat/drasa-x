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
    /// Phase 5 §13 (Increment 6) — office hours: a teacher schedules/updates/cancels sessions;
    /// students discover availability and book (capacity + duplicate-booking + schedule rules);
    /// both sides may cancel. Bookings/cancellations are audited and notified. Tenant-scoped.
    /// </summary>
    [ApiController]
    [Route("api/v1")]
    [Authorize(Policy = Policies.TenantMember)]
    public class OfficeHoursController : ControllerBase
    {
        private readonly IOfficeHourService _service;
        public OfficeHoursController(IOfficeHourService service) => _service = service;

        [HttpPost("office-hours")]
        public async Task<IActionResult> Create([FromBody] CreateOfficeHourDto dto, CancellationToken ct) => R(await _service.CreateAsync(dto, ct));

        [HttpPut("office-hours/{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateOfficeHourDto dto, CancellationToken ct) => R(await _service.UpdateAsync(id, dto, ct));

        [HttpPost("office-hours/{id}/cancel")]
        public async Task<IActionResult> Cancel(string id, CancellationToken ct) => R(await _service.CancelAsync(id, ct));

        [HttpGet("office-hours/mine")]
        public async Task<IActionResult> Mine(CancellationToken ct) => R(await _service.MineAsync(ct));

        [HttpGet("office-hours/available")]
        public async Task<IActionResult> Available(CancellationToken ct) => R(await _service.AvailableAsync(ct));

        [HttpPost("office-hours/{id}/bookings")]
        public async Task<IActionResult> Book(string id, [FromBody] BookOfficeHourDto dto, CancellationToken ct) => R(await _service.BookAsync(id, dto, ct));

        [HttpGet("office-hours/{id}/bookings")]
        public async Task<IActionResult> SessionBookings(string id, CancellationToken ct) => R(await _service.SessionBookingsAsync(id, ct));

        [HttpPost("bookings/{bookingId}/cancel")]
        public async Task<IActionResult> CancelBooking(string bookingId, CancellationToken ct) => R(await _service.CancelBookingAsync(bookingId, ct));

        [HttpPost("bookings/{bookingId}/attendance")]
        public async Task<IActionResult> MarkAttendance(string bookingId, [FromBody] MarkAttendanceDto dto, CancellationToken ct) => R(await _service.MarkAttendanceAsync(bookingId, dto, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
