using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.ProgressDto;
using DerasaX.Application.Services.Abstractions.Progress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    [ApiController]
    [Route("api/v1/student/attendance")]
    [Authorize(Policy = Policies.StudentOnly)]
    public class StudentAttendanceController : ControllerBase
    {
        private readonly IStudentAttendanceService _service;

        public StudentAttendanceController(IStudentAttendanceService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Mine([FromQuery] ProgressParameters p, CancellationToken ct)
        {
            var result = await _service.MyAttendanceAsync(p, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
