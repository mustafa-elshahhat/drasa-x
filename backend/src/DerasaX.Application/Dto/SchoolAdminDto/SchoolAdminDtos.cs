using System;
using DerasaX.Application.Common;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Dto.SchoolAdminDto
{
    // =====================================================================
    // Phase 11 — School Admin Portal DTOs. These back the NEW school-admin
    // contracts only (aggregate dashboard, parent↔student relationship
    // management, teacher↔class assignment management). All other admin
    // pages reuse existing Phase 5 contracts and their existing DTOs.
    // =====================================================================

    /// <summary>
    /// Real aggregate summary of the caller's OWN tenant — every value is a count/read of
    /// authoritative data (no fabricated metrics). An empty tenant reports zeros.
    /// </summary>
    public class SchoolAdminDashboardDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public TenantStatus TenantStatus { get; set; }
        public string TenantType { get; set; } = string.Empty;

        // People
        public int Students { get; set; }
        public int Teachers { get; set; }
        public int Parents { get; set; }
        public int Admins { get; set; }

        // Academic structure
        public int Grades { get; set; }
        public int Subjects { get; set; }
        public int Classes { get; set; }
        public int AcademicYears { get; set; }
        public int Terms { get; set; }

        // Relationships & assignments
        public int ParentStudentLinks { get; set; }
        public int TeacherClassAssignments { get; set; }

        // Operations
        public int ActiveAnnouncements { get; set; }
        public int OpenParentRequests { get; set; }
        public int OpenSupportRequests { get; set; }

        // AI usage (tenant-scoped; recorded by Phase 6 orchestration)
        public int AiUsageRecords { get; set; }
        public int AiTotalTokens { get; set; }

        public DateTime GeneratedAt { get; set; }
    }

    // ---- Parent ↔ student relationships ----

    /// <summary>A parent↔student link as managed by the SchoolAdmin (no secret material).</summary>
    public class SchoolAdminRelationshipDto
    {
        public string Id { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;
        public string ParentName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool CanViewProgress { get; set; }
        public bool CanRequestDocuments { get; set; }
        public bool CanContactTeachers { get; set; }
        public bool IsActive { get; set; }
        public DateTime ActiveFrom { get; set; }
        public DateTime? ActiveTo { get; set; }
    }

    public class CreateRelationshipDto
    {
        public string ParentId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public GuardianRelationship Relationship { get; set; } = GuardianRelationship.Guardian;
        public bool IsPrimary { get; set; }
        public bool CanViewProgress { get; set; } = true;
        public bool CanRequestDocuments { get; set; } = true;
        public bool CanContactTeachers { get; set; } = true;
    }

    public class RelationshipParameters : PaginationParameters
    {
        public string? ParentId { get; set; }
        public string? StudentId { get; set; }
        /// <summary>When true (default), only active links are returned.</summary>
        public bool ActiveOnly { get; set; } = true;
    }

    // ---- Teacher ↔ class assignments ----

    /// <summary>A teacher↔class assignment as managed by the SchoolAdmin.</summary>
    public class SchoolAdminTeacherClassAssignmentDto
    {
        public string Id { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string SchoolClassId { get; set; } = string.Empty;
        public string SchoolClassName { get; set; } = string.Empty;
        public string? SubjectId { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime ActiveFrom { get; set; }
        public DateTime? ActiveTo { get; set; }
    }

    public class CreateTeacherClassAssignmentDto
    {
        public string TeacherId { get; set; } = string.Empty;
        public string SchoolClassId { get; set; } = string.Empty;
        public string? SubjectId { get; set; }
        public TeacherClassRole Role { get; set; } = TeacherClassRole.SubjectTeacher;
    }

    public class TeacherClassAssignmentParameters : PaginationParameters
    {
        public string? TeacherId { get; set; }
        public string? SchoolClassId { get; set; }
        public bool ActiveOnly { get; set; } = true;
    }
}
