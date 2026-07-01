using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AiDto;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Ai;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Application.Services.Assessment;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.Extensions.Logging;

namespace DerasaX.Application.Services.Ai
{
    /// <summary>
    /// AI quiz-draft orchestration. Verifies teacher/curriculum access, calls the
    /// internal AI contract for a grounded draft, revalidates the entire response,
    /// and persists it into the existing Phase 5 quiz domain as a Draft /
    /// Origin=AiGenerated quiz (questions, options, correct answers, QuizGeneration
    /// provenance) plus an AiUsageRecord on success and failure. Teacher review is
    /// required before publishing; nothing is auto-published or auto-assigned.
    /// </summary>
    public class QuizDraftService : AssessmentServiceBase, IQuizDraftService
    {
        private const int MaxQuestions = 20;
        private const string Provider = "groq";
        private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase) { "mcq", "true_false" };

        private readonly IAiRagClient _ai;
        private readonly IAiUsageService _usage;
        private readonly ILogger<QuizDraftService> _logger;
        private readonly IPlanLimitEnforcer _limits;

        public QuizDraftService(IUnitOfWork uow, ITenantContext tenant, IAuditWriter audit,
            IAiRagClient ai, IAiUsageService usage, ILogger<QuizDraftService> logger, IPlanLimitEnforcer limits) : base(uow, tenant, audit)
        {
            _ai = ai;
            _usage = usage;
            _logger = logger;
            _limits = limits;
        }

        public async Task<QuizDraftResultDto> GenerateDraftAsync(GenerateQuizDraftDto request, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            await _limits.EnsureWithinAiMonthlyQuotaAsync(tenantId, ct);

            if (string.IsNullOrWhiteSpace(request.SubjectId))
                throw new BadRequestException("SubjectId is required.");
            if (request.NumQuestions < 1 || request.NumQuestions > MaxQuestions)
                throw new BadRequestException($"NumQuestions must be between 1 and {MaxQuestions}.");
            var requestedTypes = (request.QuestionTypes is { Count: > 0 } ? request.QuestionTypes : new List<string> { "mcq" })
                .Select(t => t.ToLowerInvariant()).Distinct().ToList();
            if (requestedTypes.Any(t => !SupportedTypes.Contains(t)))
                throw new BadRequestException("Unsupported question type requested.");
            var difficulty = NormalizeDifficulty(request.Difficulty);

            // Subject must exist in the caller's tenant (cross-tenant id → 404).
            var subject = await UnitOfWork.Repository<Subject, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<Subject, string>(s => s.Id == request.SubjectId))
                ?? throw new NotFoundException("Subject not found.");

            // Teacher may only generate for a subject they hold an active assignment for;
            // SchoolAdmin may generate for any same-tenant subject.
            if (IsTeacher)
            {
                var userId = RequireUser();
                var assigned = await UnitOfWork.Repository<TeacherSubjectAssignment, string>()
                    .CountAsync(new CriteriaSpecification<TeacherSubjectAssignment, string>(a =>
                        a.TeacherId == userId && a.SubjectId == request.SubjectId && a.IsActive));
                if (assigned == 0)
                    throw new ForbiddenException("You are not an assigned teacher for the requested subject.");
            }
            else if (!IsSchoolAdmin)
            {
                throw new ForbiddenException("Only an assigned teacher or a school administrator may generate a quiz draft.");
            }

            var correlationId = Guid.NewGuid().ToString("N");
            var aiReq = new AiQuizDraftRequest
            {
                CorrelationId = correlationId,
                NumQuestions = request.NumQuestions,
                Language = string.Equals(request.Language, "ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en",
                Grade = request.Grade,
                Subject = subject.Name,
                UnitId = request.Unit,
                LessonId = request.LessonId,
                Topic = request.Topic,
                Difficulty = difficulty.ToString().ToLowerInvariant(),
                QuestionTypes = requestedTypes,
            };

            AiQuizDraftResponse ai;
            try
            {
                ai = await _ai.QuizDraftAsync(aiReq, tenantId, Tenant.UserId, ct);
            }
            catch (AiServiceException ex)
            {
                await TryRecordUsageAsync(model: null, failed: true, correlationId, ex.Category, ct);
                throw;
            }

            // Defense in depth: revalidate the entire AI response before persisting.
            try
            {
                ValidateDraft(ai, request.NumQuestions, requestedTypes);
            }
            catch (BadRequestException)
            {
                await TryRecordUsageAsync(model: ai.Model, failed: true, correlationId, "bad_draft", ct);
                throw new AiServiceException("bad_response", "The AI draft failed validation.");
            }

            // Persist into the existing Phase 5 quiz domain (Draft / Origin=AiGenerated).
            var quiz = new Quiz
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                Title = string.IsNullOrWhiteSpace(ai.Draft.Title) ? $"{subject.Name} draft" : ai.Draft.Title,
                Type = QuizType.Lesson,
                Difficulty = difficulty,
                TimeLimitMinutes = 30,
                SubjectId = request.SubjectId,
                LessonId = request.LessonId,
                Status = QuizStatus.Draft,
                Origin = QuizOrigin.AiGenerated
            };
            await UnitOfWork.Repository<Quiz, string>().AddAsync(quiz);

