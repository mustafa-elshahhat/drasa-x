using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Dto.SystemAdminDto;
using DerasaX.Domain.Common;

namespace DerasaX.Application.Services.Abstractions.SystemAdminPortal
{
    /// <summary>
    /// Phase 12 — System Admin (platform) Portal service. Adds ONLY the genuinely-missing platform
    /// contracts; tenant lifecycle (create/get/list/activate/suspend/reactivate/assign-plan/usage),
    /// plans, platform audit, system settings and feature flags REUSE the existing Phase 5 §14
    /// SystemAdmin surface. Every method is platform-scope (SystemAdmin only) and audits sensitive
    /// mutations. No data is fabricated.
    /// </summary>
    public interface ISystemAdminPortalService
    {
        Task<ApiResponse<PlatformDashboardDto>> DashboardAsync(CancellationToken ct = default);
        Task<ApiResponse<PlatformUsageDto>> PlatformUsageAsync(CancellationToken ct = default);
        Task<ApiResponse<PlatformAiUsageDto>> PlatformAiUsageAsync(CancellationToken ct = default);
        Task<ApiResponse<PlatformStorageDto>> PlatformStorageAsync(CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<PlatformSubscriptionRowDto>>> ListSubscriptionsAsync(CancellationToken ct = default);

        // Cross-tenant support inbox (platform scope).
        Task<PaginationResponse<IEnumerable<SupportRequestDto>>> ListSupportTicketsAsync(SystemSupportParameters p, CancellationToken ct = default);
        Task<ApiResponse<SupportRequestDto>> RespondSupportTicketAsync(string id, RespondSupportDto dto, CancellationToken ct = default);

        // Platform announcements (durable, audited via the platform SystemSetting store).
        Task<ApiResponse<IEnumerable<PlatformAnnouncementDto>>> ListAnnouncementsAsync(CancellationToken ct = default);
        Task<ApiResponse<PlatformAnnouncementDto>> CreateAnnouncementAsync(CreatePlatformAnnouncementDto dto, CancellationToken ct = default);

        // Onboarding: create the INITIAL SchoolAdmin for a target tenant.
        Task<ApiResponse<CreatedSchoolAdminDto>> CreateSchoolAdminAsync(string tenantId, CreateSchoolAdminDto dto, CancellationToken ct = default);

        // Reset an existing SchoolAdmin's credential (mirrors the tenant-users reset-credential surface).
        Task<ApiResponse<CreatedSchoolAdminDto>> ResetSchoolAdminCredentialAsync(string tenantId, string userId, CancellationToken ct = default);

        // Operational posture: real DB-readiness check + honest deferred states.
        Task<ApiResponse<OperationalStatusDto>> OperationalStatusAsync(CancellationToken ct = default);

        // SAFE, non-destructive tenant data workflow.
        Task<ApiResponse<TenantDataRequestDto>> ExportTenantDataAsync(string tenantId, CancellationToken ct = default);
        Task<ApiResponse<TenantDataRequestDto>> RequestTenantDeletionAsync(string tenantId, CancellationToken ct = default);
    }
}
