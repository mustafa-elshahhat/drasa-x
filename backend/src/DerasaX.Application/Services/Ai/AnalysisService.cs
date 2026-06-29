using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.AiDto;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Ai;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Authorization;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Application.Services.Operations;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.Extensions.Logging;

namespace DerasaX.Application.Services.Ai
{
    /// <summary>
    /// Pain-point analysis orchestration. Teacher/SchoolAdmin only may generate;
    /// access via the student-access authorizer; non-diagnostic outputs persisted
    /// as <c>PainPoint</c> with mandatory review state + model/prompt versions;
    /// AiUsage on success and failure; parents get an approved-only safe projection.
    /// </summary>
    public class AnalysisService : OperationsServiceBase, IAnalysisService
    {
        private const string Provider = "groq";
        private const int MaxTurns = 40;
        private const int MaxContentChars = 4000;

        private readonly IAiRagClient _ai;
        private readonly IAiUsageService _usage;
        private readonly IStudentAccessAuthorizer _access;
        private readonly ILogger<AnalysisService> _logger;

        public AnalysisService(IUnitOfWork uow, ITenantContext tenant, IAuditWriter audit,
            IAiRagClient ai, IAiUsageService usage, IStudentAccessAuthorizer access, ILogger<AnalysisService> logger)
            : base(uow, tenant, audit)
        {
            _ai = ai;
            _usage = usage;
            _access = access;
            _logger = logger;
        }

        public async Task<AnalysisResultDto> GenerateAsync(GenerateAnalysisDto request, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            RequireStaff();
            if (string.IsNullOrWhiteSpace(request.StudentId))
                throw new BadRequestException("StudentId is required.");
            if (request.Conversation is null || request.Conversation.Count == 0)
                throw new BadRequestException("Conversation is required.");

            await _access.EnsureCanAccessStudentAsync(request.StudentId, ct);

            var correlationId = Guid.NewGuid().ToString("N");
            var aiReq = new AiAnalysisRequest
            {
                CorrelationId = correlationId,
                StudentRef = request.StudentId,
                Language = string.Equals(request.Language, "ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en",
                Subject = request.Subject,
                UnitId = request.UnitId,
                LessonId = request.LessonId,
                Conversation = request.Conversation
                    .Where(t => t is not null && !string.IsNullOrWhiteSpace(t.Content))
                    .Take(MaxTurns)
                    .Select(t => new AiAnalysisTurn
                    {
                        Role = string.Equals(t.Role, "assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user",
                        Content = t.Content.Length > MaxContentChars ? t.Content[..MaxContentChars] : t.Content,
                    }).ToList(),
            };
            if (aiReq.Conversation.Count == 0)
                throw new BadRequestException("Conversation has no usable content.");

            AiAnalysisResponse ai;
            try
            {
                ai = await _ai.AnalyzeAsync(aiReq, tenantId, Tenant.UserId, ct);
            }
            catch (AiServiceException ex)
            {
                await TryRecordUsageAsync(failed: true, correlationId, ex.Category, ct);
                throw;
            }

            if (string.IsNullOrWhiteSpace(ai.PainPointCategory) || string.IsNullOrWhiteSpace(ai.ModelVersion))
                throw new AiServiceException("bad_response", "The AI analysis was invalid.");

            var category = MapCategory(ai.PainPointCategory);
            var painPoint = new PainPoint
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                StudentId = request.StudentId,
                Category = category,
                Title = $"{category}{(string.IsNullOrWhiteSpace(request.Subject) ? "" : $" — {request.Subject}")}",
                Description = Trunc(ai.EvidenceSummary, 2048),
                Recommendation = Trunc(ai.Recommendation, 2048),
                ConfidenceScore = ClampConfidence(ai.Confidence),
                Escalation = MapEscalation(ai.EscalationLevel),
                ReviewStatus = HumanReviewStatus.Pending,           // mandatory human review
                AiProvider = Provider,
                ModelVersion = ai.ModelVersion,
                PromptVersion = ai.PromptVersion,
                CorrelationId = correlationId,
                IsResolved = false,
                DetectedAt = DateTime.UtcNow,
            };
            await UnitOfWork.Repository<PainPoint, string>().AddAsync(painPoint);
            await Audit.StageAsync(AuditActionType.Create, nameof(PainPoint), painPoint.Id,
                $"{{\"category\":\"{category}\",\"review\":\"Pending\"}}", ct);
            await UnitOfWork.SaveChangesAsync(ct);

            await TryRecordUsageAsync(failed: false, correlationId, "analysis", ct);

            return new AnalysisResultDto
            {
                PainPointId = painPoint.Id, StudentId = request.StudentId, Category = category.ToString(),
                Subject = request.Subject, Unit = request.UnitId, Lesson = request.LessonId,
                EvidenceSummary = painPoint.Description ?? string.Empty, Recommendation = painPoint.Recommendation ?? string.Empty,
                Confidence = painPoint.ConfidenceScore, EscalationLevel = painPoint.Escalation.ToString(),
                ReviewStatus = painPoint.ReviewStatus.ToString(), ModelVersion = ai.ModelVersion, PromptVersion = ai.PromptVersion,
                GeneratedAt = painPoint.DetectedAt, CorrelationId = correlationId,
            };
        }

        public async Task ReviewAsync(string painPointId, ReviewPainPointDto decision, CancellationToken ct = default)
        {
            RequireTenant();
            RequireStaff();
            var pp = await UnitOfWork.Repository<PainPoint, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<PainPoint, string>(p => p.Id == painPointId))
                ?? throw new NotFoundException("Pain point not found.");
            await _access.EnsureCanAccessStudentAsync(pp.StudentId, ct);

            pp.ReviewStatus = (decision.Decision ?? string.Empty).ToLowerInvariant() switch
            {
                "approve" => HumanReviewStatus.Approved,
                "reject" => HumanReviewStatus.Rejected,
                _ => throw new BadRequestException("Decision must be 'approve' or 'reject'.")
            };
            pp.ReviewedByTeacherId = RequireUser();
            pp.ReviewedAt = DateTime.UtcNow;
            await Audit.StageAsync(AuditActionType.Update, nameof(PainPoint), pp.Id,
                $"{{\"review\":\"{pp.ReviewStatus}\"}}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
        }

        public async Task<object> GetHistoryForCallerAsync(string studentId, CancellationToken ct = default)
        {
            RequireTenant();
            await _access.EnsureCanAccessStudentAsync(studentId, ct);

            var items = (await UnitOfWork.Repository<PainPoint, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<PainPoint, string>(p => p.StudentId == studentId)))
                .OrderByDescending(p => p.DetectedAt).ToList();

            // Parents get an APPROVED-only, internal-free safe projection.
            if (Tenant.Role == Roles.Parent)
            {
                return items.Where(p => p.ReviewStatus == HumanReviewStatus.Approved)
                    .Select(p => new ParentSafePainPointDto
                    {
                        Category = p.Category.ToString(), Recommendation = p.Recommendation,
                        ReviewStatus = p.ReviewStatus.ToString(), DetectedAt = p.DetectedAt,
                    }).ToList();
            }

            return items.Select(p => new PainPointReviewItemDto
            {
                Id = p.Id, Category = p.Category.ToString(), EvidenceSummary = p.Description,
                Recommendation = p.Recommendation, Confidence = p.ConfidenceScore,
                EscalationLevel = p.Escalation.ToString(), ReviewStatus = p.ReviewStatus.ToString(),
                ModelVersion = p.ModelVersion, PromptVersion = p.PromptVersion, DetectedAt = p.DetectedAt,
            }).ToList();
        }

