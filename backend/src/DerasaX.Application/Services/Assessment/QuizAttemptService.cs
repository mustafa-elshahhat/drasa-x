using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AssessmentDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Assessment;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;

namespace DerasaX.Application.Services.Assessment
{
    public class QuizAttemptService : AssessmentServiceBase, IQuizAttemptService
    {
        public QuizAttemptService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit)
            : base(unitOfWork, tenant, audit) { }

        public async Task<ApiResponse<AttemptDetailDto>> StartAsync(string quizId, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var studentId = RequireUser();

            var quiz = await UnitOfWork.Repository<Quiz, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<Quiz, string>(q => q.Id == quizId))
                ?? throw new NotFoundException("Quiz not found.");

            // Eligibility from a REAL assignment relationship (unassigned → 403).
            var assignment = await ResolveAssignmentAsync(quizId, studentId, ct)
                ?? throw new ForbiddenException("This quiz is not assigned to you.");

            if (quiz.Status != QuizStatus.Published)
                throw new ConflictException("This quiz is not available for attempts.");

            var now = DateTime.UtcNow;
            if (assignment.availableFrom is { } af && af > now)
                throw new ConflictException("This quiz is not yet available.");
            if (assignment.dueDate is { } dd && dd < now)
                throw new ConflictException("The deadline for this quiz has passed.");

            var attempts = (await UnitOfWork.Repository<QuizSubmission, string>()
                .GetAllWithSpecAsync(new CriteriaSpecification<QuizSubmission, string>(s =>
                    s.QuizId == quizId && s.StudentId == studentId))).ToList();

            // Resume an existing in-progress attempt (idempotent start).
            var inProgress = attempts.FirstOrDefault(s => s.submissionStatus == SubmissionStatus.InProgress);
            if (inProgress is not null)
            {
                var answersR = await LoadAnswersAsync(inProgress.Id);
                return Ok(await BuildDetailAsync(inProgress, answersR, includeGrade: false), 200, "Resumed in-progress attempt.");
            }

            var used = attempts.Count(s => s.submissionStatus != SubmissionStatus.InProgress);
            if (quiz.MaxAttempts is { } max && used >= max)
                throw new ConflictException("You have reached the maximum number of attempts for this quiz.");

            // Mark prior attempts non-latest.
            foreach (var prior in attempts.Where(a => a.IsLatestAttempt))
            {
                prior.IsLatestAttempt = false;
                UnitOfWork.Repository<QuizSubmission, string>().Update(prior);
            }

