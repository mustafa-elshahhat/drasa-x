using System;
using System.Collections.Generic;
using DerasaX.Application.Common;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Dto.OperationsDto
{
    // ---- 7.1 Tenant & subscription ----

    public class TenantDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Domain { get; set; }
        public TenantStatus Status { get; set; }
        public CurriculumType Type { get; set; }
    }

    public class CreateTenantDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Domain { get; set; }
        public CurriculumType Type { get; set; } = CurriculumType.National;
    }

    public class PlanDto
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public SubscriptionPlan Tier { get; set; }
        public BillingPeriod BillingPeriod { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public int TrialDays { get; set; }
        public int? MaxStudents { get; set; }
        public int? MaxTeachers { get; set; }
        public int? MaxParents { get; set; }
        public int? MaxSchoolAdmins { get; set; }
        public int? MaxClasses { get; set; }
        public int? MaxSubjects { get; set; }
        public int? MaxLessonMaterials { get; set; }
        public int? MaxStorageMb { get; set; }
        public int? MaxAiGenerationsPerMonth { get; set; }
        public int? MaxAiTokensPerMonth { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreatePlanDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public SubscriptionPlan Tier { get; set; } = SubscriptionPlan.Free;
        public BillingPeriod BillingPeriod { get; set; } = BillingPeriod.Monthly;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public int TrialDays { get; set; }
        public bool IsActive { get; set; } = true;
        public int? MaxStudents { get; set; }
        public int? MaxTeachers { get; set; }
        public int? MaxParents { get; set; }
        public int? MaxSchoolAdmins { get; set; }
        public int? MaxClasses { get; set; }
        public int? MaxSubjects { get; set; }
        public int? MaxLessonMaterials { get; set; }
        public int? MaxStorageMb { get; set; }
        public int? MaxAiGenerationsPerMonth { get; set; }
        public int? MaxAiTokensPerMonth { get; set; }
    }

    public class UpdatePlanDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public SubscriptionPlan Tier { get; set; } = SubscriptionPlan.Free;
        public BillingPeriod BillingPeriod { get; set; } = BillingPeriod.Monthly;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public int TrialDays { get; set; }
        public bool IsActive { get; set; } = true;
        public int? MaxStudents { get; set; }
        public int? MaxTeachers { get; set; }
        public int? MaxParents { get; set; }
        public int? MaxSchoolAdmins { get; set; }
        public int? MaxClasses { get; set; }
        public int? MaxSubjects { get; set; }
        public int? MaxLessonMaterials { get; set; }
        public int? MaxStorageMb { get; set; }
        public int? MaxAiGenerationsPerMonth { get; set; }
        public int? MaxAiTokensPerMonth { get; set; }
    }

    public class AssignPlanDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string PlanDefinitionId { get; set; } = string.Empty;
        public bool IsTrial { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class SubscriptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string PlanDefinitionId { get; set; } = string.Empty;
        public SubscriptionStatus Status { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsTrial { get; set; }
        public bool AutoRenew { get; set; }
    }

    public class RenewalDto
    {
        public string Id { get; set; } = string.Empty;
        public string TenantSubscriptionId { get; set; } = string.Empty;
        public RenewalStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? NewExpiresAt { get; set; }
    }

    public class RequestRenewalDto
    {
        public DateTime? RequestedExpiresAt { get; set; }
        public string? Notes { get; set; }
    }

    public class ProcessRenewalDto
    {
        public RenewalStatus Status { get; set; }
        public DateTime? NewExpiresAt { get; set; }
        public string? Notes { get; set; }
    }

    public class UsageSummaryDto
    {
        public string TenantId { get; set; } = string.Empty;
        public int StudentsCount { get; set; }
        public int TeachersCount { get; set; }
        public int AiGenerationsUsed { get; set; }
        public int? MaxStudents { get; set; }
        public int? MaxAiGenerationsPerMonth { get; set; }
        public bool OverStudentLimit { get; set; }
        public long StorageUsedBytes { get; set; }
        public int? MaxStorageMb { get; set; }
        public bool OverStorageLimit { get; set; }
        public int AiTokensUsed { get; set; }
        public int? MaxAiTokensPerMonth { get; set; }
        public bool OverAiTokenLimit { get; set; }
    }

    public class TenantParameters : PaginationParameters
    {
        public TenantStatus? Status { get; set; }
    }

    // ---- 7.2 Support ----

    public class CreateSupportRequestDto
    {
        public RequestType Type { get; set; } = RequestType.TechnicalSupport;
        public string Message { get; set; } = string.Empty;
    }

    public class SupportRequestDto
    {
        public string Id { get; set; } = string.Empty;
        public string? TenantId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public RequestType Type { get; set; }
        public RequestStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ResponseMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    public class RespondSupportDto
    {
        public string ResponseMessage { get; set; } = string.Empty;
        public RequestStatus Status { get; set; } = RequestStatus.Completed;
    }

    public class SupportParameters : PaginationParameters
    {
        public RequestStatus? Status { get; set; }
    }

    // ---- 7.3 Audit ----

    public class AuditLogDto
    {
        public string Id { get; set; } = string.Empty;
        public string? TenantId { get; set; }
        public string? ActorUserId { get; set; }
        public AuditActionType Action { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; }
    }

    public class AuditParameters : PaginationParameters
    {
        public string? EntityType { get; set; }
        public AuditActionType? Action { get; set; }
        public string? ActorUserId { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    // ---- 7.4 AI usage ----

    public class RecordAiUsageDto
    {
        public AiUsageKind Kind { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? Model { get; set; }
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? TotalTokens { get; set; }
        public decimal? Cost { get; set; }
        public string? CorrelationId { get; set; }
        public bool Failed { get; set; }
        public int? LatencyMs { get; set; }
    }

    public class AiUsageDto
    {
        public string Id { get; set; } = string.Empty;
        public AiUsageKind Kind { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? Model { get; set; }
        public int? TotalTokens { get; set; }
        public decimal? Cost { get; set; }
        public DateTime UsedAt { get; set; }
    }

    public class AiUsageSummaryDto
    {
        public string TenantId { get; set; } = string.Empty;
        public int Records { get; set; }
        public int TotalTokens { get; set; }
        public decimal TotalCost { get; set; }
        public int? MonthlyLimit { get; set; }
        public bool OverLimit { get; set; }
    }

    public class AiUsageParameters : PaginationParameters
    {
        public AiUsageKind? Kind { get; set; }
        public string? Provider { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    // ---- 7.5 Settings & feature flags ----

    public class SettingDto
    {
        public string Id { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        /// <summary>Plaintext value, or "***REDACTED***" when the setting is marked secret.</summary>
        public string Value { get; set; } = string.Empty;
        public SettingValueType ValueType { get; set; }
        public bool IsSecret { get; set; }
    }

    public class UpsertSettingDto
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public SettingValueType ValueType { get; set; } = SettingValueType.String;
        public bool IsSecret { get; set; }
    }

    public class FeatureFlagDto
    {
        public string Id { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string? TargetTenantId { get; set; }
    }

    public class UpsertFeatureFlagDto
    {
        public string Key { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string? TargetTenantId { get; set; }
    }

    public class FeatureEvaluationDto
    {
        public string Key { get; set; } = string.Empty;
        public bool Enabled { get; set; }
    }

    // ---- 7.6 File metadata ----

    public class CreateFileRecordDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public FileRecordType Type { get; set; } = FileRecordType.Other;
        public string? ChecksumSha256 { get; set; }
    }

    public class FileRecordDto
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public FileRecordType Type { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; }
        /// <summary>Opaque storage key — never an insecure permanent public URL (Phase 16 owns delivery).</summary>
        public string StorageKey { get; set; } = string.Empty;
    }

    public class FileParameters : PaginationParameters
    {
        public FileRecordType? Type { get; set; }
    }

    // ---- 7.7 Reports ----

    public class ReportParameters
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class TenantUsersReportDto
    {
        public int Students { get; set; }
        public int Teachers { get; set; }
        public int Parents { get; set; }
        public int Admins { get; set; }
    }

    public class ActivityReportDto
    {
        public string Kind { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
