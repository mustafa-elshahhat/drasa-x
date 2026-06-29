using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;
using System;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// Links a parent/guardian to a student. Many-to-many: a parent may have multiple
    /// children and a student may have multiple permitted guardians. Tenant-owned;
    /// same-tenant integrity for both users is enforced by a database trigger. The
    /// permission flags gate what the guardian may do (view progress, request docs).
    /// </summary>
    public class ParentStudentRelationship : AuditableEntity<string>
    {
        public string ParentId { get; set; } = string.Empty;
        public Parent? Parent { get; set; }

        public string StudentId { get; set; } = string.Empty;
        public Student? Student { get; set; }

        public GuardianRelationship Relationship { get; set; } = GuardianRelationship.Guardian;
        public bool IsPrimary { get; set; }
        public bool CanViewProgress { get; set; } = true;
        public bool CanRequestDocuments { get; set; } = true;
        public bool CanContactTeachers { get; set; } = true;

        public bool IsActive { get; set; } = true;
        public DateTime ActiveFrom { get; set; }
        public DateTime? ActiveTo { get; set; }
    }
}
