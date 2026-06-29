using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.TeacherDto;
using DerasaX.Domain.Common;

namespace DerasaX.Application.Services.Abstractions.TeacherPortal
{
    /// <summary>
    /// Phase 9 — teacher-scoped portal reads. Every method authorizes by the
    /// teacher's active assignments and the tenant claim; SchoolAdmin is tenant-wide.
    /// </summary>
    public interface ITeacherPortalService
    {
        Task<ApiResponse<TeacherDashboardDto>> DashboardAsync(CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<TeacherClassDto>>> MyClassesAsync(CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<TeacherSubjectDto>>> MySubjectsAsync(CancellationToken ct = default);

        /// <summary>
        /// Students actively enrolled in a class the teacher is assigned to.
        /// Cross-tenant / unknown class → 404; same-tenant but unassigned → 403.
        /// </summary>
        Task<ApiResponse<IEnumerable<TeacherStudentDto>>> ClassStudentsAsync(string classId, CancellationToken ct = default);
    }
}
