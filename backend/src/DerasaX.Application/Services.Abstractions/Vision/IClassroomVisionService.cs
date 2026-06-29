using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.VisionDto;
using DerasaX.Domain.Common;

namespace DerasaX.Application.Services.Abstractions.Vision
{
    /// <summary>
    /// Computer-vision attendance + engagement orchestration (Phase 15). Backend is the
    /// system of record; the AI service is called via <c>IAiRagClient</c> (backend-mediated,
    /// scope ai:vision). CV NEVER auto-marks attendance: detections become review-required
    /// candidates that authorized staff confirm/reject/override. Tenant isolation is enforced
    /// by the global query filter plus explicit same-tenant student validation.
    /// </summary>
    public interface IClassroomVisionService
    {
        // staff (teacher / school-admin)
        Task<ApiResponse<VisionSessionDto>> StartSessionAsync(StartVisionSessionDto dto, CancellationToken ct = default);
        Task<ApiResponse<VisionSessionDto>> EndSessionAsync(string sessionId, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<VisionSessionDto>>> ListSessionsAsync(VisionSessionParameters p, CancellationToken ct = default);
        Task<ApiResponse<VisionSessionDto>> GetSessionAsync(string sessionId, CancellationToken ct = default);

        Task<ApiResponse<FrameAnalysisResultDto>> AnalyzeFrameAsync(string sessionId, AnalyzeFrameDto dto, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<FrameAnalysisDto>>> ListFrameAnalysesAsync(string sessionId, PaginationParameters p, CancellationToken ct = default);

        Task<PaginationResponse<IEnumerable<AttendanceCandidateDto>>> ListCandidatesAsync(string sessionId, VisionCandidateParameters p, CancellationToken ct = default);
        Task<ApiResponse<AttendanceCandidateDto>> ConfirmCandidateAsync(string candidateId, ConfirmCandidateDto dto, CancellationToken ct = default);
        Task<ApiResponse<AttendanceCandidateDto>> RejectCandidateAsync(string candidateId, RejectCandidateDto dto, CancellationToken ct = default);
        Task<ApiResponse<AttendanceCandidateDto>> OverrideCandidateAsync(string candidateId, OverrideCandidateDto dto, CancellationToken ct = default);

        Task<ApiResponse<SessionSummaryDto>> GetSessionSummaryAsync(string sessionId, CancellationToken ct = default);

        // identity mapping (enrollment)
        Task<ApiResponse<FaceEnrollmentDto>> EnrollFaceAsync(EnrollFaceDto dto, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<FaceEnrollmentDto>>> ListEnrollmentsAsync(PaginationParameters p, CancellationToken ct = default);

        // Phase 16 — optional CONSENTED durable enrollment asset (default OFF; never raw frames).
        Task<ApiResponse<bool>> AttachEnrollmentAssetAsync(string enrollmentId, string fileRecordId,
            bool consentObtained, string? consentReference, System.DateTime? retentionUntil, CancellationToken ct = default);
        Task<string> GetAuthorizedEnrollmentAssetIdAsync(string enrollmentId, CancellationToken ct = default);

        // student / parent read-only
        Task<ApiResponse<StudentEngagementSummaryDto>> MyEngagementSummaryAsync(CancellationToken ct = default);
        Task<ApiResponse<StudentEngagementSummaryDto>> ChildEngagementSummaryAsync(string childId, CancellationToken ct = default);
    }
}
