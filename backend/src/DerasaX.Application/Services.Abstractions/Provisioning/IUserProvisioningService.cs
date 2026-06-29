using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.ProvisioningDto;
using DerasaX.Domain.Common;

namespace DerasaX.Application.Services.Abstractions.Provisioning
{
    /// <summary>
    /// Phase 5 — SchoolAdmin provisioning of tenant Student/Teacher/Parent accounts and their
    /// credentials. Strictly tenant-scoped: a SchoolAdmin can only act on their own tenant, and
    /// can never create platform/tenant-admin accounts. Credentials are returned once and never
    /// logged.
    /// </summary>
    public interface IUserProvisioningService
    {
        Task<ApiResponse<ProvisionedCredentialDto>> CreateAsync(CreateTenantUserDto dto, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<TenantUserDto>>> ListAsync(TenantUserParameters p, CancellationToken ct = default);
        Task<ApiResponse<TenantUserDto>> GetAsync(string userId, CancellationToken ct = default);
        Task<ApiResponse<TenantUserDto>> SetEnabledAsync(string userId, bool enabled, CancellationToken ct = default);
        Task<ApiResponse<ProvisionedCredentialDto>> ResetCredentialAsync(string userId, CancellationToken ct = default);
    }
}
