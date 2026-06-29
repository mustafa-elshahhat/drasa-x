using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AssessmentDto;
using DerasaX.Domain.Common;

namespace DerasaX.Application.Services.Abstractions.Assessment
{
    /// <summary>
    /// Phase 5 closure — end-to-end homework / general (non-quiz) assignment lifecycle:
    /// teacher draft → edit → publish/assign → student listing → submission → teacher review →
    /// grade + feedback. Quiz assignment remains in <see cref="IQuizAssignmentService"/>; this
    /// service owns the standalone-task workflow the teacher user story requires.
    /// </summary>
    public interface IHomeworkService
    {
        // Teacher / SchoolAdmin
        Task<ApiResponse<HomeworkDto>> CreateDraftAsync(CreateHomeworkDto dto, CancellationToken ct = default);
        Task<ApiResponse<HomeworkDto>> UpdateAsync(string assignmentId, UpdateHomeworkDto dto, CancellationToken ct = default);
        Task<ApiResponse<HomeworkDto>> PublishAsync(string assignmentId, PublishHomeworkDto dto, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<HomeworkDto>>> ListMineAsync(CancellationToken ct = default);
        Task<ApiResponse<HomeworkDto>> GetAsync(string assignmentId, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<HomeworkSubmissionDto>>> ListSubmissionsAsync(string assignmentId, HomeworkSubmissionParameters p, CancellationToken ct = default);
        Task<ApiResponse<HomeworkSubmissionDto>> GradeAsync(string submissionId, GradeHomeworkDto dto, CancellationToken ct = default);

        // Student
        Task<ApiResponse<IEnumerable<AssignedHomeworkDto>>> ListAssignedAsync(CancellationToken ct = default);
        Task<ApiResponse<HomeworkSubmissionDto>> SubmitAsync(string assignmentId, SubmitHomeworkDto dto, CancellationToken ct = default);
        Task<ApiResponse<HomeworkSubmissionDto>> GetMySubmissionAsync(string assignmentId, CancellationToken ct = default);
    }
}
