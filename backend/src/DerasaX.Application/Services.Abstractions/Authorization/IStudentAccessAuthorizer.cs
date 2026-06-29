using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DerasaX.Application.Services.Abstractions.Authorization
{
    /// <summary>
    /// The single source of truth for "may the current caller see this student's data?".
    /// Centralises the relationship rules so no controller/service re-implements them:
    /// a student sees only itself; a teacher only students enrolled in classes they are
    /// assigned to; a parent only linked children (with view permission); a SchoolAdmin
    /// any same-tenant student; a platform SystemAdmin is never silently granted access via
    /// tenant-member routes. Cross-tenant / unknown students resolve to a safe 404; an
    /// in-tenant student the caller has no relationship with resolves to 403.
    /// </summary>
    public interface IStudentAccessAuthorizer
    {
        /// <summary>Throws 404 if the student is not visible in the tenant, 403 if the caller has no permitted relationship.</summary>
        Task EnsureCanAccessStudentAsync(string studentId, CancellationToken ct = default);

        /// <summary>
        /// Resolves the set of students the caller may aggregate over. <c>AllTenant</c> is true
        /// for a SchoolAdmin (rely on the tenant query filter rather than an explicit id list);
        /// otherwise <c>StudentIds</c> is the explicit constrained set (self / assigned / linked).
        /// </summary>
        Task<StudentScope> ResolveScopeAsync(CancellationToken ct = default);
    }

    /// <summary>The students a caller may aggregate over (see <see cref="IStudentAccessAuthorizer.ResolveScopeAsync"/>).</summary>
    public sealed record StudentScope(bool AllTenant, IReadOnlyList<string> StudentIds);
}
