using DerasaX.Application.Common;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Dto.ProvisioningDto
{
    /// <summary>Request for a SchoolAdmin to provision a new tenant Student/Teacher/Parent account.</summary>
    public class CreateTenantUserDto
    {
        public string FullName { get; set; } = string.Empty;
        /// <summary>Stable business login identifier (must be unique within the tenant).</summary>
        public string LoginCode { get; set; } = string.Empty;
        /// <summary>One of: Student, Teacher, Parent. Admin roles cannot be provisioned through this surface.</summary>
        public string Role { get; set; } = string.Empty;
        /// <summary>Required when Role == Student (the grade the student belongs to).</summary>
        public string? GradeId { get; set; }
        public Gender? Gender { get; set; }
    }

    /// <summary>A provisioned/managed tenant account (no secret material).</summary>
    public class TenantUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string LoginCode { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsDisabled { get; set; }
        public string? GradeId { get; set; }
    }

    /// <summary>
    /// Returned ONCE when an account is created or its credential is regenerated. The temporary
    /// password is shown a single time to the provisioning SchoolAdmin and is never persisted in
    /// clear text nor written to logs/audit.
    /// </summary>
    public class ProvisionedCredentialDto
    {
        public string UserId { get; set; } = string.Empty;
        public string LoginCode { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string TemporaryPassword { get; set; } = string.Empty;
    }

    public class TenantUserParameters : PaginationParameters
    {
        /// <summary>Optional role filter: Student | Teacher | Parent.</summary>
        public string? Role { get; set; }
        /// <summary>Include disabled accounts (default false).</summary>
        public bool IncludeDisabled { get; set; }
        /// <summary>Case-insensitive contains match on full name or login code.</summary>
        public string? Search { get; set; }
    }
}
