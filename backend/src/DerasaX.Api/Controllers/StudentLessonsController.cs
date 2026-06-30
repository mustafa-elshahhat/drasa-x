using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions.Progress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    [ApiController]
    [Route("api/v1/student/lessons")]
    [Authorize(Policy = Policies.StudentOnly)]
    public class StudentLessonsController : ControllerBase
    {
        private readonly IStudentProgressService _progress;

        public StudentLessonsController(IStudentProgressService progress) => _progress = progress;

        [HttpGet("{lessonId}")]
        public async Task<IActionResult> Get(string lessonId, CancellationToken ct)
        {
            var result = await _progress.GetLessonDetailAsync(lessonId, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{lessonId}/complete")]
        public async Task<IActionResult> Complete(string lessonId, CancellationToken ct)
        {
            var result = await _progress.CompleteLessonAsync(lessonId, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
