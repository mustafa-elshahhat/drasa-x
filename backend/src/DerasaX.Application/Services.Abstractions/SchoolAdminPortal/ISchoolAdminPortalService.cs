using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.SchoolAdminDto;
using DerasaX.Domain.Common;

namespace DerasaX.Application.Services.Abstractions.SchoolAdminPortal
{
    /// <summary>
    /// Phase 11 — School Admin Portal contracts that did not exist before: a real aggregate
    /// tenant dashboard, parent↔student relationship management, and teacher↔class assignment
    /// management. All operations are tenant-scoped from the trusted token (SchoolAdminOnly),
    /// re-validate referenced users/classes as same-tenant (404 on mismatch, no leak), and are
    /// audited in the same transaction as the change. Every other admin surface reuses the
    /// existing Phase 5 contracts.
    /// </summary>
    public interface ISchoolAdminPortalService
    {
        Task<ApiResponse<SchoolAdminDashboardDto>> DashboardAsync(CancellationToken ct = default);

        // Parent ↔ student relationships
        Task<ApiResponse<IEnumerable<SchoolAdminRelationshipDto>>> ListRelationshipsAsync(RelationshipParameters p, CancellationToken ct = default);
        Task<ApiResponse<SchoolAdminRelationshipDto>> CreateRelationshipAsync(CreateRelationshipDto dto, CancellationToken ct = default);
        Task<ApiResponse<SchoolAdminRelationshipDto>> DeactivateRelationshipAsync(string id, CancellationToken ct = default);

        // Teacher ↔ class assignments
        Task<ApiResponse<IEnumerable<SchoolAdminTeacherClassAssignmentDto>>> ListClassAssignmentsAsync(TeacherClassAssignmentParameters p, CancellationToken ct = default);
        Task<ApiResponse<SchoolAdminTeacherClassAssignmentDto>> CreateClassAssignmentAsync(CreateTeacherClassAssignmentDto dto, CancellationToken ct = default);
        Task<ApiResponse<SchoolAdminTeacherClassAssignmentDto>> DeactivateClassAssignmentAsync(string id, CancellationToken ct = default);
    }
}
