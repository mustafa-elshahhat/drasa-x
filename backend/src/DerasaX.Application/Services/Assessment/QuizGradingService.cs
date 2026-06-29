using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    public class QuizGradingService : AssessmentServiceBase, IQuizGradingService
    {
        public QuizGradingService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit)
            : base(unitOfWork, tenant, audit) { }

        public async Task<PaginationResponse<IEnumerable<AttemptSummaryDto>>> ListSubmissionsAsync(
            string quizId, AttemptParameters p, CancellationToken ct = default)
        {
            await LoadManageableQuizAsync(quizId, ct);
            Expression<Func<QuizSubmission, bool>> criteria = s =>
                s.QuizId == quizId &&
                (string.IsNullOrEmpty(p.StudentId) || s.StudentId == p.StudentId) &&
                (p.Status.HasValue ? s.submissionStatus == p.Status.Value : s.submissionStatus != SubmissionStatus.InProgress);

            var repo = UnitOfWork.Repository<QuizSubmission, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<QuizSubmission, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<QuizSubmission, string>(criteria, s => s.SubmittedAt, p.PageNumber, p.PageSize, descending: true));

            var dto = items.Select(MapSummary).ToList();
            return new PaginationResponse<IEnumerable<AttemptSummaryDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Submissions retrieved successfully." };
        }

        public async Task<ApiResponse<AttemptDetailDto>> GetSubmissionAsync(string attemptId, CancellationToken ct = default)
        {
            var attempt = await LoadAttemptForManageAsync(attemptId, ct);
            var answers = await LoadAnswersAsync(attempt.Id);
            return Ok(MapDetail(attempt, answers), 200, "Submission retrieved.");
        }

        public async Task<ApiResponse<AttemptSummaryDto>> GradeAsync(string attemptId, ManualGradeDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var attempt = await LoadAttemptForManageAsync(attemptId, ct);
            if (attempt.submissionStatus == SubmissionStatus.InProgress)
                throw new ConflictException("An in-progress attempt cannot be graded.");

            var questions = await LoadQuizQuestionsAsync(attempt.QuizId);
            var pointsByQuestion = questions.ToDictionary(q => q.Id, q => q.Points);
            var answers = await LoadAnswersAsync(attempt.Id);
            var answerById = answers.ToDictionary(a => a.Id);

            foreach (var g in dto.Grades)
            {
                if (!answerById.TryGetValue(g.AnswerId, out var answer))
                    throw new BadRequestException("Grade references an answer that is not part of this attempt.");
                var max = pointsByQuestion.TryGetValue(answer.QuestionId, out var pts) ? pts : 0;
                if (g.PointsEarned < 0 || g.PointsEarned > max)
                    throw new BadRequestException($"PointsEarned must be between 0 and {max}.");
                answer.PointsEarned = g.PointsEarned;
                answer.IsCorrect = g.IsCorrect;
                answer.Feedback = g.Feedback;
                answer.GradingMethod = GradingMethod.Manual;
                answer.GradedByTeacherId = Tenant.UserId;
                answer.GradedAt = DateTime.UtcNow;
                UnitOfWork.Repository<SubmissionAnswer, string>().Update(answer);
            }

            // Authoritative recompute from persisted answers — client never sets the total.
            attempt.AchievedScore = answers.Sum(a => a.PointsEarned);
            attempt.submissionStatus = SubmissionStatus.Graded;
            attempt.GradingMethod = GradingMethod.Mixed;
            attempt.GradedByTeacherId = Tenant.UserId;
            attempt.GradedAt = DateTime.UtcNow;
            UnitOfWork.Repository<QuizSubmission, string>().Update(attempt);

            await Audit.StageAsync(AuditActionType.Update, nameof(QuizSubmission), attempt.Id, "{\"action\":\"manual-grade\"}", ct);
            await StageNotificationAsync(tenantId, attempt.StudentId, "Quiz graded",
                $"Your quiz has been graded: {attempt.AchievedScore}/{attempt.TotalScore}.", NotificationCategory.QuizGraded);
            await UnitOfWork.SaveChangesAsync(ct);

            return Ok(MapSummary(attempt), 200, "Attempt graded.");
        }

        public async Task<ApiResponse<AttemptSummaryDto>> FeedbackAsync(string attemptId, FeedbackDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var attempt = await LoadAttemptForManageAsync(attemptId, ct);
            if (string.IsNullOrWhiteSpace(dto.Feedback))
                throw new BadRequestException("Feedback text is required.");

            attempt.TeacherFeedback = dto.Feedback;
            UnitOfWork.Repository<QuizSubmission, string>().Update(attempt);
            await Audit.StageAsync(AuditActionType.Update, nameof(QuizSubmission), attempt.Id, "{\"action\":\"feedback\"}", ct);
            await StageNotificationAsync(tenantId, attempt.StudentId, "Teacher feedback added",
                "Your teacher added feedback to your quiz attempt.", NotificationCategory.QuizGraded);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(MapSummary(attempt), 200, "Feedback saved.");
        }

        public async Task<ApiResponse<QuizAnalyticsDto>> AnalyticsAsync(string quizId, CancellationToken ct = default)
        {
            var quiz = await LoadManageableQuizAsync(quizId, ct);
            var questions = await LoadQuizQuestionsAsync(quiz.Id);

            var submissions = (await UnitOfWork.Repository<QuizSubmission, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<QuizSubmission, string>(s =>
                    s.QuizId == quizId && s.submissionStatus != SubmissionStatus.InProgress))).ToList();
            var submissionIds = submissions.Select(s => s.Id).ToList();
            var answers = submissionIds.Count == 0
                ? new List<SubmissionAnswer>()
                : (await UnitOfWork.Repository<SubmissionAnswer, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<SubmissionAnswer, string>(a => submissionIds.Contains(a.QuizSubmissionId)))).ToList();

            var result = new QuizAnalyticsDto
            {
                QuizId = quizId,
                TotalSubmissions = submissions.Count,
                TotalPoints = questions.Sum(q => q.Points),
                AverageScorePercentage = submissions.Count == 0 ? 0 : Math.Round(submissions.Average(s => s.Percentage), 2)
            };
            foreach (var q in questions)
            {
                var qAnswers = answers.Where(a => a.QuestionId == q.Id).ToList();
                var correct = qAnswers.Count(a => a.IsCorrect);
                result.Questions.Add(new QuestionAnalyticsDto
                {
                    QuestionId = q.Id, Text = q.Text, Answered = qAnswers.Count, CorrectCount = correct,
                    CorrectRate = qAnswers.Count == 0 ? 0 : Math.Round((double)correct / qAnswers.Count * 100, 2)
                });
            }
            return Ok(result, 200, "Analytics retrieved successfully.");
        }

        // ---- helpers ----

        private async Task<QuizSubmission> LoadAttemptForManageAsync(string attemptId, CancellationToken ct)
        {
            var attempt = await UnitOfWork.Repository<QuizSubmission, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<QuizSubmission, string>(s => s.Id == attemptId))
                ?? throw new NotFoundException("Attempt not found.");
            var quiz = await UnitOfWork.Repository<Quiz, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<Quiz, string>(q => q.Id == attempt.QuizId))
                ?? throw new NotFoundException("Attempt not found.");
            await AuthorizeManageAsync(quiz, ct);
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

        private static AttemptDetailDto MapDetail(QuizSubmission s, List<SubmissionAnswer> answers)
        {
            var d = new AttemptDetailDto
            {
                Id = s.Id, QuizId = s.QuizId, StudentId = s.StudentId, AttemptNumber = s.AttemptNumber,
                Status = s.submissionStatus, StartedAt = s.StartedAt, SubmittedAt = s.SubmittedAt,
                AchievedScore = s.AchievedScore, TotalScore = s.TotalScore, Percentage = s.Percentage,
                IsLatestAttempt = s.IsLatestAttempt, TeacherFeedback = s.TeacherFeedback
            };
            d.Answers = answers.Select(a => new AnswerStateDto
            {
                Id = a.Id, QuestionId = a.QuestionId, SelectedOptionId = a.SelectedOptionId, AnswerText = a.AnswerText,
                IsCorrect = a.IsCorrect, PointsEarned = a.PointsEarned, Feedback = a.Feedback
            }).ToList();
            return d;
        }
    }
}