            var order = 0;
            foreach (var q in ai.Draft.Questions)
            {
                var qType = MapType(q.QuestionType);
                var question = new Question
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    QuizId = quiz.Id,
                    Text = q.QuestionText,
                    Type = qType,
                    Order = order++,
                    Points = q.Points,
                    Explanation = q.Explanation,
                    CorrectAnswerText = null // MCQ/TrueFalse correctness is stored on the option
                };
                await UnitOfWork.Repository<Question, string>().AddAsync(question);

                for (var i = 0; i < q.Options.Count; i++)
                {
                    await UnitOfWork.Repository<QuestionOption, string>().AddAsync(new QuestionOption
                    {
                        Id = Guid.NewGuid().ToString(),
                        TenantId = tenantId,
                        QuestionId = question.Id,
                        Text = q.Options[i],
                        IsCorrect = i == q.CorrectIndex
                    });
                }
            }

            var generation = new QuizGeneration
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                QuizId = quiz.Id,
                PromptUsed = $"prompt={ai.PromptVersion}; difficulty={difficulty}; types={string.Join(',', requestedTypes)}; n={ai.Draft.QuestionCount}",
                AiProvider = ai.Provider,
                AiModel = ai.Model,
                ModelVersion = ai.ModelVersion,
                PromptVersion = ai.PromptVersion,
                CorrelationId = correlationId,
                Status = QuizGenerationStatus.Pending
            };
            await UnitOfWork.Repository<QuizGeneration, string>().AddAsync(generation);

            await Audit.StageAsync(AuditActionType.Create, nameof(Quiz), quiz.Id, "{\"origin\":\"AiGenerated\",\"status\":\"Draft\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);

            await TryRecordUsageAsync(model: ai.Model, failed: false, correlationId, "quiz_generation", ct);

            return new QuizDraftResultDto
            {
                QuizId = quiz.Id,
                QuizGenerationId = generation.Id,
                Status = quiz.Status.ToString(),
                Origin = quiz.Origin.ToString(),
                Title = quiz.Title ?? string.Empty,
                QuestionCount = ai.Draft.QuestionCount,
                Grounded = ai.Grounded,
                CitationCount = ai.Citations?.Count ?? 0,
                Provider = ai.Provider,
                Model = ai.Model,
                PromptVersion = ai.PromptVersion,
                CorrelationId = correlationId
            };
        }

        private void ValidateDraft(AiQuizDraftResponse ai, int expected, List<string> allowedTypes)
        {
            if (ai.Draft?.Questions is null || ai.Draft.Questions.Count == 0)
                throw new BadRequestException("Draft has no questions.");
            if (ai.Draft.Questions.Count != expected)
                throw new BadRequestException("Draft question count mismatch.");
            if (!ai.Grounded)
                throw new BadRequestException("Draft is not grounded.");

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var q in ai.Draft.Questions)
            {
                if (!SupportedTypes.Contains(q.QuestionType) || !allowedTypes.Contains(q.QuestionType.ToLowerInvariant()))
                    throw new BadRequestException("Unsupported question type in draft.");
                if (string.IsNullOrWhiteSpace(q.QuestionText))
                    throw new BadRequestException("Empty question text.");
                if (q.Options is null || q.Options.Count < 2)
                    throw new BadRequestException("Question needs at least two options.");
                if (q.Options.Select(o => o.Trim().ToLowerInvariant()).Distinct().Count() != q.Options.Count)
                    throw new BadRequestException("Duplicate options.");
                if (q.CorrectIndex < 0 || q.CorrectIndex >= q.Options.Count)
                    throw new BadRequestException("Missing/invalid correct answer.");
                if (q.Points < 1 || q.Points > 10)
                    throw new BadRequestException("Invalid points.");
                if (!seen.Add(q.QuestionText.Trim().ToLowerInvariant()))
                    throw new BadRequestException("Duplicate question.");
            }
        }

        private async Task TryRecordUsageAsync(string? model, bool failed, string correlationId, string category, CancellationToken ct)
        {
            try
            {
                await _usage.RecordInternalAsync(new RecordAiUsageDto
                {
                    Kind = AiUsageKind.QuizGeneration,
                    Provider = Provider,
                    Model = model,
                    Failed = failed,
                    CorrelationId = correlationId
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI quiz usage recording failed. correlationId={CorrelationId} category={Category}", correlationId, category);
            }
        }

        private static DifficultyLevel NormalizeDifficulty(string? value) => (value ?? "core").Trim().ToLowerInvariant() switch
        {
            "remedial" => DifficultyLevel.Remedial,
            "advanced" => DifficultyLevel.Advanced,
            _ => DifficultyLevel.Core
        };

        private static QuestionType MapType(string type) => type.Trim().ToLowerInvariant() switch
        {
            "true_false" => QuestionType.TrueFalse,
            _ => QuestionType.MCQ
        };
    }
}
