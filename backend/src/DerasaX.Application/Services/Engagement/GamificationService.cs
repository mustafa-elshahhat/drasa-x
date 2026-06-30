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
using DerasaX.Application.Services.Abstractions.Authorization;
using DerasaX.Application.Services.Abstractions.Engagement;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.Engagement
{
    /// <summary>
    /// Phase 14 — ledger-based gamification. Points are the SUM of immutable
    /// <see cref="StudentPointTransaction"/> rows; nothing stores a mutable total. Manual awards are
    /// authorized (teacher/admin), bounded, audited and notified; every award is idempotent via a
    /// per-tenant idempotency key. Leaderboards are tenant-scoped (and optionally grade-scoped).
    /// </summary>
    public class GamificationService : EngagementServiceBase, IGamificationService
    {
        private readonly IStudentAccessAuthorizer _access;
        private readonly UserManager<ApplicationUser> _users;

        public GamificationService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit,
            IStudentAccessAuthorizer access, UserManager<ApplicationUser> users) : base(unitOfWork, tenant, audit)
        {
            _access = access;
            _users = users;
        }

        public async Task<ApiResponse<StudentPointsSummaryDto>> SummaryAsync(string studentId, CancellationToken ct = default)
        {
            await _access.EnsureCanAccessStudentAsync(studentId, ct);
            var txs = (await UnitOfWork.Repository<StudentPointTransaction, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<StudentPointTransaction, string>(t => t.StudentId == studentId))).ToList();
            var streak = (await UnitOfWork.Repository<StudentStreak, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<StudentStreak, string>(s => s.StudentId == studentId))).FirstOrDefault();
            var badgeCount = await UnitOfWork.Repository<StudentBadge, string>().CountAsync(
                new CriteriaSpecification<StudentBadge, string>(b => b.StudentId == studentId));

            return Ok(new StudentPointsSummaryDto
            {
                StudentId = studentId,
                TotalPoints = txs.Sum(t => t.Points),
                TransactionCount = txs.Count,
                CurrentStreak = streak?.CurrentCount ?? 0,
                LongestStreak = streak?.LongestCount ?? 0,
                BadgeCount = badgeCount
            }, 200, "Points summary retrieved.");
        }

        public async Task<PaginationResponse<IEnumerable<PointTransactionDto>>> LedgerAsync(string studentId, PointLedgerParameters p, CancellationToken ct = default)
        {
            await _access.EnsureCanAccessStudentAsync(studentId, ct);
            Expression<Func<StudentPointTransaction, bool>> criteria = t => t.StudentId == studentId;
            var repo = UnitOfWork.Repository<StudentPointTransaction, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<StudentPointTransaction, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<StudentPointTransaction, string>(criteria, t => t.AwardedAt, p.PageNumber, p.PageSize, descending: true));
            var dto = items.Select(Map).ToList();
            return new PaginationResponse<IEnumerable<PointTransactionDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Point ledger retrieved." };
        }

        public async Task<ApiResponse<PointTransactionDto>> AwardManualAsync(string studentId, ManualAwardPointsDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            // Authorization: only a teacher (assigned students) or school admin may award.
            // A student can never reach this with a self-target because the role gate rejects students.
            if (!IsTeacher && !IsSchoolAdmin) throw new ForbiddenException("Only a teacher or school administrator may award points.");
            await _access.EnsureCanAccessStudentAsync(studentId, ct);

            if (dto.Points == 0) throw new BadRequestException("Points must be non-zero.");
            if (dto.Points < GamificationDefaults.ManualAwardMin || dto.Points > GamificationDefaults.ManualAwardMax)
                throw new BadRequestException($"Points must be between {GamificationDefaults.ManualAwardMin} and {GamificationDefaults.ManualAwardMax}.");
            if (string.IsNullOrWhiteSpace(dto.Reason)) throw new BadRequestException("Reason is required.");

            var key = string.IsNullOrWhiteSpace(dto.IdempotencyKey)
                ? $"manual:{Guid.NewGuid():N}"
                : $"manual:{dto.IdempotencyKey.Trim()}";

            var tx = await PointLedgerStaging.StageAsync(UnitOfWork, tenantId, studentId, dto.Points, dto.Reason.Trim(),
                PointSourceType.ManualAward, key, ct: ct);

            if (tx is null)
            {
                // Idempotent retry: the same key was already recorded; return the existing row.
                var existing = (await UnitOfWork.Repository<StudentPointTransaction, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<StudentPointTransaction, string>(t => t.IdempotencyKey == key))).First();
                return Ok(Map(existing), 200, "Points already awarded (idempotent).");
            }

            await Audit.StageAsync(AuditActionType.Create, nameof(StudentPointTransaction), tx.Id,
                $"{{\"action\":\"manual-award\",\"points\":{dto.Points}}}", ct);
            await StageNotificationAsync(tenantId, studentId, "Points awarded",
                $"You were awarded {dto.Points} point(s): {dto.Reason.Trim()}", NotificationCategory.General);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(tx), 201, "Points awarded.");
        }

        public async Task<ApiResponse<IEnumerable<PointLeaderboardRowDto>>> LeaderboardAsync(PointLeaderboardParameters p, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();

            HashSet<string>? gradeStudentIds = null;
            if (!string.IsNullOrWhiteSpace(p.GradeId))
            {
                var grade = p.GradeId!.Trim();
                gradeStudentIds = (await _users.Users
                        .OfType<Student>()
                        .Where(s => s.TenantId == tenantId && !s.IsDeleted && s.GradeId == grade)
                        .Select(s => s.Id)
                        .ToListAsync(ct))
                    .ToHashSet();
            }

            // Tenant-scoped by the global query filter. In-memory aggregation: acceptable at the
            // school/grade scale this serves; documented as a known scale consideration.
            var txs = (await UnitOfWork.Repository<StudentPointTransaction, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<StudentPointTransaction, string>(_ => true))).ToList();

            var rows = txs
                .Where(t => gradeStudentIds is null || gradeStudentIds.Contains(t.StudentId))
                .GroupBy(t => t.StudentId)
                .Select(g => new { StudentId = g.Key, Total = g.Sum(x => x.Points) })
                .OrderByDescending(x => x.Total)
                .ThenBy(x => x.StudentId, StringComparer.Ordinal)
                .Skip((p.PageNumber - 1) * p.PageSize)
                .Take(p.PageSize)
                .Select((x, i) => new PointLeaderboardRowDto
                {
                    StudentId = x.StudentId,
                    TotalPoints = x.Total,
                    Rank = ((p.PageNumber - 1) * p.PageSize) + i + 1
                })
                .ToList();

            // Resolve display names so the leaderboard shows real names, not raw ids.
            var rowIds = rows.Select(r => r.StudentId).ToList();
            if (rowIds.Count > 0)
            {
                var names = await _users.Users.Where(u => rowIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.FullName }).ToListAsync(ct);
                foreach (var r in rows)
                    r.StudentName = names.FirstOrDefault(n => n.Id == r.StudentId)?.FullName;
            }
            return Ok<IEnumerable<PointLeaderboardRowDto>>(rows, 200, "Leaderboard retrieved.");
        }

        public async Task<ApiResponse<IEnumerable<GamificationRuleDto>>> RulesAsync(CancellationToken ct = default)
        {
            RequireTenant();
            if (!IsSchoolAdmin && !IsTeacher) throw new ForbiddenException("Only a teacher or school administrator may view gamification rules.");
            var rules = await UnitOfWork.Repository<GamificationRule, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<GamificationRule, string>(_ => true));
            return Ok<IEnumerable<GamificationRuleDto>>(rules.Select(MapRule).ToList(), 200, "Gamification rules retrieved.");
        }

        public async Task<ApiResponse<GamificationRuleDto>> UpsertRuleAsync(UpsertGamificationRuleDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            if (!IsSchoolAdmin) throw new ForbiddenException("Only a school administrator may manage gamification rules.");
            if (string.IsNullOrWhiteSpace(dto.Code)) throw new BadRequestException("Code is required.");
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new BadRequestException("Name is required.");
            if (dto.Points < 0 || dto.Points > GamificationDefaults.ManualAwardMax)
                throw new BadRequestException($"Points must be between 0 and {GamificationDefaults.ManualAwardMax}.");

            var code = dto.Code.Trim();
            var rule = (await UnitOfWork.Repository<GamificationRule, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<GamificationRule, string>(r => r.Code == code))).FirstOrDefault();

            if (rule is null)
            {
                rule = new GamificationRule
                {
                    Id = Guid.NewGuid().ToString(), TenantId = tenantId, Code = code, Name = dto.Name.Trim(),
                    Description = dto.Description, Trigger = dto.Trigger, Points = dto.Points, BadgeId = dto.BadgeId, Enabled = dto.Enabled
                };
                await UnitOfWork.Repository<GamificationRule, string>().AddAsync(rule);
                await Audit.StageAsync(AuditActionType.Create, nameof(GamificationRule), rule.Id, ct: ct);
            }
            else
            {
                rule.Name = dto.Name.Trim();
                rule.Description = dto.Description;
                rule.Trigger = dto.Trigger;
                rule.Points = dto.Points;
                rule.BadgeId = dto.BadgeId;
                rule.Enabled = dto.Enabled;
                UnitOfWork.Repository<GamificationRule, string>().Update(rule);
                await Audit.StageAsync(AuditActionType.Update, nameof(GamificationRule), rule.Id, ct: ct);
            }
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(MapRule(rule), 200, "Gamification rule saved.");
        }

        // ---- helpers ----

        private static PointTransactionDto Map(StudentPointTransaction t) => new()
        {
            Id = t.Id, StudentId = t.StudentId, Points = t.Points, Reason = t.Reason,
            SourceType = t.SourceType, SourceId = t.SourceId, AwardedAt = t.AwardedAt
        };

        private static GamificationRuleDto MapRule(GamificationRule r) => new()
        {
            Id = r.Id, Code = r.Code, Name = r.Name, Description = r.Description,
            Trigger = r.Trigger, Points = r.Points, BadgeId = r.BadgeId, Enabled = r.Enabled
        };
    }
}
