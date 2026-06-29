namespace DerasaX.Application.Common
{
    /// <summary>Canonical role names (match seeded Identity roles and JWT <c>role</c> claim).</summary>
    public static class Roles
    {
        public const string Student = "Student";
        public const string Teacher = "Teacher";
        public const string Parent = "Parent";
        public const string SchoolAdmin = "SchoolAdmin";
        public const string SystemAdmin = "SystemAdmin";
    }

    /// <summary>Authorization policy names (Phase 2 AUTHORIZATION_MATRIX §2).</summary>
    public static class Policies
    {
        public const string StudentOnly = "StudentOnly";
        public const string TeacherOnly = "TeacherOnly";
        public const string ParentOnly = "ParentOnly";
        public const string SchoolAdminOnly = "SchoolAdminOnly";
        public const string SystemAdminOnly = "SystemAdminOnly";
        public const string TeacherOrSchoolAdmin = "TeacherOrSchoolAdmin";
        public const string TenantStaff = "TenantStaff";
        public const string TenantMember = "TenantMember";

        /// <summary>
        /// Self-account operations (change own password, revoke own session). Any
        /// authenticated user qualifies — including a platform SystemAdmin that has no
        /// tenant claim — because these act only on the caller's own account, with the
        /// identity taken from the trusted JWT. This intentionally does NOT grant access
        /// to tenant-domain routes, which keep their role/tenant policies.
        /// </summary>
        public const string SelfAccount = "SelfAccount";
    }

    /// <summary>Stable machine error codes (Phase 2 ERROR_CONTRACT §3).</summary>
    public static class ErrorCodes
    {
        public const string ValidationError = "VALIDATION_ERROR";
        public const string BadRequest = "BAD_REQUEST";
        public const string Unauthenticated = "UNAUTHENTICATED";
        public const string Forbidden = "FORBIDDEN";
        public const string NotFound = "NOT_FOUND";
        public const string Conflict = "CONFLICT";
        public const string RateLimited = "RATE_LIMITED";
        public const string InternalError = "INTERNAL_ERROR";
        public const string AiUnavailable = "AI_UNAVAILABLE";
        public const string TenantSuspended = "TENANT_SUSPENDED";
        public const string AccountLocked = "ACCOUNT_LOCKED";
        public const string AccountDisabled = "ACCOUNT_DISABLED";
        // Phase 16 — durable file storage provider is selected but unreachable / unconfigured.
        public const string StorageUnavailable = "STORAGE_UNAVAILABLE";
    }
}
