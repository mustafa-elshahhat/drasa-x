using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.EngagementDto;
using DerasaX.Domain.Common;

namespace DerasaX.Application.Services.Abstractions.Engagement
{
    /// <summary>
    /// Phase 14 — ledger-based gamification: a student's points are the sum of immutable
    /// transactions (never a single mutable total). Manual awards require an authorized
    /// teacher/admin actor and are audited; automatic awards (competition rewards, office-hour
    /// attendance) are idempotent. Leaderboards are tenant-scoped and optionally grade-scoped.
    /// </summary>
    public interface IGamificationService
    {
        Task<ApiResponse<StudentPointsSummaryDto>> SummaryAsync(string studentId, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<PointTransactionDto>>> LedgerAsync(string studentId, PointLedgerParameters p, CancellationToken ct = default);
        Task<ApiResponse<PointTransactionDto>> AwardManualAsync(string studentId, ManualAwardPointsDto dto, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<PointLeaderboardRowDto>>> LeaderboardAsync(PointLeaderboardParameters p, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<GamificationRuleDto>>> RulesAsync(CancellationToken ct = default);
        Task<ApiResponse<GamificationRuleDto>> UpsertRuleAsync(UpsertGamificationRuleDto dto, CancellationToken ct = default);
    }
}
