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
    public class QuizAuthoringService : AssessmentServiceBase, IQuizAuthoringService
    {
        public QuizAuthoringService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit)
            : base(unitOfWork, tenant, audit) { }

        public async Task<PaginationResponse<IEnumerable<QuizDto>>> ListAsync(QuizParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            Expression<Func<Quiz, bool>> criteria = q =>
                (string.IsNullOrEmpty(p.SubjectId) || q.SubjectId == p.SubjectId) &&
                (!p.Status.HasValue || q.Status == p.Status.Value) &&
                (!p.Type.HasValue || q.Type == p.Type.Value);

            var repo = UnitOfWork.Repository<Quiz, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<Quiz, string>(criteria));
            var items = (await repo.GetAllWithSpecAsync(
                new PagedSpecification<Quiz, string>(criteria, q => q.Id, p.PageNumber, p.PageSize))).ToList();

            // One bounded companion query for question counts/points (no N+1).
            var ids = items.Select(q => q.Id).ToList();
            var questions = ids.Count == 0
                ? new List<Question>()
                : (await UnitOfWork.Repository<Question, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<Question, string>(q => ids.Contains(q.QuizId)))).ToList();
            var byQuiz = questions.GroupBy(q => q.QuizId)
                .ToDictionary(g => g.Key, g => (count: g.Count(), points: g.Sum(x => x.Points)));

            var dto = items.Select(q =>
            {
                var m = MapQuiz(q);
                if (byQuiz.TryGetValue(q.Id, out var agg)) { m.QuestionCount = agg.count; m.TotalPoints = agg.points; }
                return m;
            }).ToList();

            return new PaginationResponse<IEnumerable<QuizDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Quizzes retrieved successfully." };
        }

        public async Task<ApiResponse<QuizDetailDto>> GetByIdAsync(string id, CancellationToken ct = default)
        {
            // Teacher/admin detail view — includes correct answers. Authorize management.
            var quiz = await LoadManageableQuizAsync(id, ct);
            var questions = await LoadQuestionsAsync(quiz.Id);
            return Ok(MapDetail(quiz, questions, includeAnswers: true), 200, "Quiz retrieved successfully.");
        }

        public async Task<ApiResponse<QuizDto>> CreateAsync(AddQuizDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new BadRequestException("Title is required.");
            if (dto.TimeLimitMinutes <= 0 || dto.TimeLimitMinutes > 600)
                throw new BadRequestException("TimeLimitMinutes must be between 1 and 600.");
            if (dto.MaxAttempts is < 1)
                throw new BadRequestException("MaxAttempts, when set, must be at least 1.");

            // Subject anchoring: a teacher may only author for a subject they hold an
            // active assignment for; SchoolAdmin may author for any same-tenant subject.
            if (!string.IsNullOrEmpty(dto.SubjectId))
            {
                var subject = await UnitOfWork.Repository<Subject, string>()
                    .GetByIdWithSpecAsync(new CriteriaSpecification<Subject, string>(s => s.Id == dto.SubjectId))
                    ?? throw new NotFoundException("Subject not found.");
            }

            if (IsTeacher)
            {
                if (string.IsNullOrEmpty(dto.SubjectId))
                    throw new BadRequestException("A teacher must specify the SubjectId they are assigned to.");
                var userId = RequireUser();
                var assigned = await UnitOfWork.Repository<TeacherSubjectAssignment, string>()
                    .CountAsync(new CriteriaSpecification<TeacherSubjectAssignment, string>(a =>
                        a.TeacherId == userId && a.SubjectId == dto.SubjectId && a.IsActive));
                if (assigned == 0)
                    throw new ForbiddenException("You are not an assigned teacher for the requested subject.");
            }

            var quiz = new Quiz
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                Title = dto.Title,
                Type = dto.Type,
                Difficulty = dto.Difficulty,
                TimeLimitMinutes = dto.TimeLimitMinutes,
                MaxAttempts = dto.MaxAttempts,
                DueDate = AsUtc(dto.DueDate),
                SubjectId = dto.SubjectId,
                LessonId = dto.LessonId,
                Status = QuizStatus.Draft,
                Origin = QuizOrigin.Manual
            };

            await UnitOfWork.Repository<Quiz, string>().AddAsync(quiz);
            await Audit.StageAsync(AuditActionType.Create, nameof(Quiz), quiz.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(MapQuiz(quiz), 201, "Quiz draft created successfully.");
        }

        public async Task<ApiResponse<QuizDto>> UpdateAsync(UpdateQuizDto dto, CancellationToken ct = default)
        {
            var quiz = await LoadManageableQuizAsync(dto.Id, ct);
            RequireDraft(quiz);
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new BadRequestException("Title is required.");
            if (dto.TimeLimitMinutes <= 0 || dto.TimeLimitMinutes > 600)
                throw new BadRequestException("TimeLimitMinutes must be between 1 and 600.");
            if (dto.MaxAttempts is < 1)
                throw new BadRequestException("MaxAttempts, when set, must be at least 1.");

            quiz.Title = dto.Title;
            quiz.Type = dto.Type;
            quiz.Difficulty = dto.Difficulty;
            quiz.TimeLimitMinutes = dto.TimeLimitMinutes;
            quiz.MaxAttempts = dto.MaxAttempts;
            quiz.DueDate = AsUtc(dto.DueDate);

            UnitOfWork.Repository<Quiz, string>().Update(quiz);
            await Audit.StageAsync(AuditActionType.Update, nameof(Quiz), quiz.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(MapQuiz(quiz), 200, "Quiz updated successfully.");
        }

        public async Task<ApiResponse<QuestionDto>> AddQuestionAsync(string quizId, AddQuestionDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var quiz = await LoadManageableQuizAsync(quizId, ct);
            RequireDraft(quiz);
            ValidateQuestion(dto.Type, dto.Text, dto.Points, dto.CorrectAnswerText, dto.Options);

            var question = new Question
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                QuizId = quiz.Id,
                Text = dto.Text,
                Type = dto.Type,
                Order = dto.Order,
                Points = dto.Points,
                CorrectAnswerText = dto.CorrectAnswerText,
                Explanation = dto.Explanation
            };
            await UnitOfWork.Repository<Question, string>().AddAsync(question);
            foreach (var opt in dto.Options)
            {
                await UnitOfWork.Repository<QuestionOption, string>().AddAsync(new QuestionOption
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    QuestionId = question.Id,
                    Text = opt.Text,
                    IsCorrect = opt.IsCorrect
                });
            }
            await Audit.StageAsync(AuditActionType.Update, nameof(Quiz), quiz.Id, "{\"action\":\"add-question\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);

            var options = await LoadOptionsAsync(question.Id);
            return Ok(MapQuestion(question, options, includeAnswers: true), 201, "Question added successfully.");
        }

        public async Task<ApiResponse<QuestionDto>> UpdateQuestionAsync(string quizId, UpdateQuestionDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var quiz = await LoadManageableQuizAsync(quizId, ct);
            RequireDraft(quiz);
            ValidateQuestion(dto.Type, dto.Text, dto.Points, dto.CorrectAnswerText, dto.Options);

            var question = await UnitOfWork.Repository<Question, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<Question, string>(q => q.Id == dto.Id && q.QuizId == quiz.Id))
                ?? throw new NotFoundException("Question not found.");

            question.Text = dto.Text;
            question.Type = dto.Type;
            question.Order = dto.Order;
            question.Points = dto.Points;
            question.CorrectAnswerText = dto.CorrectAnswerText;
            question.Explanation = dto.Explanation;
            UnitOfWork.Repository<Question, string>().Update(question);

            // Replace options (draft only, before any attempts — safe).
            var existing = await LoadOptionsAsync(question.Id);
            foreach (var o in existing) UnitOfWork.Repository<QuestionOption, string>().Delete(o);
            foreach (var opt in dto.Options)
            {
                await UnitOfWork.Repository<QuestionOption, string>().AddAsync(new QuestionOption
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    QuestionId = question.Id,
                    Text = opt.Text,
                    IsCorrect = opt.IsCorrect
                });
            }
            await Audit.StageAsync(AuditActionType.Update, nameof(Quiz), quiz.Id, "{\"action\":\"update-question\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);

            var options = await LoadOptionsAsync(question.Id);
            return Ok(MapQuestion(question, options, includeAnswers: true), 200, "Question updated successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteQuestionAsync(string quizId, string questionId, CancellationToken ct = default)
        {
            var quiz = await LoadManageableQuizAsync(quizId, ct);
            RequireDraft(quiz);
            var question = await UnitOfWork.Repository<Question, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<Question, string>(q => q.Id == questionId && q.QuizId == quiz.Id))
                ?? throw new NotFoundException("Question not found.");

            var options = await LoadOptionsAsync(question.Id);
            foreach (var o in options) UnitOfWork.Repository<QuestionOption, string>().Delete(o);
            UnitOfWork.Repository<Question, string>().Delete(question);
            await Audit.StageAsync(AuditActionType.Update, nameof(Quiz), quiz.Id, "{\"action\":\"delete-question\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(true, 200, "Question deleted successfully.");
        }

        public async Task<ApiResponse<QuizDto>> PublishAsync(string id, CancellationToken ct = default)
        {
            var quiz = await LoadManageableQuizAsync(id, ct);
            if (quiz.Status == QuizStatus.Published)
                throw new ConflictException("Quiz is already published.");
            if (quiz.Status == QuizStatus.Archived)
                throw new ConflictException("An archived quiz cannot be published.");

            var questions = await LoadQuestionsAsync(quiz.Id);
            if (questions.Count == 0)
                throw new BadRequestException("A quiz must contain at least one question to be published.");

            foreach (var q in questions)
            {
                if (q.Points <= 0)
                    throw new BadRequestException($"Question '{q.Text}' must have a positive point value.");
                if (IsObjective(q.Type))
                {
                    if (q.Options.Count < 2)
                        throw new BadRequestException($"Question '{q.Text}' must have at least two options.");
                    if (!q.Options.Any(o => o.IsCorrect))
                        throw new BadRequestException($"Question '{q.Text}' must have at least one correct option.");
                }
            }

            quiz.Status = QuizStatus.Published;
            quiz.ApprovedByTeacherId = Tenant.UserId;
            quiz.ApprovedAt = DateTime.UtcNow;
            UnitOfWork.Repository<Quiz, string>().Update(quiz);
            await Audit.StageAsync(AuditActionType.Update, nameof(Quiz), quiz.Id, "{\"action\":\"publish\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);

            return Ok(MapQuiz(quiz), 200, "Quiz published successfully.");
        }

        public async Task<ApiResponse<QuizDto>> ArchiveAsync(string id, CancellationToken ct = default)
        {
            var quiz = await LoadManageableQuizAsync(id, ct);
            if (quiz.Status == QuizStatus.Archived)
                throw new ConflictException("Quiz is already archived.");
            quiz.Status = QuizStatus.Archived;
            UnitOfWork.Repository<Quiz, string>().Update(quiz);
            await Audit.StageAsync(AuditActionType.Update, nameof(Quiz), quiz.Id, "{\"action\":\"archive\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(MapQuiz(quiz), 200, "Quiz archived successfully.");
        }

        // ---- helpers ----

        private static void RequireDraft(Quiz quiz)
        {
            if (quiz.Status != QuizStatus.Draft)
                throw new ConflictException("Only a draft quiz can be edited. Published/archived quizzes are immutable to preserve historical attempts.");
        }

        private static bool IsObjective(QuestionType t) =>
            t == QuestionType.MCQ || t == QuestionType.TrueFalse || t == QuestionType.MultiSelect;

        private static void ValidateQuestion(QuestionType type, string text, int points, string? correctText, List<AddQuestionOptionDto> options)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new BadRequestException("Question text is required.");
            if (points <= 0) throw new BadRequestException("Question points must be positive.");
            if (IsObjective(type))
            {
                if (options is null || options.Count < 2)
                    throw new BadRequestException("Objective questions require at least two options.");
                if (!options.Any(o => o.IsCorrect))
                    throw new BadRequestException("Objective questions require at least one correct option.");
            }
            // Essay/free-text: graded manually; a model answer is optional.
        }

        private async Task<List<Question>> LoadQuestionsAsync(string quizId)
        {
            var questions = (await UnitOfWork.Repository<Question, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Question, string>(q => q.QuizId == quizId, q => q.Options))).ToList();
            return questions.OrderBy(q => q.Order).ToList();
        }

        private async Task<List<QuestionOption>> LoadOptionsAsync(string questionId) =>
            (await UnitOfWork.Repository<QuestionOption, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<QuestionOption, string>(o => o.QuestionId == questionId))).ToList();

        private static QuizDto MapQuiz(Quiz q) => new()
        {
            Id = q.Id, Title = q.Title, Status = q.Status, Origin = q.Origin, Type = q.Type,
            Difficulty = q.Difficulty, TimeLimitMinutes = q.TimeLimitMinutes, MaxAttempts = q.MaxAttempts,
            DueDate = q.DueDate, SubjectId = q.SubjectId, LessonId = q.LessonId,
            ApprovedByTeacherId = q.ApprovedByTeacherId, ApprovedAt = q.ApprovedAt
        };

        private static QuizDetailDto MapDetail(Quiz q, List<Question> questions, bool includeAnswers)
        {
            var d = new QuizDetailDto
            {
                Id = q.Id, Title = q.Title, Status = q.Status, Origin = q.Origin, Type = q.Type,
                Difficulty = q.Difficulty, TimeLimitMinutes = q.TimeLimitMinutes, MaxAttempts = q.MaxAttempts,
                DueDate = q.DueDate, SubjectId = q.SubjectId, LessonId = q.LessonId,
                ApprovedByTeacherId = q.ApprovedByTeacherId, ApprovedAt = q.ApprovedAt,
                QuestionCount = questions.Count, TotalPoints = questions.Sum(x => x.Points)
            };
            d.Questions = questions.Select(x => MapQuestion(x, x.Options.ToList(), includeAnswers)).ToList();
            return d;
        }

        private static QuestionDto MapQuestion(Question q, List<QuestionOption> options, bool includeAnswers) => new()
        {
            Id = q.Id, Text = q.Text, Type = q.Type, Order = q.Order, Points = q.Points,
            CorrectAnswerText = includeAnswers ? q.CorrectAnswerText : null,
            Explanation = q.Explanation,
            Options = options.Select(o => new QuestionOptionDto
            {
                Id = o.Id, Text = o.Text, IsCorrect = includeAnswers ? o.IsCorrect : null
            }).ToList()
        };
    }
}
