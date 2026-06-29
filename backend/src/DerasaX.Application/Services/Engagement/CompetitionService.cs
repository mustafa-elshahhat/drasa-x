using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.EngagementDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Engagement;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;

namespace DerasaX.Application.Services.Engagement
{
    public class CompetitionService : EngagementServiceBase, ICompetitionService
    {
        public CompetitionService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit)
            : base(unitOfWork, tenant, audit) { }

        public async Task<PaginationResponse<IEnumerable<CompetitionDto>>> ListAsync(CompetitionParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            Expression<Func<Competition, bool>> criteria = c => !p.Status.HasValue || c.Status == p.Status.Value;
            var repo = UnitOfWork.Repository<Competition, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<Competition, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<Competition, string>(criteria, c => c.StartsAt, p.PageNumber, p.PageSize, descending: true));
            var dto = items.Select(Map).ToList();
            return new PaginationResponse<IEnumerable<CompetitionDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Competitions retrieved." };
        }

        public async Task<ApiResponse<CompetitionDto>> GetAsync(string id, CancellationToken ct = default) =>
            Ok(Map(await LoadAsync(id)), 200, "Competition retrieved.");

        public async Task<ApiResponse<CompetitionDto>> CreateAsync(CreateCompetitionDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            RequireStaff();
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new BadRequestException("Title is required.");
            if (AsUtc(dto.EndsAt) <= AsUtc(dto.StartsAt)) throw new BadRequestException("EndsAt must be after StartsAt.");

            var competition = new Competition
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, Title = dto.Title, Description = dto.Description,
                Status = CompetitionStatus.Draft, StartsAt = AsUtc(dto.StartsAt), EndsAt = AsUtc(dto.EndsAt)
            };
            await UnitOfWork.Repository<Competition, string>().AddAsync(competition);
            await Audit.StageAsync(AuditActionType.Create, nameof(Competition), competition.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(competition), 201, "Competition created.");
        }

        public async Task<ApiResponse<CompetitionDto>> UpdateAsync(string id, UpdateCompetitionDto dto, CancellationToken ct = default)
        {
            RequireStaff();
            var competition = await LoadAsync(id);
            if (competition.Status is CompetitionStatus.Closed or CompetitionStatus.Archived)
                throw new ConflictException("A closed or archived competition cannot be edited.");
            if (AsUtc(dto.EndsAt) <= AsUtc(dto.StartsAt)) throw new BadRequestException("EndsAt must be after StartsAt.");
            competition.Title = dto.Title;
            competition.Description = dto.Description;
            competition.StartsAt = AsUtc(dto.StartsAt);
            competition.EndsAt = AsUtc(dto.EndsAt);
            UnitOfWork.Repository<Competition, string>().Update(competition);
            await Audit.StageAsync(AuditActionType.Update, nameof(Competition), competition.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(competition), 200, "Competition updated.");
        }

        public async Task<ApiResponse<CompetitionDto>> PublishAsync(string id, CancellationToken ct = default)
        {
            RequireStaff();
            var competition = await LoadAsync(id);
            if (competition.Status != CompetitionStatus.Draft)
                throw new ConflictException("Only a draft competition can be published.");
            competition.Status = CompetitionStatus.Published;
            UnitOfWork.Repository<Competition, string>().Update(competition);
            await Audit.StageAsync(AuditActionType.Update, nameof(Competition), competition.Id, "{\"action\":\"publish\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(competition), 200, "Competition published.");
        }

        public async Task<ApiResponse<CompetitionDto>> ArchiveAsync(string id, CancellationToken ct = default)
        {
            RequireStaff();
            var competition = await LoadAsync(id);
            competition.Status = CompetitionStatus.Archived;
            UnitOfWork.Repository<Competition, string>().Update(competition);
            await Audit.StageAsync(AuditActionType.Update, nameof(Competition), competition.Id, "{\"action\":\"archive\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(competition), 200, "Competition archived.");
        }

        public async Task<ApiResponse<CompetitionEntryDto>> EnterAsync(string id, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var studentId = RequireUser();
            if (!IsStudent) throw new ForbiddenException("Only a student may enter a competition.");
            var competition = await LoadAsync(id);

            if (competition.Status is not (CompetitionStatus.Published or CompetitionStatus.Active))
                throw new ConflictException("This competition is not open for entries.");
            if (DateTime.UtcNow >= competition.EndsAt)
                throw new ConflictException("The competition entry window has closed.");

            // Duplicate-entry prevention (one entry per student).
            var existing = await UnitOfWork.Repository<CompetitionEntry, string>().CountAsync(
                new CriteriaSpecification<CompetitionEntry, string>(e => e.CompetitionId == id && e.StudentId == studentId));
            if (existing > 0) throw new ConflictException("You have already entered this competition.");

            var entry = new CompetitionEntry
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, CompetitionId = id, StudentId = studentId, EnteredAt = DateTime.UtcNow
            };
            await UnitOfWork.Repository<CompetitionEntry, string>().AddAsync(entry);
            await Audit.StageAsync(AuditActionType.Create, nameof(CompetitionEntry), entry.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(new CompetitionEntryDto { Id = entry.Id, CompetitionId = id, StudentId = studentId, EnteredAt = entry.EnteredAt }, 201, "Entered competition.");
        }

        public async Task<ApiResponse<bool>> RecordScoreAsync(string id, string entryId, RecordScoreDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            RequireStaff(); // only authorized staff manage scores (score integrity)
            await LoadAsync(id);
            var entry = await UnitOfWork.Repository<CompetitionEntry, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<CompetitionEntry, string>(e => e.Id == entryId && e.CompetitionId == id))
                ?? throw new NotFoundException("Entry not found.");
            if (dto.Score < 0) throw new BadRequestException("Score cannot be negative.");

            var score = (await UnitOfWork.Repository<CompetitionScore, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<CompetitionScore, string>(s => s.CompetitionEntryId == entryId))).FirstOrDefault();
            if (score is null)
            {
                await UnitOfWork.Repository<CompetitionScore, string>().AddAsync(new CompetitionScore
                {
                    Id = Guid.NewGuid().ToString(), TenantId = tenantId, CompetitionEntryId = entryId, Score = dto.Score, ScoredAt = DateTime.UtcNow
                });
            }
            else
            {
                score.Score = dto.Score;
                score.ScoredAt = DateTime.UtcNow;
                UnitOfWork.Repository<CompetitionScore, string>().Update(score);
            }
            await Audit.StageAsync(AuditActionType.Update, nameof(CompetitionScore), entryId, "{\"action\":\"record-score\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(true, 200, "Score recorded.");
        }

        public async Task<ApiResponse<IEnumerable<LeaderboardRowDto>>> LeaderboardAsync(string id, CancellationToken ct = default)
        {
            RequireTenant();
            var competition = await LoadAsync(id);
            // Result visibility timing: students may view only once the competition is active or closed.
            if (IsStudent && competition.Status is not (CompetitionStatus.Active or CompetitionStatus.Closed))
                throw new ForbiddenException("Results are not yet available.");

            var entries = (await UnitOfWork.Repository<CompetitionEntry, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<CompetitionEntry, string>(e => e.CompetitionId == id))).ToList();
            var entryIds = entries.Select(e => e.Id).ToList();
            var scores = entryIds.Count == 0
                ? new List<CompetitionScore>()
                : (await UnitOfWork.Repository<CompetitionScore, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<CompetitionScore, string>(s => entryIds.Contains(s.CompetitionEntryId)))).ToList();

            var scoredEntryIds = scores.Select(s => s.CompetitionEntryId).ToHashSet();
            var rows = entries
                .Where(e => scoredEntryIds.Contains(e.Id))
                .Select(e => new
                {
                    e.StudentId,
                    Score = scores.Where(s => s.CompetitionEntryId == e.Id).Max(s => s.Score)
                })
                .OrderByDescending(x => x.Score)
                .Select((x, i) => new LeaderboardRowDto { StudentId = x.StudentId, Score = x.Score, Rank = i + 1 })
                .ToList();
            return Ok<IEnumerable<LeaderboardRowDto>>(rows, 200, "Leaderboard retrieved.");
        }

        public async Task<ApiResponse<CompetitionDto>> CloseAsync(string id, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            RequireStaff();
            var competition = await LoadAsync(id);
            if (competition.Status is not (CompetitionStatus.Published or CompetitionStatus.Active))
                throw new ConflictException("Only a published or active competition can be closed and have results published.");

            competition.Status = CompetitionStatus.Closed;
            UnitOfWork.Repository<Competition, string>().Update(competition);

            // Rank the scored entries (highest score first).
            var entries = (await UnitOfWork.Repository<CompetitionEntry, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<CompetitionEntry, string>(e => e.CompetitionId == id))).ToList();
            var entryIds = entries.Select(e => e.Id).ToList();
            var scores = entryIds.Count == 0
                ? new List<CompetitionScore>()
                : (await UnitOfWork.Repository<CompetitionScore, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<CompetitionScore, string>(s => entryIds.Contains(s.CompetitionEntryId)))).ToList();

            var ranked = entries
                .Where(e => scores.Any(s => s.CompetitionEntryId == e.Id))
                .Select(e => new { e.StudentId, Score = scores.Where(s => s.CompetitionEntryId == e.Id).Max(s => s.Score) })
                .OrderByDescending(x => x.Score)
                .ToList();

            // Idempotent gamification rewards: participation for every scored entrant, plus a top-rank
            // bonus for first place. Re-closing (or a retry) never double-awards thanks to the keys.
            var (participation, partRuleId) = await GamificationDefaults.ResolveAsync(
                UnitOfWork, GamificationTrigger.CompetitionParticipation, GamificationDefaults.CompetitionParticipation, ct);
            var (topRank, topRuleId) = await GamificationDefaults.ResolveAsync(
                UnitOfWork, GamificationTrigger.CompetitionTopRank, GamificationDefaults.CompetitionTopRank, ct);

            for (var i = 0; i < ranked.Count; i++)
            {
                var r = ranked[i];
                if (participation != 0)
                    await PointLedgerStaging.StageAsync(UnitOfWork, tenantId, r.StudentId, participation,
                        "Competition participation", PointSourceType.CompetitionReward, $"comp-part:{id}:{r.StudentId}",
                        sourceId: id, gamificationRuleId: partRuleId, ct: ct);
                if (i == 0 && topRank != 0)
                    await PointLedgerStaging.StageAsync(UnitOfWork, tenantId, r.StudentId, topRank,
                        "Competition winner", PointSourceType.CompetitionReward, $"comp-top:{id}:{r.StudentId}",
                        sourceId: id, gamificationRuleId: topRuleId, ct: ct);
            }

            // Notify every entrant that results are published (bounded to opted-in entrants).
            foreach (var studentId in entries.Select(e => e.StudentId).Distinct())
                await StageNotificationAsync(tenantId, studentId, "Competition results published",
                    $"Results for '{competition.Title}' are now available.", NotificationCategory.General);

            await Audit.StageAsync(AuditActionType.Update, nameof(Competition), competition.Id,
                $"{{\"action\":\"close-publish-results\",\"scoredEntrants\":{ranked.Count}}}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(competition), 200, "Competition closed and results published.");
        }

        public async Task<ApiResponse<CompetitionSubmissionDto>> SubmitAsync(string id, SubmitCompetitionDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var studentId = RequireUser();
            if (!IsStudent) throw new ForbiddenException("Only a student may submit to a competition.");
            if (string.IsNullOrWhiteSpace(dto.Content)) throw new BadRequestException("Submission content is required.");

            var competition = await LoadAsync(id);
            if (competition.Status is not (CompetitionStatus.Published or CompetitionStatus.Active))
                throw new ConflictException("This competition is not open for submissions.");
            if (DateTime.UtcNow >= competition.EndsAt)
                throw new ConflictException("The competition submission window has closed.");

            // A student must have entered before submitting work (entry = registration).
            var entered = await UnitOfWork.Repository<CompetitionEntry, string>().CountAsync(
                new CriteriaSpecification<CompetitionEntry, string>(e => e.CompetitionId == id && e.StudentId == studentId));
            if (entered == 0) throw new ConflictException("Enter the competition before submitting your work.");

            var content = dto.Content.Trim();
            var existing = (await UnitOfWork.Repository<CompetitionSubmission, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<CompetitionSubmission, string>(s => s.CompetitionId == id && s.StudentId == studentId))).FirstOrDefault();

            if (existing is null)
            {
                var submission = new CompetitionSubmission
                {
                    Id = Guid.NewGuid().ToString(), TenantId = tenantId, CompetitionId = id,
                    StudentId = studentId, Content = content, SubmittedAt = DateTime.UtcNow
                };
                await UnitOfWork.Repository<CompetitionSubmission, string>().AddAsync(submission);
                await Audit.StageAsync(AuditActionType.Create, nameof(CompetitionSubmission), submission.Id, ct: ct);
                await UnitOfWork.SaveChangesAsync(ct);
                return Ok(MapSubmission(submission), 201, "Submission recorded.");
            }

            // Durable resubmission while the window is open: update content in place.
            existing.Content = content;
            existing.SubmittedAt = DateTime.UtcNow;
            UnitOfWork.Repository<CompetitionSubmission, string>().Update(existing);
            await Audit.StageAsync(AuditActionType.Update, nameof(CompetitionSubmission), existing.Id, "{\"action\":\"resubmit\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(MapSubmission(existing), 200, "Submission updated.");
        }

        public async Task<ApiResponse<CompetitionSubmissionDto>> MySubmissionAsync(string id, CancellationToken ct = default)
        {
            RequireTenant();
            var studentId = RequireUser();
            if (!IsStudent) throw new ForbiddenException("Only a student has a personal submission.");
            await LoadAsync(id);
            var submission = (await UnitOfWork.Repository<CompetitionSubmission, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<CompetitionSubmission, string>(s => s.CompetitionId == id && s.StudentId == studentId))).FirstOrDefault()
                ?? throw new NotFoundException("You have not submitted to this competition.");
            return Ok(MapSubmission(submission), 200, "Submission retrieved.");
        }

        public async Task<ApiResponse<IEnumerable<CompetitionSubmissionDto>>> SubmissionsAsync(string id, CancellationToken ct = default)
        {
            RequireTenant();
            RequireStaff(); // only staff judge submissions
            await LoadAsync(id);
            var submissions = await UnitOfWork.Repository<CompetitionSubmission, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<CompetitionSubmission, string>(s => s.CompetitionId == id));
            return Ok<IEnumerable<CompetitionSubmissionDto>>(submissions.Select(MapSubmission).ToList(), 200, "Submissions retrieved.");
        }

        // ---- helpers ----

        private void RequireStaff()
        {
            if (!IsTeacher && !IsSchoolAdmin) throw new ForbiddenException("Only a teacher or school administrator may perform this action.");
        }

        private async Task<Competition> LoadAsync(string id) =>
            await UnitOfWork.Repository<Competition, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Competition, string>(c => c.Id == id)) ?? throw new NotFoundException("Competition not found.");

        private static CompetitionDto Map(Competition c) => new()
        {
            Id = c.Id, Title = c.Title, Description = c.Description, Status = c.Status, StartsAt = c.StartsAt, EndsAt = c.EndsAt
        };

        private static CompetitionSubmissionDto MapSubmission(CompetitionSubmission s) => new()
        {
            Id = s.Id, CompetitionId = s.CompetitionId, StudentId = s.StudentId, Content = s.Content, SubmittedAt = s.SubmittedAt
        };
    }
}
