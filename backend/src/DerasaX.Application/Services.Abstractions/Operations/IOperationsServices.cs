using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Domain.Common;

namespace DerasaX.Application.Services.Abstractions.Operations
{
    /// <summary>SystemAdmin platform administration of tenants and subscriptions.</summary>
    public interface ITenantAdminService
    {
        Task<PaginationResponse<IEnumerable<TenantDto>>> ListTenantsAsync(TenantParameters p, CancellationToken ct = default);
        Task<ApiResponse<TenantDto>> GetTenantAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<TenantDto>> CreateTenantAsync(CreateTenantDto dto, CancellationToken ct = default);
        Task<ApiResponse<TenantDto>> SetStatusAsync(string id, Domain.Enums.TenantStatus status, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<PlanDto>>> ListPlansAsync(CancellationToken ct = default);
        Task<ApiResponse<PlanDto>> CreatePlanAsync(CreatePlanDto dto, CancellationToken ct = default);
        Task<ApiResponse<PlanDto>> UpdatePlanAsync(string id, UpdatePlanDto dto, CancellationToken ct = default);
        Task<ApiResponse<SubscriptionDto>> AssignPlanAsync(AssignPlanDto dto, CancellationToken ct = default);
        Task<ApiResponse<SubscriptionDto>> GetSubscriptionAsync(string tenantId, CancellationToken ct = default);
        Task<ApiResponse<RenewalDto>> ProcessRenewalAsync(string renewalId, ProcessRenewalDto dto, CancellationToken ct = default);
        Task<ApiResponse<UsageSummaryDto>> TenantUsageAsync(string tenantId, CancellationToken ct = default);
    }

    /// <summary>SchoolAdmin self-service for the caller's own tenant.</summary>
    public interface ITenantSelfService
    {
        Task<ApiResponse<TenantDto>> CurrentTenantAsync(CancellationToken ct = default);
        Task<ApiResponse<SubscriptionDto>> CurrentSubscriptionAsync(CancellationToken ct = default);
        Task<ApiResponse<UsageSummaryDto>> CurrentUsageAsync(CancellationToken ct = default);
        Task<ApiResponse<RenewalDto>> RequestRenewalAsync(RequestRenewalDto dto, CancellationToken ct = default);
    }

    public interface ISupportService
    {
        Task<ApiResponse<SupportRequestDto>> CreateAsync(CreateSupportRequestDto dto, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<SupportRequestDto>>> ListAsync(SupportParameters p, CancellationToken ct = default);
        Task<ApiResponse<SupportRequestDto>> GetAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<SupportRequestDto>> RespondAsync(string id, RespondSupportDto dto, CancellationToken ct = default);
    }

    public interface IAuditQueryService
    {
        Task<PaginationResponse<IEnumerable<AuditLogDto>>> QueryAsync(AuditParameters p, bool platformScope, CancellationToken ct = default);
    }

    public interface IAiUsageService
    {
        Task<ApiResponse<AiUsageDto>> RecordAsync(RecordAiUsageDto dto, CancellationToken ct = default);

        /// <summary>
        /// Records AI usage on behalf of the internal AI orchestrator during a
        /// user-initiated request (e.g. a student tutor call). Unlike
        /// <see cref="RecordAsync"/> this does NOT require the SchoolAdmin role —
        /// it is invoked only by trusted server-side orchestration, still strictly
        /// scoped to the caller's tenant from the request context.
        /// </summary>
        Task<AiUsageDto> RecordInternalAsync(RecordAiUsageDto dto, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<AiUsageDto>>> ListAsync(AiUsageParameters p, CancellationToken ct = default);
        Task<ApiResponse<AiUsageSummaryDto>> SummaryAsync(CancellationToken ct = default);
    }

    public interface ISettingsService
    {
        Task<ApiResponse<IEnumerable<SettingDto>>> TenantSettingsAsync(CancellationToken ct = default);
        Task<ApiResponse<SettingDto>> UpsertTenantSettingAsync(UpsertSettingDto dto, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<SettingDto>>> SystemSettingsAsync(CancellationToken ct = default);
        Task<ApiResponse<SettingDto>> UpsertSystemSettingAsync(UpsertSettingDto dto, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<FeatureFlagDto>>> FeatureFlagsAsync(CancellationToken ct = default);
        Task<ApiResponse<FeatureFlagDto>> UpsertFeatureFlagAsync(UpsertFeatureFlagDto dto, CancellationToken ct = default);
        Task<ApiResponse<FeatureEvaluationDto>> EvaluateFeatureAsync(string key, CancellationToken ct = default);
    }

    public interface IFileMetadataService
    {
        Task<ApiResponse<FileRecordDto>> CreateAsync(CreateFileRecordDto dto, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<FileRecordDto>>> ListAsync(FileParameters p, CancellationToken ct = default);
        Task<ApiResponse<FileRecordDto>> GetAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<bool>> ArchiveAsync(string id, CancellationToken ct = default);
    }

    /// <summary>
    /// Enforces the current tenant's subscription-plan limits at the write-time
    /// service boundary (not just display). Resolves the tenant's latest
    /// <c>TenantSubscription</c> -&gt; <c>SubscriptionPlanDefinition</c> once per check;
    /// a tenant with no subscription at all is treated as unlimited (backward
    /// compatible with tenants that predate plan assignment). Throws
    /// <see cref="DerasaX.Domain.Exceptions.PlanLimitExceededException"/> (409) when a
    /// write would meet or exceed the plan's limit.
    /// </summary>
    public interface IPlanLimitEnforcer
    {
        /// <summary>role: Roles.Student, Roles.Teacher, Roles.Parent, or Roles.SchoolAdmin.</summary>
        Task EnsureCanAddUserAsync(string tenantId, string role, CancellationToken ct = default);
        Task EnsureCanAddClassAsync(string tenantId, CancellationToken ct = default);
        Task EnsureCanAddSubjectAsync(string tenantId, CancellationToken ct = default);
        Task EnsureCanAddLessonMaterialAsync(string tenantId, CancellationToken ct = default);
        Task EnsureCanUploadBytesAsync(string tenantId, long additionalBytes, CancellationToken ct = default);
        /// <summary>Checks both the request-count and token-sum ceilings for the current UTC calendar month.</summary>
        Task EnsureWithinAiMonthlyQuotaAsync(string tenantId, CancellationToken ct = default);
    }

    public interface IReportService
    {
        Task<ApiResponse<TenantUsersReportDto>> TenantUsersAsync(CancellationToken ct = default);
        Task<ApiResponse<ActivityReportDto>> AssessmentSummaryAsync(ReportParameters p, CancellationToken ct = default);
        Task<ApiResponse<ActivityReportDto>> AuditActivityAsync(ReportParameters p, CancellationToken ct = default);
        Task<ApiResponse<ActivityReportDto>> AiUsageActivityAsync(ReportParameters p, CancellationToken ct = default);
    }
}
