using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AcademicDto;
using DerasaX.Domain.Common;

namespace DerasaX.Application.Services.Abstractions.Academic
{
    public interface IAcademicYearService
    {
        Task<PaginationResponse<IEnumerable<AcademicYearDto>>> ListAsync(AcademicYearParameters parameters, CancellationToken ct = default);
        Task<ApiResponse<AcademicYearDto>> GetByIdAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<AcademicYearDto>> CreateAsync(AddAcademicYearDto dto, CancellationToken ct = default);
        Task<ApiResponse<AcademicYearDto>> UpdateAsync(UpdateAcademicYearDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> ArchiveAsync(string id, CancellationToken ct = default);
    }

    public interface ITermService
    {
        Task<PaginationResponse<IEnumerable<TermDto>>> ListAsync(TermParameters parameters, CancellationToken ct = default);
        Task<ApiResponse<TermDto>> GetByIdAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<TermDto>> CreateAsync(AddTermDto dto, CancellationToken ct = default);
        Task<ApiResponse<TermDto>> UpdateAsync(UpdateTermDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> ArchiveAsync(string id, CancellationToken ct = default);
    }

    public interface ISchoolClassService
    {
        Task<PaginationResponse<IEnumerable<SchoolClassDto>>> ListAsync(SchoolClassParameters parameters, CancellationToken ct = default);
        Task<ApiResponse<SchoolClassDto>> GetByIdAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<SchoolClassDto>> CreateAsync(AddSchoolClassDto dto, CancellationToken ct = default);
        Task<ApiResponse<SchoolClassDto>> UpdateAsync(UpdateSchoolClassDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> ArchiveAsync(string id, CancellationToken ct = default);
    }

    public interface IEnrollmentService
    {
        Task<PaginationResponse<IEnumerable<EnrollmentDto>>> ListAsync(EnrollmentParameters parameters, CancellationToken ct = default);
        Task<ApiResponse<EnrollmentDto>> EnrollAsync(AddEnrollmentDto dto, CancellationToken ct = default);
        Task<ApiResponse<EnrollmentDto>> WithdrawAsync(WithdrawEnrollmentDto dto, CancellationToken ct = default);
    }

    public interface ITeacherAssignmentService
    {
        Task<PaginationResponse<IEnumerable<TeacherSubjectAssignmentDto>>> ListAsync(TeacherSubjectAssignmentParameters parameters, CancellationToken ct = default);
        Task<ApiResponse<TeacherSubjectAssignmentDto>> AssignAsync(AddTeacherSubjectAssignmentDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> DeactivateAsync(string id, CancellationToken ct = default);
    }
}
