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
    /// Phase 5 §10 — quiz authoring, lifecycle, assignment, submission review and
    /// question-level analytics. Teacher/SchoolAdmin only; a teacher may only manage
    /// quizzes anchored to a subject they hold an active assignment for (else 403).
    /// Cross-tenant quiz ids resolve to a safe 404 via the global tenant filter.
    /// </summary>
    [ApiController]
    [Route("api/v1/quizzes")]
    [Authorize(Policy = Policies.TeacherOrSchoolAdmin)]
    public class QuizzesController : ControllerBase
    {
        private readonly IQuizAuthoringService _authoring;
        private readonly IQuizAssignmentService _assignment;
        private readonly IQuizGradingService _grading;

        public QuizzesController(IQuizAuthoringService authoring, IQuizAssignmentService assignment, IQuizGradingService grading)
        {
            _authoring = authoring;
            _assignment = assignment;
            _grading = grading;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] QuizParameters parameters, CancellationToken ct)
            => Result(await _authoring.ListAsync(parameters, ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, CancellationToken ct)
            => Result(await _authoring.GetByIdAsync(id, ct));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AddQuizDto dto, CancellationToken ct)
            => Result(await _authoring.CreateAsync(dto, ct));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateQuizDto dto, CancellationToken ct)
        {
            dto.Id = id;
            return Result(await _authoring.UpdateAsync(dto, ct));
        }

        [HttpPost("{id}/questions")]
        public async Task<IActionResult> AddQuestion(string id, [FromBody] AddQuestionDto dto, CancellationToken ct)
            => Result(await _authoring.AddQuestionAsync(id, dto, ct));

        [HttpPut("{id}/questions/{questionId}")]
        public async Task<IActionResult> UpdateQuestion(string id, string questionId, [FromBody] UpdateQuestionDto dto, CancellationToken ct)
        {
            dto.Id = questionId;
            return Result(await _authoring.UpdateQuestionAsync(id, dto, ct));
        }

        [HttpDelete("{id}/questions/{questionId}")]
        public async Task<IActionResult> DeleteQuestion(string id, string questionId, CancellationToken ct)
            => Result(await _authoring.DeleteQuestionAsync(id, questionId, ct));

        [HttpPost("{id}/publish")]
        public async Task<IActionResult> Publish(string id, CancellationToken ct)
            => Result(await _authoring.PublishAsync(id, ct));

        [HttpPost("{id}/archive")]
        public async Task<IActionResult> Archive(string id, CancellationToken ct)
            => Result(await _authoring.ArchiveAsync(id, ct));

        [HttpPost("{id}/assignments")]
        public async Task<IActionResult> Assign(string id, [FromBody] AssignQuizDto dto, CancellationToken ct)
            => Result(await _assignment.AssignAsync(id, dto, ct));

        [HttpGet("{id}/assignments")]
        public async Task<IActionResult> ListAssignments(string id, CancellationToken ct)
            => Result(await _assignment.ListForQuizAsync(id, ct));

        [HttpGet("{id}/submissions")]
        public async Task<IActionResult> ListSubmissions(string id, [FromQuery] AttemptParameters parameters, CancellationToken ct)
            => Result(await _grading.ListSubmissionsAsync(id, parameters, ct));

        [HttpGet("{id}/analytics")]
        public async Task<IActionResult> Analytics(string id, CancellationToken ct)
            => Result(await _grading.AnalyticsAsync(id, ct));

        private IActionResult Result<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