        private void RequireStaff()
        {
            if (!(IsSchoolAdmin || Tenant.Role == Roles.Teacher))
                throw new ForbiddenException("Only a teacher or school administrator may perform this analysis operation.");
        }

        private async Task TryRecordUsageAsync(bool failed, string correlationId, string category, CancellationToken ct)
        {
            try
            {
                await _usage.RecordInternalAsync(new RecordAiUsageDto
                {
                    Kind = AiUsageKind.Recommendation, Provider = Provider, Failed = failed, CorrelationId = correlationId,
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI analysis usage recording failed. correlationId={CorrelationId} category={Category}", correlationId, category);
            }
        }

        private static string? Trunc(string? s, int n) => string.IsNullOrEmpty(s) ? s : (s.Length > n ? s[..n] : s);
        private static decimal ClampConfidence(double c) => (decimal)Math.Max(0d, Math.Min(1d, c));

        private static PainPointCategory MapCategory(string c) => c.Trim().ToLowerInvariant() switch
        {
            "concept" => PainPointCategory.Concept,
            "skill" => PainPointCategory.Skill,
            "attendance" => PainPointCategory.Attendance,
            "engagement" => PainPointCategory.Engagement,
            "assessment" => PainPointCategory.Assessment,
            _ => PainPointCategory.NoFinding,
        };

        private static EscalationLevel MapEscalation(string e) => e.Trim().ToLowerInvariant() switch
        {
            "monitor" => EscalationLevel.Monitor,
            "escalate" => EscalationLevel.Escalate,
            _ => EscalationLevel.None,
        };
    }
}
