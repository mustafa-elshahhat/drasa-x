using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.AssessmentDto;
using DerasaX.Application.Services.Abstractions.Assessment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 §10 — teacher/SchoolAdmin grading: review an individual submission,
    /// apply manual grades to free-text answers, and attach teacher feedback. Grading and
    /// feedback changes are audited and notify the student. Only a teacher authorized for
    /// the quiz's subject (or SchoolAdmin) may grade; otherwise 403.
    /// </summary>
    [ApiController]
    [Route("api/v1/submissions")]
    [Authorize(Policy = Policies.TeacherOrSchoolAdmin)]
    public class QuizGradingController : ControllerBase
    {
        private readonly IQuizGradingService _grading;
        public QuizGradingController(IQuizGradingService grading) => _grading = grading;

        [HttpGet("{attemptId}")]
        public async Task<IActionResult> Get(string attemptId, CancellationToken ct)
            => Result(await _grading.GetSubmissionAsync(attemptId, ct));

        [HttpPost("{attemptId}/grade")]
        public async Task<IActionResult> Grade(string attemptId, [FromBody] ManualGradeDto dto, CancellationToken ct)
            => Result(await _grading.GradeAsync(attemptId, dto, ct));

        [HttpPost("{attemptId}/feedback")]
        public async Task<IActionResult> Feedback(string attemptId, [FromBody] FeedbackDto dto, CancellationToken ct)
            => Result(await _grading.FeedbackAsync(attemptId, dto, ct));

        private IActionResult Result<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
