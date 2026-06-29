using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.AiDto;
using DerasaX.Application.Dto.VisionDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Ai;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Authorization;
using DerasaX.Application.Services.Abstractions.Vision;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DerasaX.Application.Services.Vision
{
    /// <summary>
    /// Phase 15 — computer-vision attendance + engagement. Backend is the system of
    /// record; the AI service is called via <see cref="IAiRagClient"/> (backend-mediated).
    /// CV never auto-marks attendance — detections become review-required candidates.
    /// </summary>
    public class ClassroomVisionService : IClassroomVisionService
    {
        private const int MaxImageBase64Chars = 12_000_000;

        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;
        private readonly IAuditWriter _audit;
        private readonly IAiRagClient _ai;
        private readonly IStudentAccessAuthorizer _access;
        private readonly UserManager<ApplicationUser> _users;
        private readonly ILogger<ClassroomVisionService> _logger;

        public ClassroomVisionService(
            IUnitOfWork uow, ITenantContext tenant, IAuditWriter audit, IAiRagClient ai,
            IStudentAccessAuthorizer access, UserManager<ApplicationUser> users,
            ILogger<ClassroomVisionService> logger)
        {
            _uow = uow;
            _tenant = tenant;
            _audit = audit;
            _ai = ai;
            _access = access;
            _users = users;
            _logger = logger;
        }

        // =====================================================================
        // SESSION LIFECYCLE
        // =====================================================================
        public async Task<ApiResponse<VisionSessionDto>> StartSessionAsync(StartVisionSessionDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var userId = RequireUser();

            if (dto.RecognitionThreshold is < 0 or > 1)
                throw new BadRequestException("RecognitionThreshold must be between 0 and 1.");

            if (!string.IsNullOrWhiteSpace(dto.SchoolClassId))
            {
                _ = await _uow.Repository<SchoolClass, string>().GetByIdWithSpecAsync(
                        new CriteriaSpecification<SchoolClass, string>(c => c.Id == dto.SchoolClassId))
                    ?? throw new NotFoundException("Class not found.");
            }

            var now = DateTime.UtcNow;
            var session = new ClassroomVisionSession
            {
                Id = Guid.NewGuid().ToString("N"),
                TenantId = tenantId,
                TeacherId = userId,
                SchoolClassId = string.IsNullOrWhiteSpace(dto.SchoolClassId) ? null : dto.SchoolClassId,
                SubjectId = string.IsNullOrWhiteSpace(dto.SubjectId) ? null : dto.SubjectId,
                LessonId = string.IsNullOrWhiteSpace(dto.LessonId) ? null : dto.LessonId,
                Title = string.IsNullOrWhiteSpace(dto.Title) ? null : dto.Title,
                Status = VisionSessionStatus.Active,
                StartedAt = now,
                SessionDate = dto.SessionDate.HasValue ? AsUtcDate(dto.SessionDate.Value) : AsUtcDate(now),
                RecognitionThreshold = dto.RecognitionThreshold ?? 0.5m,
                Notes = dto.Notes,
            };

            await _uow.Repository<ClassroomVisionSession, string>().AddAsync(session);
            await _audit.StageAsync(AuditActionType.Create, nameof(ClassroomVisionSession), session.Id,
                JsonSerializer.Serialize(new { session.SchoolClassId, session.LessonId }), ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("CV session {Id} started by {User} for class {Class}", session.Id, userId, session.SchoolClassId);
            return Ok(MapSession(session), 201, "Computer-vision session started.");
        }

        public async Task<ApiResponse<VisionSessionDto>> EndSessionAsync(string sessionId, CancellationToken ct = default)
        {
            var session = await LoadOwnedSessionAsync(sessionId);
            if (session.Status != VisionSessionStatus.Ended)
            {
                session.Status = VisionSessionStatus.Ended;
                session.EndedAt = DateTime.UtcNow;
                _uow.Repository<ClassroomVisionSession, string>().Update(session);
            }

            await RecomputeSummaryAsync(session, ct);
            await _audit.StageAsync(AuditActionType.Update, nameof(ClassroomVisionSession), session.Id,
                JsonSerializer.Serialize(new { action = "end" }), ct);
            await _uow.SaveChangesAsync(ct);

            // Best-effort: clear the AI service's ephemeral buffers. Never fail the request on this.
            try
            {
                await _ai.EndVisionSessionAsync(
                    new AiVisionEndSessionRequest { CorrelationId = Guid.NewGuid().ToString("N"), SessionId = session.Id },
                    session.TenantId!, _tenant.UserId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CV end-session buffer clear failed for {Session} (non-fatal)", session.Id);
            }

            var counts = await CandidateCountsAsync(session.Id);
            return Ok(MapSession(session, counts), 200, "Computer-vision session ended.");
        }

        public async Task<PaginationResponse<IEnumerable<VisionSessionDto>>> ListSessionsAsync(VisionSessionParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            var userId = RequireUser();
            var restrictToOwn = !IsAdmin;
            DateTime? from = p.From.HasValue ? AsUtc(p.From.Value) : null;
            DateTime? to = p.To.HasValue ? AsUtc(p.To.Value) : null;

            Expression<Func<ClassroomVisionSession, bool>> criteria = s =>
                (!restrictToOwn || s.TeacherId == userId) &&
                (string.IsNullOrEmpty(p.SchoolClassId) || s.SchoolClassId == p.SchoolClassId) &&
                (!p.Status.HasValue || s.Status == p.Status.Value) &&
                (from == null || s.SessionDate >= from) &&
                (to == null || s.SessionDate <= to);

            var repo = _uow.Repository<ClassroomVisionSession, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<ClassroomVisionSession, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<ClassroomVisionSession, string>(criteria, s => s.StartedAt, p.PageNumber, p.PageSize, descending: true));

            var dto = items.Select(s => MapSession(s)).ToList();
            return new PaginationResponse<IEnumerable<VisionSessionDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Computer-vision sessions retrieved." };
        }

        public async Task<ApiResponse<VisionSessionDto>> GetSessionAsync(string sessionId, CancellationToken ct = default)
        {
            var session = await LoadOwnedSessionAsync(sessionId);
            var counts = await CandidateCountsAsync(session.Id);
            return Ok(MapSession(session, counts), 200, "Computer-vision session retrieved.");
        }

        // =====================================================================
        // FRAME ANALYSIS (backend-mediated AI call)
        // =====================================================================
        public async Task<ApiResponse<FrameAnalysisResultDto>> AnalyzeFrameAsync(string sessionId, AnalyzeFrameDto dto, CancellationToken ct = default)
        {
            var session = await LoadOwnedSessionAsync(sessionId);
            if (session.Status != VisionSessionStatus.Active)
                throw new ConflictException("The session is not active; start a new session to analyze frames.");

            if (string.IsNullOrWhiteSpace(dto.ImageBase64))
                throw new BadRequestException("An image frame is required.");
            if (dto.ImageBase64.Length > MaxImageBase64Chars)
                throw new BadRequestException("The image frame is too large.");

            var tenantId = session.TenantId!;
            var correlationId = Guid.NewGuid().ToString("N");
            var aiRequest = new AiVisionAnalyzeRequest
            {
                CorrelationId = correlationId,
                SessionId = session.Id,
                ImageBase64 = dto.ImageBase64,
                FrameIndex = dto.FrameIndex ?? session.FrameCount,
                CaptureLabel = dto.CaptureLabel,
                WantEngagement = dto.WantEngagement ?? true,
                RecognitionThreshold = (double)session.RecognitionThreshold,
            };

            // Backend-mediated AI call. A provider/timeout/non-2xx failure surfaces as a
            // stable AiServiceException -> 502 AI_UNAVAILABLE (honest, never a fake success).
            var ai = await _ai.AnalyzeVisionFrameAsync(aiRequest, tenantId, _tenant.UserId, ct);

            var engineKind = ParseEngine(ai.Engine);
            var now = DateTime.UtcNow;

            var frame = new ClassroomVisionFrameAnalysis
            {
                Id = Guid.NewGuid().ToString("N"),
                TenantId = tenantId,
                SessionId = session.Id,
                FrameIndex = aiRequest.FrameIndex,
                CaptureLabel = dto.CaptureLabel,
                FacesDetected = ai.FacesDetected,
                EngineKind = engineKind,
                Degraded = ai.Degraded,
                ModelVersion = Trunc(ai.ModelVersion, 64),
                CorrelationId = correlationId,
                QualityFlags = ai.QualityFlags.Count == 0 ? null : Trunc(string.Join(",", ai.QualityFlags), 512),
                AnalyzedAt = now,
            };
            await _uow.Repository<ClassroomVisionFrameAnalysis, string>().AddAsync(frame);

            var candidatesThisCall = new Dictionary<string, AttendanceDetectionCandidate>();
            var candidateRepo = _uow.Repository<AttendanceDetectionCandidate, string>();
            var enrollmentRepo = _uow.Repository<StudentFaceEnrollment, string>();
            var results = new List<VisionFaceResultDto>();
            var mappedStudentIds = new List<string>();

            foreach (var face in ai.Results)
            {
                var trackId = string.IsNullOrWhiteSpace(face.TrackId) ? Guid.NewGuid().ToString("N") : face.TrackId;
                var recConf = (decimal)Math.Clamp(face.RecognitionConfidence, 0.0, 1.0);
                var engagement = ParseEngagement(face.Engagement);

                // Identity mapping (enrollment): opaque external label -> tenant student.
                string? mappedStudentId = null;
                if (!string.IsNullOrWhiteSpace(face.ExternalLabelId))
                {
                    var enrollment = await enrollmentRepo.GetByIdWithSpecAsync(
                        new CriteriaSpecification<StudentFaceEnrollment, string>(e =>
                            e.ExternalLabelId == face.ExternalLabelId && e.IsActive));
                    mappedStudentId = enrollment?.StudentId;
                }
                if (mappedStudentId != null) mappedStudentIds.Add(mappedStudentId);

                // Persist the engagement/emotion observation (analytics signal).
                await _uow.Repository<StudentEngagementObservation, string>().AddAsync(new StudentEngagementObservation
                {
                    Id = Guid.NewGuid().ToString("N"),
                    TenantId = tenantId,
                    SessionId = session.Id,
                    FrameAnalysisId = frame.Id,
                    TrackId = Trunc(trackId, 128)!,
                    ExternalLabelId = Trunc(face.ExternalLabelId, 128),
                    StudentId = mappedStudentId,
                    Emotion = Trunc(face.Emotion, 32) ?? "Unknown",
                    EmotionConfidence = (decimal)Math.Clamp(face.EmotionConfidence, 0.0, 1.0),
                    Engagement = engagement,
                    EngagementConfidence = (decimal)Math.Clamp(face.EngagementConfidence, 0.0, 1.0),
                    EngagementFrames = face.EngagementFrames,
                    EngagementReady = engagement != EngagementLabel.NotReady,
                    EngineKind = engineKind,
                    Degraded = ai.Degraded,
                    ObservedAt = now,
                });

                // Upsert the attendance candidate (one per session+track).
                var candidate = await UpsertCandidateAsync(candidateRepo, candidatesThisCall, session, trackId,
                    face, recConf, mappedStudentId, ai.Degraded, now);

                results.Add(new VisionFaceResultDto
                {
                    TrackId = trackId,
                    Bbox = face.Bbox ?? new List<int>(),
                    ExternalLabelId = face.ExternalLabelId,
                    RecognitionConfidence = recConf,
                    RecognitionStatus = face.RecognitionStatus,
                    Emotion = face.Emotion,
                    EmotionConfidence = (decimal)Math.Clamp(face.EmotionConfidence, 0.0, 1.0),
                    Engagement = face.Engagement,
                    EngagementConfidence = (decimal)Math.Clamp(face.EngagementConfidence, 0.0, 1.0),
                    EngagementFrames = face.EngagementFrames,
                    EngagementFramesRequired = face.EngagementFramesRequired,
                    MappedStudentId = mappedStudentId,
                    CandidateId = candidate.Id,
                    QualityFlags = face.QualityFlags ?? new List<string>(),
                });
            }

            // Update session rollup.
            session.FrameCount += 1;
            session.EngineKind ??= engineKind;
            session.Degraded = session.Degraded || ai.Degraded;
            session.ModelVersion ??= Trunc(ai.ModelVersion, 64);
            _uow.Repository<ClassroomVisionSession, string>().Update(session);

            await _audit.StageAsync(AuditActionType.Create, nameof(ClassroomVisionFrameAnalysis), frame.Id,
                JsonSerializer.Serialize(new { session.Id, ai.FacesDetected, engine = ai.Engine, degraded = ai.Degraded }), ct);
            await _uow.SaveChangesAsync(ct);

            // Decorate mapped student names for the live review UI.
            if (mappedStudentIds.Count > 0)
            {
                var names = await LoadNamesAsync(mappedStudentIds);
                foreach (var r in results.Where(r => r.MappedStudentId != null))
                    r.MappedStudentName = names.GetValueOrDefault(r.MappedStudentId!);
            }

            return Ok(new FrameAnalysisResultDto
            {
                FrameAnalysisId = frame.Id,
                SessionId = session.Id,
                FrameIndex = frame.FrameIndex,
                FacesDetected = ai.FacesDetected,
                Engine = ai.Engine,
                Degraded = ai.Degraded,
                ModelVersion = ai.ModelVersion,
                Results = results,
            }, 200, "Frame analyzed.");
        }

        private async Task<AttendanceDetectionCandidate> UpsertCandidateAsync(
            Domain.Interfaces.RepositoriesInterfaces.IGenericRepository<AttendanceDetectionCandidate, string> repo,
            Dictionary<string, AttendanceDetectionCandidate> local,
            ClassroomVisionSession session, string trackId, AiVisionFaceResult face, decimal recConf,
            string? mappedStudentId, bool degraded, DateTime now)
        {
            if (!local.TryGetValue(trackId, out var candidate))
            {
                candidate = await repo.GetByIdWithSpecAsync(new CriteriaSpecification<AttendanceDetectionCandidate, string>(c =>
                    c.SessionId == session.Id && c.TrackId == trackId));
            }

            if (candidate is null)
            {
                candidate = new AttendanceDetectionCandidate
                {
                    Id = Guid.NewGuid().ToString("N"),
                    TenantId = session.TenantId,
                    SessionId = session.Id,
                    TrackId = Trunc(trackId, 128)!,
                    ExternalLabelId = Trunc(face.ExternalLabelId, 128),
                    MappedStudentId = mappedStudentId,
                    BestRecognitionConfidence = recConf,
                    RecognitionStatus = Trunc(face.RecognitionStatus, 32) ?? "unknown",
                    DetectionCount = 1,
                    FirstSeenAt = now,
                    LastSeenAt = now,
                    Degraded = degraded,
                    ReviewStatus = CandidateReviewStatus.Pending,
                };
                await repo.AddAsync(candidate);
                local[trackId] = candidate;
                return candidate;
            }

            // Update only while still pending (never disturb a reviewed candidate).
            if (candidate.ReviewStatus == CandidateReviewStatus.Pending)
            {
                candidate.DetectionCount += 1;
                candidate.LastSeenAt = now;
                if (recConf > candidate.BestRecognitionConfidence)
                {
                    candidate.BestRecognitionConfidence = recConf;
                    candidate.RecognitionStatus = Trunc(face.RecognitionStatus, 32) ?? candidate.RecognitionStatus;
                }
                candidate.MappedStudentId ??= mappedStudentId;
                if (!local.ContainsKey(trackId))
                    repo.Update(candidate);
            }
            local[trackId] = candidate;
            return candidate;
        }

        public async Task<PaginationResponse<IEnumerable<FrameAnalysisDto>>> ListFrameAnalysesAsync(string sessionId, PaginationParameters p, CancellationToken ct = default)
        {
            var session = await LoadOwnedSessionAsync(sessionId);
            Expression<Func<ClassroomVisionFrameAnalysis, bool>> criteria = f => f.SessionId == session.Id;
            var repo = _uow.Repository<ClassroomVisionFrameAnalysis, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<ClassroomVisionFrameAnalysis, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<ClassroomVisionFrameAnalysis, string>(criteria, f => f.AnalyzedAt, p.PageNumber, p.PageSize, descending: true));
            var dto = items.Select(f => new FrameAnalysisDto
            {
                Id = f.Id, FrameIndex = f.FrameIndex, CaptureLabel = f.CaptureLabel, FacesDetected = f.FacesDetected,
                EngineKind = f.EngineKind.ToString(), Degraded = f.Degraded, ModelVersion = f.ModelVersion, AnalyzedAt = f.AnalyzedAt
            }).ToList();
            return new PaginationResponse<IEnumerable<FrameAnalysisDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Frame analyses retrieved." };
        }

        // =====================================================================
        // ATTENDANCE CANDIDATE REVIEW
        // =====================================================================
        public async Task<PaginationResponse<IEnumerable<AttendanceCandidateDto>>> ListCandidatesAsync(string sessionId, VisionCandidateParameters p, CancellationToken ct = default)
        {
            var session = await LoadOwnedSessionAsync(sessionId);
            Expression<Func<AttendanceDetectionCandidate, bool>> criteria = c =>
                c.SessionId == session.Id && (!p.ReviewStatus.HasValue || c.ReviewStatus == p.ReviewStatus.Value);
            var repo = _uow.Repository<AttendanceDetectionCandidate, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<AttendanceDetectionCandidate, string>(criteria));
            var items = (await repo.GetAllWithSpecAsync(
                new PagedSpecification<AttendanceDetectionCandidate, string>(criteria, c => c.LastSeenAt, p.PageNumber, p.PageSize, descending: true))).ToList();

            var names = await LoadNamesAsync(items.SelectMany(c => new[] { c.MappedStudentId, c.ResolvedStudentId }).Where(x => x != null)!);
            var dto = items.Select(c => MapCandidate(c, names)).ToList();
            return new PaginationResponse<IEnumerable<AttendanceCandidateDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Attendance candidates retrieved." };
        }

        public async Task<ApiResponse<AttendanceCandidateDto>> ConfirmCandidateAsync(string candidateId, ConfirmCandidateDto dto, CancellationToken ct = default)
        {
            var (session, candidate) = await LoadCandidateAsync(candidateId);
            if (candidate.ReviewStatus != CandidateReviewStatus.Pending)
                throw new ConflictException("This candidate has already been reviewed.");

            var studentId = !string.IsNullOrWhiteSpace(dto.StudentId) ? dto.StudentId! : candidate.MappedStudentId;
            if (string.IsNullOrWhiteSpace(studentId))
                throw new BadRequestException("A student must be selected to confirm this candidate (unknown/low-confidence detection).");

            var status = ParseStatus(dto.Status);
            var record = await ResolveAttendanceAsync(session, studentId!, status, AttendanceSource.ComputerVision, dto.Notes, ct);

            candidate.ReviewStatus = CandidateReviewStatus.Confirmed;
            candidate.ResolvedStudentId = studentId;
            candidate.ResolvedStatus = status;
            candidate.AttendanceRecordId = record.Id;
            StampReview(candidate, dto.Notes);
            _uow.Repository<AttendanceDetectionCandidate, string>().Update(candidate);

            if (dto.Remember && !string.IsNullOrWhiteSpace(candidate.ExternalLabelId))
                await UpsertEnrollmentAsync(session.TenantId!, studentId!, candidate.ExternalLabelId!, null, "confirm-remember", ct);

            await _audit.StageAsync(AuditActionType.Update, nameof(AttendanceDetectionCandidate), candidate.Id,
                JsonSerializer.Serialize(new { action = "confirm", studentId, status = status.ToString(), attendanceRecordId = record.Id }), ct);
            await _uow.SaveChangesAsync(ct);

            var names = await LoadNamesAsync(new[] { studentId! });
            return Ok(MapCandidate(candidate, names), 200, "Attendance confirmed.");
        }

        public async Task<ApiResponse<AttendanceCandidateDto>> RejectCandidateAsync(string candidateId, RejectCandidateDto dto, CancellationToken ct = default)
        {
            var (_, candidate) = await LoadCandidateAsync(candidateId);
            if (candidate.ReviewStatus != CandidateReviewStatus.Pending)
                throw new ConflictException("This candidate has already been reviewed.");

            candidate.ReviewStatus = CandidateReviewStatus.Rejected;
            StampReview(candidate, dto.Notes);
            _uow.Repository<AttendanceDetectionCandidate, string>().Update(candidate);
            await _audit.StageAsync(AuditActionType.Update, nameof(AttendanceDetectionCandidate), candidate.Id,
                JsonSerializer.Serialize(new { action = "reject" }), ct);
            await _uow.SaveChangesAsync(ct);
            return Ok(MapCandidate(candidate, new Dictionary<string, string?>()), 200, "Candidate rejected.");
        }

        public async Task<ApiResponse<AttendanceCandidateDto>> OverrideCandidateAsync(string candidateId, OverrideCandidateDto dto, CancellationToken ct = default)
        {
            var (session, candidate) = await LoadCandidateAsync(candidateId);
            if (string.IsNullOrWhiteSpace(dto.StudentId))
                throw new BadRequestException("A student is required to override this candidate.");

            var status = ParseStatus(dto.Status);
            var record = await ResolveAttendanceAsync(session, dto.StudentId, status, AttendanceSource.ComputerVision, dto.Notes, ct);

            candidate.ReviewStatus = CandidateReviewStatus.Overridden;
            candidate.ResolvedStudentId = dto.StudentId;
            candidate.ResolvedStatus = status;
            candidate.AttendanceRecordId = record.Id;
            StampReview(candidate, dto.Notes);
            _uow.Repository<AttendanceDetectionCandidate, string>().Update(candidate);

            await _audit.StageAsync(AuditActionType.Update, nameof(AttendanceDetectionCandidate), candidate.Id,
                JsonSerializer.Serialize(new { action = "override", studentId = dto.StudentId, status = status.ToString(), attendanceRecordId = record.Id }), ct);
            await _uow.SaveChangesAsync(ct);

            var names = await LoadNamesAsync(new[] { dto.StudentId });
            return Ok(MapCandidate(candidate, names), 200, "Attendance overridden.");
        }

        /// <summary>Idempotent attendance upsert (Source=ComputerVision). The unique index
        /// (TenantId, StudentId, AttendanceDate, SessionKey) guarantees no duplicate row
        /// for the same student in the same CV session.</summary>
        private async Task<StudentAttendanceRecord> ResolveAttendanceAsync(
            ClassroomVisionSession session, string studentId, AttendanceStatus status, AttendanceSource source, string? notes, CancellationToken ct)
        {
            // Same-tenant student integrity (ApplicationUser has no tenant query filter).
            var student = await _users.Users.FirstOrDefaultAsync(u => u.Id == studentId, ct);
            if (student is null || student is not Student || student.TenantId != session.TenantId || student.IsDeleted)
                throw new NotFoundException("Student not found in this tenant.");

            var sessionKey = $"cv-{session.Id}";
            var date = AsUtcDate(session.SessionDate);
            var repo = _uow.Repository<StudentAttendanceRecord, string>();
            var existing = await repo.GetByIdWithSpecAsync(new CriteriaSpecification<StudentAttendanceRecord, string>(r =>
                r.StudentId == studentId && r.AttendanceDate == date && r.SessionKey == sessionKey));

            if (existing is not null)
            {
                existing.Status = status;
                existing.Source = source;
                existing.RecordedAt = DateTime.UtcNow;
                existing.SchoolClassId = session.SchoolClassId ?? existing.SchoolClassId;
                existing.Notes = notes ?? existing.Notes;
                repo.Update(existing);
                return existing;
            }

            var record = new StudentAttendanceRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                TenantId = session.TenantId,
                StudentId = studentId,
                SchoolClassId = session.SchoolClassId,
                SessionKey = sessionKey,
                AttendanceDate = date,
                Status = status,
                RecordedAt = DateTime.UtcNow,
                Source = source,
                Notes = notes,
            };
            await repo.AddAsync(record);
            return record;
        }

        // =====================================================================
        // SUMMARY / ANALYTICS
        // =====================================================================
        public async Task<ApiResponse<SessionSummaryDto>> GetSessionSummaryAsync(string sessionId, CancellationToken ct = default)
        {
            var session = await LoadOwnedSessionAsync(sessionId);
            var summary = await RecomputeSummaryAsync(session, ct);
            await _uow.SaveChangesAsync(ct);
            return Ok(MapSummary(summary), 200, "Session summary retrieved.");
        }

        private async Task<ClassroomVisionSessionSummary> RecomputeSummaryAsync(ClassroomVisionSession session, CancellationToken ct)
        {
            var frames = await _uow.Repository<ClassroomVisionFrameAnalysis, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<ClassroomVisionFrameAnalysis, string>(f => f.SessionId == session.Id));
            var observations = (await _uow.Repository<StudentEngagementObservation, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<StudentEngagementObservation, string>(o => o.SessionId == session.Id))).ToList();
            var candidates = (await _uow.Repository<AttendanceDetectionCandidate, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<AttendanceDetectionCandidate, string>(c => c.SessionId == session.Id))).ToList();

            var ready = observations.Where(o => o.EngagementReady).ToList();
            var summaryRepo = _uow.Repository<ClassroomVisionSessionSummary, string>();
            var summary = await summaryRepo.GetByIdWithSpecAsync(
                new CriteriaSpecification<ClassroomVisionSessionSummary, string>(s => s.SessionId == session.Id));
            var isNew = summary is null;
            summary ??= new ClassroomVisionSessionSummary
            {
                Id = Guid.NewGuid().ToString("N"),
                TenantId = session.TenantId,
                SessionId = session.Id,
            };

            summary.TotalFrames = frames.Count();
            summary.TotalFaceObservations = observations.Count;
            summary.DistinctTracks = candidates.Count;
            summary.PendingCandidates = candidates.Count(c => c.ReviewStatus == CandidateReviewStatus.Pending);
            summary.ConfirmedAttendance = candidates.Count(c => c.ReviewStatus == CandidateReviewStatus.Confirmed);
            summary.RejectedCandidates = candidates.Count(c => c.ReviewStatus == CandidateReviewStatus.Rejected);
            summary.OverriddenCandidates = candidates.Count(c => c.ReviewStatus == CandidateReviewStatus.Overridden);
            summary.EngagedObservations = observations.Count(o => o.Engagement == EngagementLabel.Engaged);
            summary.DisengagedObservations = observations.Count(o => o.Engagement == EngagementLabel.Disengaged);
            summary.NotReadyObservations = observations.Count(o => o.Engagement == EngagementLabel.NotReady);
            summary.AverageEngagementConfidence = ready.Count == 0 ? 0m : Math.Round(ready.Average(o => o.EngagementConfidence), 4);
            summary.Degraded = session.Degraded;
            summary.GeneratedAt = DateTime.UtcNow;

            if (isNew) await summaryRepo.AddAsync(summary);
            else summaryRepo.Update(summary);
            return summary;
        }

        // =====================================================================
        // ENROLLMENT (identity mapping)
        // =====================================================================
        public async Task<ApiResponse<FaceEnrollmentDto>> EnrollFaceAsync(EnrollFaceDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            if (string.IsNullOrWhiteSpace(dto.StudentId)) throw new BadRequestException("StudentId is required.");
            if (string.IsNullOrWhiteSpace(dto.ExternalLabelId)) throw new BadRequestException("ExternalLabelId is required.");

            var student = await _users.Users.FirstOrDefaultAsync(u => u.Id == dto.StudentId, ct);
            if (student is null || student is not Student || student.TenantId != tenantId || student.IsDeleted)
                throw new NotFoundException("Student not found in this tenant.");

            var enrollment = await UpsertEnrollmentAsync(tenantId, dto.StudentId, dto.ExternalLabelId, dto.DisplayLabel, "manual", ct);
            await _audit.StageAsync(AuditActionType.Create, nameof(StudentFaceEnrollment), enrollment.Id,
                JsonSerializer.Serialize(new { dto.StudentId, dto.ExternalLabelId }), ct);
            await _uow.SaveChangesAsync(ct);

            var names = await LoadNamesAsync(new[] { dto.StudentId });
            return Ok(MapEnrollment(enrollment, names), 201, "Face enrollment saved.");
        }

        public async Task<PaginationResponse<IEnumerable<FaceEnrollmentDto>>> ListEnrollmentsAsync(PaginationParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            var repo = _uow.Repository<StudentFaceEnrollment, string>();
            Expression<Func<StudentFaceEnrollment, bool>> criteria = e => e.IsActive;
            var total = await repo.CountAsync(new CriteriaSpecification<StudentFaceEnrollment, string>(criteria));
            var items = (await repo.GetAllWithSpecAsync(
                new PagedSpecification<StudentFaceEnrollment, string>(criteria, e => e.EnrolledAt, p.PageNumber, p.PageSize, descending: true))).ToList();
            var names = await LoadNamesAsync(items.Select(e => e.StudentId));
            var dto = items.Select(e => MapEnrollment(e, names)).ToList();
            return new PaginationResponse<IEnumerable<FaceEnrollmentDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Face enrollments retrieved." };
        }

        public async Task<ApiResponse<bool>> AttachEnrollmentAssetAsync(string enrollmentId, string fileRecordId,
            bool consentObtained, string? consentReference, DateTime? retentionUntil, CancellationToken ct = default)
        {
            RequireTenant();
            if (!consentObtained)
                throw new BadRequestException("Explicit consent is required to store a CV enrollment asset.");

            var repo = _uow.Repository<StudentFaceEnrollment, string>();
            var enrollment = await repo.GetByIdWithSpecAsync(
                new CriteriaSpecification<StudentFaceEnrollment, string>(e => e.Id == enrollmentId))
                ?? throw new NotFoundException("Enrollment not found.");

            enrollment.ConsentObtained = true;
            enrollment.ConsentReference = consentReference;
            enrollment.AssetRetentionUntil = retentionUntil;
            enrollment.FileRecordId = fileRecordId;
            repo.Update(enrollment);
            // Sensitive CV enrollment asset access (store) — audited.
            await _audit.StageAsync(AuditActionType.Update, nameof(StudentFaceEnrollment), enrollment.Id,
                "{\"action\":\"store-consented-asset\",\"sensitive\":true}", ct);
            await _uow.SaveChangesAsync(ct);
            return Ok(true, 200, "Consented enrollment asset stored.");
        }

        public async Task<string> GetAuthorizedEnrollmentAssetIdAsync(string enrollmentId, CancellationToken ct = default)
        {
            RequireTenant();
            var enrollment = await _uow.Repository<StudentFaceEnrollment, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<StudentFaceEnrollment, string>(e => e.Id == enrollmentId))
                ?? throw new NotFoundException("Enrollment not found.");
            if (string.IsNullOrEmpty(enrollment.FileRecordId))
                throw new NotFoundException("No consented asset is stored for this enrollment.");
            // Sensitive CV enrollment asset access (read) — audited.
            await _audit.StageAsync(AuditActionType.Export, nameof(StudentFaceEnrollment), enrollment.Id,
                "{\"action\":\"access-consented-asset\",\"sensitive\":true}", ct);
            await _uow.SaveChangesAsync(ct);
            return enrollment.FileRecordId!;
        }

        private async Task<StudentFaceEnrollment> UpsertEnrollmentAsync(string tenantId, string studentId, string externalLabelId, string? displayLabel, string source, CancellationToken ct)
        {
            var repo = _uow.Repository<StudentFaceEnrollment, string>();
            var existing = await repo.GetByIdWithSpecAsync(new CriteriaSpecification<StudentFaceEnrollment, string>(e => e.ExternalLabelId == externalLabelId));
            if (existing is not null)
            {
                existing.StudentId = studentId;
                existing.DisplayLabel = displayLabel ?? existing.DisplayLabel;
                existing.IsActive = true;
                existing.Source = source;
                repo.Update(existing);
                return existing;
            }
            var enrollment = new StudentFaceEnrollment
            {
                Id = Guid.NewGuid().ToString("N"),
                TenantId = tenantId,
                StudentId = studentId,
                ExternalLabelId = Trunc(externalLabelId, 128)!,
                DisplayLabel = Trunc(displayLabel, 128),
                IsActive = true,
                Source = source,
                EnrolledAt = DateTime.UtcNow,
            };
            await repo.AddAsync(enrollment);
            return enrollment;
        }

        // =====================================================================
        // STUDENT / PARENT READ-ONLY SUMMARIES
        // =====================================================================
        public async Task<ApiResponse<StudentEngagementSummaryDto>> MyEngagementSummaryAsync(CancellationToken ct = default)
        {
            RequireTenant();
            var studentId = _tenant.UserId ?? throw new UnauthorizedException("Authenticated student context is required.");
            return Ok(await BuildStudentSummaryAsync(studentId), 200, "Engagement summary retrieved.");
        }

        public async Task<ApiResponse<StudentEngagementSummaryDto>> ChildEngagementSummaryAsync(string childId, CancellationToken ct = default)
        {
            await _access.EnsureCanAccessStudentAsync(childId, ct); // relationship-authorized (parent ↔ child)
            return Ok(await BuildStudentSummaryAsync(childId), 200, "Engagement summary retrieved.");
        }

        private async Task<StudentEngagementSummaryDto> BuildStudentSummaryAsync(string studentId)
        {
            var observations = (await _uow.Repository<StudentEngagementObservation, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<StudentEngagementObservation, string>(o => o.StudentId == studentId))).ToList();
            var cvAttendance = await _uow.Repository<StudentAttendanceRecord, string>().CountAsync(
                new CriteriaSpecification<StudentAttendanceRecord, string>(a => a.StudentId == studentId && a.Source == AttendanceSource.ComputerVision));

            var ready = observations.Where(o => o.EngagementReady).ToList();
            return new StudentEngagementSummaryDto
            {
                StudentId = studentId,
                EngagedObservations = observations.Count(o => o.Engagement == EngagementLabel.Engaged),
                DisengagedObservations = observations.Count(o => o.Engagement == EngagementLabel.Disengaged),
                NotReadyObservations = observations.Count(o => o.Engagement == EngagementLabel.NotReady),
                AverageEngagementConfidence = ready.Count == 0 ? 0m : Math.Round(ready.Average(o => o.EngagementConfidence), 4),
                SessionsObserved = observations.Select(o => o.SessionId).Distinct().Count(),
                CvAttendanceCount = cvAttendance,
                LastObservedAt = observations.Count == 0 ? null : observations.Max(o => o.ObservedAt),
                Degraded = observations.Any(o => o.Degraded),
            };
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        private bool IsAdmin => string.Equals(_tenant.Role, Roles.SchoolAdmin, StringComparison.Ordinal);

        private string RequireTenant() => _tenant.TenantId ?? throw new UnauthorizedException("Tenant context is required for this operation.");
        private string RequireUser() => _tenant.UserId ?? throw new UnauthorizedException("Authenticated user context is required.");

        private async Task<ClassroomVisionSession> LoadOwnedSessionAsync(string sessionId)
        {
            RequireTenant();
            var userId = RequireUser();
            var session = await _uow.Repository<ClassroomVisionSession, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<ClassroomVisionSession, string>(s => s.Id == sessionId))
                ?? throw new NotFoundException("Computer-vision session not found.");
            // Teachers may only act on their own sessions; school admins on any tenant session.
            if (!IsAdmin && session.TeacherId != userId)
                throw new NotFoundException("Computer-vision session not found.");
            return session;
        }

        private async Task<(ClassroomVisionSession session, AttendanceDetectionCandidate candidate)> LoadCandidateAsync(string candidateId)
        {
            RequireTenant();
            var candidate = await _uow.Repository<AttendanceDetectionCandidate, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<AttendanceDetectionCandidate, string>(c => c.Id == candidateId))
                ?? throw new NotFoundException("Attendance candidate not found.");
            var session = await LoadOwnedSessionAsync(candidate.SessionId);
            return (session, candidate);
        }

        private async Task<(int pending, int confirmed, int rejected)> CandidateCountsAsync(string sessionId)
        {
            var repo = _uow.Repository<AttendanceDetectionCandidate, string>();
            var pending = await repo.CountAsync(new CriteriaSpecification<AttendanceDetectionCandidate, string>(c => c.SessionId == sessionId && c.ReviewStatus == CandidateReviewStatus.Pending));
            var confirmed = await repo.CountAsync(new CriteriaSpecification<AttendanceDetectionCandidate, string>(c => c.SessionId == sessionId && c.ReviewStatus == CandidateReviewStatus.Confirmed));
            var rejected = await repo.CountAsync(new CriteriaSpecification<AttendanceDetectionCandidate, string>(c => c.SessionId == sessionId && c.ReviewStatus == CandidateReviewStatus.Rejected));
            return (pending, confirmed, rejected);
        }

        private void StampReview(AttendanceDetectionCandidate candidate, string? notes)
        {
            candidate.ReviewedByUserId = _tenant.UserId;
            candidate.ReviewedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(notes)) candidate.ReviewNotes = Trunc(notes, 1024);
        }

        private async Task<Dictionary<string, string?>> LoadNamesAsync(IEnumerable<string> ids)
        {
            var list = ids.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            if (list.Count == 0) return new Dictionary<string, string?>();
            var users = await _users.Users.Where(u => list.Contains(u.Id)).Select(u => new { u.Id, u.FullName }).ToListAsync();
            return users.ToDictionary(u => u.Id, u => (string?)u.FullName);
        }

        private static VisionEngineKind ParseEngine(string engine) =>
            string.Equals(engine, "torch", StringComparison.OrdinalIgnoreCase) ? VisionEngineKind.Torch : VisionEngineKind.Stub;

        private static EngagementLabel ParseEngagement(string? label) =>
            Enum.TryParse<EngagementLabel>(label, ignoreCase: true, out var e) ? e : EngagementLabel.NotReady;

        private static AttendanceStatus ParseStatus(string? status) =>
            Enum.TryParse<AttendanceStatus>(status, ignoreCase: true, out var s) ? s : AttendanceStatus.Present;

        private static DateTime AsUtc(DateTime v) => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc);
        private static DateTime AsUtcDate(DateTime v) => DateTime.SpecifyKind(v.Date, DateTimeKind.Utc);
        private static string? Trunc(string? v, int max) => string.IsNullOrEmpty(v) ? v : (v.Length <= max ? v : v[..max]);

        private static ApiResponse<T> Ok<T>(T data, int status, string message) =>
            new(data) { Success = true, StatusCode = status, Message = message };

        private static VisionSessionDto MapSession(ClassroomVisionSession s, (int pending, int confirmed, int rejected)? counts = null) => new()
        {
            Id = s.Id, TeacherId = s.TeacherId, SchoolClassId = s.SchoolClassId, SubjectId = s.SubjectId, LessonId = s.LessonId,
            Title = s.Title, Status = s.Status.ToString(), StartedAt = s.StartedAt, EndedAt = s.EndedAt, SessionDate = s.SessionDate,
            FrameCount = s.FrameCount, RecognitionThreshold = s.RecognitionThreshold, EngineKind = s.EngineKind?.ToString(),
            Degraded = s.Degraded, ModelVersion = s.ModelVersion, Notes = s.Notes,
            PendingCandidates = counts?.pending ?? 0, ConfirmedAttendance = counts?.confirmed ?? 0, RejectedCandidates = counts?.rejected ?? 0,
        };

        private static AttendanceCandidateDto MapCandidate(AttendanceDetectionCandidate c, Dictionary<string, string?> names) => new()
        {
            Id = c.Id, SessionId = c.SessionId, TrackId = c.TrackId, ExternalLabelId = c.ExternalLabelId,
            MappedStudentId = c.MappedStudentId, MappedStudentName = c.MappedStudentId is null ? null : names.GetValueOrDefault(c.MappedStudentId),
            ResolvedStudentId = c.ResolvedStudentId, ResolvedStudentName = c.ResolvedStudentId is null ? null : names.GetValueOrDefault(c.ResolvedStudentId),
            BestRecognitionConfidence = c.BestRecognitionConfidence, RecognitionStatus = c.RecognitionStatus,
            DetectionCount = c.DetectionCount, FirstSeenAt = c.FirstSeenAt, LastSeenAt = c.LastSeenAt, Degraded = c.Degraded,
            ReviewStatus = c.ReviewStatus.ToString(), ReviewedByUserId = c.ReviewedByUserId, ReviewedAt = c.ReviewedAt,
            ReviewNotes = c.ReviewNotes, ResolvedStatus = c.ResolvedStatus?.ToString(), AttendanceRecordId = c.AttendanceRecordId,
        };

        private static SessionSummaryDto MapSummary(ClassroomVisionSessionSummary s) => new()
        {
            SessionId = s.SessionId, TotalFrames = s.TotalFrames, TotalFaceObservations = s.TotalFaceObservations,
            DistinctTracks = s.DistinctTracks, PendingCandidates = s.PendingCandidates, ConfirmedAttendance = s.ConfirmedAttendance,
            RejectedCandidates = s.RejectedCandidates, OverriddenCandidates = s.OverriddenCandidates,
            EngagedObservations = s.EngagedObservations, DisengagedObservations = s.DisengagedObservations,
            NotReadyObservations = s.NotReadyObservations, AverageEngagementConfidence = s.AverageEngagementConfidence,
            Degraded = s.Degraded, GeneratedAt = s.GeneratedAt,
        };

        private static FaceEnrollmentDto MapEnrollment(StudentFaceEnrollment e, Dictionary<string, string?> names) => new()
        {
            Id = e.Id, StudentId = e.StudentId, StudentName = names.GetValueOrDefault(e.StudentId),
            ExternalLabelId = e.ExternalLabelId, DisplayLabel = e.DisplayLabel, IsActive = e.IsActive,
            Source = e.Source, EnrolledAt = e.EnrolledAt,
        };
    }
}
