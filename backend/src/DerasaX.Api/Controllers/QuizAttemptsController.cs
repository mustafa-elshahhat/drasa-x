using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.AssessmentDto;
using DerasaX.Application.Services.Abstractions.Assessment;
using DerasaX.Api.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 §10 — student-facing assessment endpoints: assigned-quiz discovery and
    /// the attempt lifecycle (start → save answers → submit → history/result). Eligibility
    /// derives from real enrollment/assignment relationships; the backend computes the
    /// authoritative score (the client can never submit or override its own grade).
    /// </summary>
    [ApiController]
    [Route("api/v1")]
    [Authorize(Policy = Policies.StudentOnly)]
    public class QuizAttemptsController : ControllerBase
    {
        private readonly IQuizAssignmentService _assignment;
        private readonly IQuizAttemptService _attempts;

        public QuizAttemptsController(IQuizAssignmentService assignment, IQuizAttemptService attempts)
        {
            _assignment = assignment;
            _attempts = attempts;
        }

        [HttpGet("assigned-quizzes")]
        public async Task<IActionResult> Assigned(CancellationToken ct)
            => Result(await _assignment.ListAssignedToMeAsync(ct));

        [HttpPost("quizzes/{id}/attempts")]
        public async Task<IActionResult> Start(string id, CancellationToken ct)
            => Result(await _attempts.StartAsync(id, ct));

        [HttpGet("attempts/{attemptId}")]
        public async Task<IActionResult> Get(string attemptId, CancellationToken ct)
            => Result(await _attempts.GetAsync(attemptId, ct));

        [HttpPut("attempts/{attemptId}/answers")]
        public async Task<IActionResult> SaveAnswers(string attemptId, [FromBody] SaveAnswersDto dto, CancellationToken ct)
            => Result(await _attempts.SaveAnswersAsync(attemptId, dto, ct));

        [HttpPost("attempts/{attemptId}/submit")]
        [EnableRateLimiting(RateLimitPolicies.Submission)]
        public async Task<IActionResult> Submit(string attemptId, CancellationToken ct)
            => Result(await _attempts.SubmitAsync(attemptId, ct));

        [HttpGet("quizzes/{id}/my-attempts")]
        public async Task<IActionResult> MyHistory(string id, CancellationToken ct)
            => Result(await _attempts.MyHistoryAsync(id, ct));

        [HttpGet("attempts/{attemptId}/result")]
        public async Task<IActionResult> MyResult(string attemptId, CancellationToken ct)
            => Result(await _attempts.MyResultAsync(attemptId, ct));

        private IActionResult Result<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