            var attempt = new QuizSubmission
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                QuizId = quizId,
                StudentId = studentId,
                AssignmentId = assignment.assignmentId,
                AttemptNumber = attempts.Count + 1,
                IsLatestAttempt = true,
                submissionStatus = SubmissionStatus.InProgress,
                StartedAt = now,
                AchievedScore = 0,
                TotalScore = 0
            };
            await UnitOfWork.Repository<QuizSubmission, string>().AddAsync(attempt);
            await Audit.StageAsync(AuditActionType.Create, nameof(QuizSubmission), attempt.Id, "{\"action\":\"start-attempt\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);

            return Ok(await BuildDetailAsync(attempt, new List<SubmissionAnswer>(), includeGrade: false), 201, "Attempt started.");
        }

        public async Task<ApiResponse<AttemptDetailDto>> GetAsync(string attemptId, CancellationToken ct = default)
        {
            var attempt = await LoadOwnAttemptAsync(attemptId);
            var answers = await LoadAnswersAsync(attempt.Id);
            var released = attempt.submissionStatus != SubmissionStatus.InProgress;
            return Ok(await BuildDetailAsync(attempt, answers, includeGrade: released), 200, "Attempt retrieved.");
        }

        public async Task<ApiResponse<AttemptDetailDto>> SaveAnswersAsync(string attemptId, SaveAnswersDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var attempt = await LoadOwnAttemptAsync(attemptId);
            if (attempt.submissionStatus != SubmissionStatus.InProgress)
                throw new ConflictException("This attempt has already been submitted and can no longer be modified.");

            var questions = await LoadQuizQuestionsAsync(attempt.QuizId);
            var questionIds = questions.Select(q => q.Id).ToHashSet();
            var existing = await LoadAnswersAsync(attempt.Id);

            foreach (var ans in dto.Answers)
            {
                if (!questionIds.Contains(ans.QuestionId))
                    throw new BadRequestException("Answer references a question that is not part of this quiz.");
                var question = questions.First(q => q.Id == ans.QuestionId);
                if (!string.IsNullOrEmpty(ans.SelectedOptionId) && question.Options.All(o => o.Id != ans.SelectedOptionId))
                    throw new BadRequestException("Selected option does not belong to the question.");

                var prior = existing.FirstOrDefault(a => a.QuestionId == ans.QuestionId);
                if (prior is null)
                {
                    await UnitOfWork.Repository<SubmissionAnswer, string>().AddAsync(new SubmissionAnswer
                    {
                        Id = Guid.NewGuid().ToString(),
                        TenantId = tenantId,
                        QuizSubmissionId = attempt.Id,
                        QuestionId = ans.QuestionId,
                        SelectedOptionId = ans.SelectedOptionId,
                        AnswerText = ans.AnswerText
                    });
                }
                else
                {
                    prior.SelectedOptionId = ans.SelectedOptionId;
                    prior.AnswerText = ans.AnswerText;
                    UnitOfWork.Repository<SubmissionAnswer, string>().Update(prior);
                }
            }
            await UnitOfWork.SaveChangesAsync(ct);

            var refreshed = await LoadAnswersAsync(attempt.Id);
            return Ok(await BuildDetailAsync(attempt, refreshed, includeGrade: false), 200, "Answers saved.");
        }

        public async Task<ApiResponse<AttemptSummaryDto>> SubmitAsync(string attemptId, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var attempt = await LoadOwnAttemptAsync(attemptId);

            // Idempotency: a submitted attempt cannot be resubmitted (no duplicate grade).
            if (attempt.submissionStatus != SubmissionStatus.InProgress)
                throw new ConflictException("This attempt has already been submitted.");

            var questions = await LoadQuizQuestionsAsync(attempt.QuizId);
            var answers = await LoadAnswersAsync(attempt.Id);

            int achieved = 0;
            int total = questions.Sum(q => q.Points);
            bool needsManual = false;

            foreach (var q in questions)
            {
                var answer = answers.FirstOrDefault(a => a.QuestionId == q.Id);
                if (IsObjective(q.Type))
                {
                    var correct = answer?.SelectedOptionId != null &&
                        q.Options.Any(o => o.Id == answer.SelectedOptionId && o.IsCorrect);
                    if (answer is not null)
                    {
                        answer.IsCorrect = correct;
                        answer.PointsEarned = correct ? q.Points : 0;
                        answer.GradingMethod = GradingMethod.Automatic;
                        answer.GradedAt = DateTime.UtcNow;
                        UnitOfWork.Repository<SubmissionAnswer, string>().Update(answer);
                    }
                    if (correct) achieved += q.Points;
                }
                else
                {
                    // Free-text/essay → manual grading required; not auto-scored.
                    needsManual = true;
                    if (answer is not null)
                    {
                        answer.GradingMethod = GradingMethod.Manual;
                        UnitOfWork.Repository<SubmissionAnswer, string>().Update(answer);
                    }
                }
            }

            attempt.AchievedScore = achieved;
            attempt.TotalScore = total;
            attempt.SubmittedAt = DateTime.UtcNow;
            attempt.submissionStatus = needsManual ? SubmissionStatus.Submitted : SubmissionStatus.Graded;
            attempt.GradingMethod = needsManual ? GradingMethod.Mixed : GradingMethod.Automatic;
            if (!needsManual) attempt.GradedAt = DateTime.UtcNow;
            UnitOfWork.Repository<QuizSubmission, string>().Update(attempt);

            await Audit.StageAsync(AuditActionType.Update, nameof(QuizSubmission), attempt.Id, "{\"action\":\"submit-attempt\"}", ct);
            await StageNotificationAsync(tenantId, attempt.StudentId,
                needsManual ? "Quiz submitted" : "Quiz graded",
                needsManual ? "Your quiz was submitted and is awaiting teacher grading."
                            : $"Your quiz was graded: {achieved}/{total}.",
                NotificationCategory.QuizGraded);
            await UnitOfWork.SaveChangesAsync(ct);

            return Ok(MapSummary(attempt), 200, "Attempt submitted.");
        }

        public async Task<ApiResponse<IEnumerable<AttemptSummaryDto>>> MyHistoryAsync(string quizId, CancellationToken ct = default)
        {
            RequireTenant();
            var studentId = RequireUser();
            var attempts = (await UnitOfWork.Repository<QuizSubmission, string>()
                .GetAllWithSpecAsync(new CriteriaSpecification<QuizSubmission, string>(s =>
                    s.QuizId == quizId && s.StudentId == studentId))).ToList();
            var dto = attempts.OrderBy(a => a.AttemptNumber).Select(MapSummary).ToList();
            return Ok<IEnumerable<AttemptSummaryDto>>(dto, 200, "Attempt history retrieved.");
        }

        public async Task<ApiResponse<AttemptDetailDto>> MyResultAsync(string attemptId, CancellationToken ct = default)
        {
            var attempt = await LoadOwnAttemptAsync(attemptId);
            if (attempt.submissionStatus == SubmissionStatus.InProgress)
                throw new ConflictException("Results are available only after the attempt is submitted.");
            var answers = await LoadAnswersAsync(attempt.Id);
            return Ok(await BuildDetailAsync(attempt, answers, includeGrade: true), 200, "Result retrieved.");
        }

        // ---- helpers ----

        private static bool IsObjective(QuestionType t) =>
            t == QuestionType.MCQ || t == QuestionType.TrueFalse || t == QuestionType.MultiSelect;

        private async Task<QuizSubmission> LoadOwnAttemptAsync(string attemptId)
        {
            var studentId = RequireUser();
            var attempt = await UnitOfWork.Repository<QuizSubmission, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<QuizSubmission, string>(s => s.Id == attemptId))
                ?? throw new NotFoundException("Attempt not found.");
            if (attempt.StudentId != studentId)
                throw new ForbiddenException("You can only access your own attempts.");
            return attempt;
        }

        private async Task<List<Question>> LoadQuizQuestionsAsync(string quizId)
        {
            var questions = (await UnitOfWork.Repository<Question, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Question, string>(q => q.QuizId == quizId, q => q.Options))).ToList();
            return questions.OrderBy(q => q.Order).ToList();
        }

        private async Task<List<SubmissionAnswer>> LoadAnswersAsync(string attemptId) =>
            (await UnitOfWork.Repository<SubmissionAnswer, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<SubmissionAnswer, string>(a => a.QuizSubmissionId == attemptId))).ToList();

        private static AttemptSummaryDto MapSummary(QuizSubmission s) => new()
        {
            Id = s.Id, QuizId = s.QuizId, StudentId = s.StudentId, AttemptNumber = s.AttemptNumber,
            Status = s.submissionStatus, StartedAt = s.StartedAt, SubmittedAt = s.SubmittedAt,
            AchievedScore = s.AchievedScore, TotalScore = s.TotalScore, Percentage = s.Percentage,
            IsLatestAttempt = s.IsLatestAttempt
        };

        private async Task<AttemptDetailDto> BuildDetailAsync(QuizSubmission s, List<SubmissionAnswer> answers, bool includeGrade)
        {
            var questions = await LoadQuizQuestionsAsync(s.QuizId);
            var d = new AttemptDetailDto
            {
                Id = s.Id, QuizId = s.QuizId, StudentId = s.StudentId, AttemptNumber = s.AttemptNumber,
                Status = s.submissionStatus, StartedAt = s.StartedAt, SubmittedAt = s.SubmittedAt,
                AchievedScore = includeGrade ? s.AchievedScore : 0,
                TotalScore = includeGrade ? s.TotalScore : 0,
                Percentage = includeGrade ? s.Percentage : 0,
                IsLatestAttempt = s.IsLatestAttempt,
                TeacherFeedback = includeGrade ? s.TeacherFeedback : null
            };
            // Student-safe question rendering: correct flags / model answers are NEVER populated.
            d.Questions = questions.Select(q => new QuestionDto
            {
                Id = q.Id, Text = q.Text, Type = q.Type, Order = q.Order, Points = q.Points,
                CorrectAnswerText = null, Explanation = null,
                Options = q.Options.Select(o => new QuestionOptionDto { Id = o.Id, Text = o.Text, IsCorrect = null }).ToList()
            }).ToList();
            d.Answers = answers.Select(a => new AnswerStateDto
            {
                QuestionId = a.QuestionId,
                SelectedOptionId = a.SelectedOptionId,
                AnswerText = a.AnswerText,
                // Correctness/points are released only once the attempt is graded — never
                // expose the correct answer to an in-progress attempt.
                IsCorrect = includeGrade ? a.IsCorrect : null,
                PointsEarned = includeGrade ? a.PointsEarned : null,
                Feedback = includeGrade ? a.Feedback : null
            }).ToList();
            return d;
        }
    }
}
