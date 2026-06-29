using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.ParentDto;
using DerasaX.Application.Dto.ProgressDto;
using DerasaX.Domain.Common;

namespace DerasaX.Application.Services.Abstractions.ParentPortal
{
    /// <summary>
    /// Phase 10 — parent-scoped portal reads. Every method authorizes by the
    /// parent's ACTIVE, progress-permitted parent-student links and the tenant
    /// claim. A parent only sees children explicitly linked to them. Cross-tenant /
    /// unknown child → 404; same-tenant but unlinked child → 403.
    /// </summary>
    public interface IParentPortalService
    {
        Task<ApiResponse<ParentDashboardDto>> DashboardAsync(CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<ParentChildDto>>> ChildrenAsync(CancellationToken ct = default);

        /// <summary>
        /// Detailed overview of one linked child. Cross-tenant / unknown → 404;
        /// same-tenant but unlinked → 403.
        /// </summary>
        Task<ApiResponse<ParentChildOverviewDto>> ChildOverviewAsync(string childId, CancellationToken ct = default);

        /// <summary>
        /// Read-only attendance for one linked child (parent-readable; no writes).
        /// Cross-tenant / unknown → 404; same-tenant but unlinked → 403.
        /// </summary>
        Task<ApiResponse<StudentAttendanceDto>> ChildAttendanceAsync(string childId, ProgressParameters p, CancellationToken ct = default);
    }
}
