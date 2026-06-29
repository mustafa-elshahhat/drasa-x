using System;
using System.Collections.Generic;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Dto.SystemAdminDto
{
    // =====================================================================
    // Phase 12 — System Admin (platform) Portal DTOs.
    // These back the genuinely-missing platform contracts (aggregate
    // dashboard, platform usage/AI/storage roll-ups, cross-tenant support
    // inbox, platform announcements, create-initial-school-admin, operational
    // status, and the safe non-destructive tenant data export/deletion
    // request). Everything else REUSES the existing Phase 5 §14 operations
    // DTOs (TenantDto, PlanDto, SubscriptionDto, UsageSummaryDto, SupportRequestDto,
    // AuditLogDto, SettingDto, FeatureFlagDto). No metric here is fabricated:
    // an empty platform returns zeros.
    // =====================================================================

    public class PlatformDashboardDto
    {
        // Tenants by lifecycle state
        public int TenantsTotal { get; set; }
        public int TenantsActive { get; set; }
        public int TenantsSuspended { get; set; }
        public int TenantsArchived { get; set; }

        // Users by role (platform-wide, excluding soft-deleted)
        public int Students { get; set; }
        public int Teachers { get; set; }
        public int Parents { get; set; }
        public int SchoolAdmins { get; set; }
        public int SystemAdmins { get; set; }

        // Plans & subscriptions
        public int PlansTotal { get; set; }
        public int PlansActive { get; set; }
        public int SubscriptionsTotal { get; set; }
        public int SubscriptionsActive { get; set; }
        public int SubscriptionsTrial { get; set; }

        // AI usage (platform-wide)
        public int AiUsageRecords { get; set; }
        public long AiTotalTokens { get; set; }
        public decimal AiTotalCost { get; set; }

        // Support
        public int SupportTotal { get; set; }
        public int SupportOpen { get; set; }

        // Recent platform audit activity (real rows from platform-audit)
        public int RecentAuditEvents { get; set; }
        public List<AuditLogDto> RecentActivity { get; set; } = new();

        public DateTime GeneratedAt { get; set; }
    }

    public class PlatformUsageRowDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public TenantStatus Status { get; set; }
        public int StudentsCount { get; set; }
        public int TeachersCount { get; set; }
        public int AiGenerationsUsed { get; set; }
        public int? MaxStudents { get; set; }
        public int? MaxAiGenerationsPerMonth { get; set; }
        public bool OverStudentLimit { get; set; }
    }

    public class PlatformUsageDto
    {
        public int TenantsCount { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalAiGenerations { get; set; }
        public List<PlatformUsageRowDto> Tenants { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class TenantAiUsageRowDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public int Records { get; set; }
        public long TotalTokens { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class PlatformAiUsageDto
    {
        public int Records { get; set; }
        public long TotalTokens { get; set; }
        public decimal TotalCost { get; set; }
        public List<TenantAiUsageRowDto> Tenants { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Honest platform storage posture. Per-tenant BYTE accounting is NOT implemented yet
    /// (object/file storage + delivery is the Phase 16 deliverable); this reports the plan
    /// <c>MaxStorageMb</c> ceilings that DO exist and is explicit that byte usage is not measured.
    /// </summary>
    public class TenantStorageRowDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public int? MaxStorageMb { get; set; }
        public int FileRecords { get; set; }
        public long DeclaredBytes { get; set; }
    }

    public class PlatformStorageDto
    {
        public bool ByteAccountingImplemented { get; set; }
        public string Note { get; set; } = string.Empty;
        public List<TenantStorageRowDto> Tenants { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class PlatformSubscriptionRowDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string SubscriptionId { get; set; } = string.Empty;
        public string PlanDefinitionId { get; set; } = string.Empty;
        public string PlanCode { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public SubscriptionStatus Status { get; set; }
        public bool IsTrial { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class SystemSupportParameters : PaginationParameters
    {
        public RequestStatus? Status { get; set; }
        public string? TenantId { get; set; }
    }

    public class CreateSchoolAdminDto
    {
        public string FullName { get; set; } = string.Empty;
        public string LoginCode { get; set; } = string.Empty;
    }

    /// <summary>One-time credential returned ONCE on creation; never persisted in clear text or audited.</summary>
    public class CreatedSchoolAdminDto
    {
        public string UserId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string LoginCode { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string TemporaryPassword { get; set; } = string.Empty;
    }

    public class PlatformAnnouncementDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class CreatePlatformAnnouncementDto
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    public class ServicePostureDto
    {
        public bool Configured { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }

    public class HealthStatusDto
    {
        public string Api { get; set; } = "up";
        public bool DatabaseReachable { get; set; }
        public string DatabaseNote { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; }
    }

    public class OperationalStatusDto
    {
        public HealthStatusDto Health { get; set; } = new();
        public ServicePostureDto ErrorMonitoring { get; set; } = new();
        public ServicePostureDto Backups { get; set; } = new();
        public ServicePostureDto SecurityEvents { get; set; } = new();

        // Phase 19 — observability surface (populated by the API layer from the health-check
        // report + runtime metrics + deployment info; the service leaves them at defaults).
        public ServicePostureDto Storage { get; set; } = new();
        public ServicePostureDto AiService { get; set; } = new();
        public ServicePostureDto BackgroundJobs { get; set; } = new();
        public RuntimeMetricsDto Metrics { get; set; } = new();
        public string Version { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public double UptimeSeconds { get; set; }

        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>Phase 19 — process-local request metrics summary (no PII, no secrets).</summary>
    public class RuntimeMetricsDto
    {
        public long TotalRequests { get; set; }
        public long Status2xx { get; set; }
        public long Status3xx { get; set; }
        public long Status4xx { get; set; }
        public long Status5xx { get; set; }
        public double AvgLatencyMs { get; set; }
    }

    /// <summary>
    /// SAFE, NON-DESTRUCTIVE tenant data request. An export returns a real preview of what the
    /// export WOULD contain (entity counts) and records an audited <c>Export</c> event. A deletion
    /// request ONLY records an audited request for manual platform approval — it never deletes any
    /// tenant data in this phase (execution rule 10).
    /// </summary>
    public class TenantDataRequestDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty; // "export" | "deletion-request"
        public string Status { get; set; } = string.Empty;       // "preview-generated" | "request-recorded"
        public bool Destructive { get; set; }                     // always false in Phase 12
        public Dictionary<string, int> Preview { get; set; } = new();
        public string Note { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
    }
}
