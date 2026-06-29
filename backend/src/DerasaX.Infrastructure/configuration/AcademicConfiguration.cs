using DerasaX.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DerasaX.Infrastructure.configuration
{
    // ---- Alternate keys on existing principals so dependents can use same-tenant composite FKs ----

    public class GradeConfiguration : IEntityTypeConfiguration<Grade>
    {
        public void Configure(EntityTypeBuilder<Grade> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
            builder.HasIndex(x => new { x.TenantId, x.Name });
        }
    }

    public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
    {
        public void Configure(EntityTypeBuilder<Subject> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
        }
    }

    // ---- Academic structure ----

    public class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
    {
        public void Configure(EntityTypeBuilder<AcademicYear> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Code).IsRequired().HasMaxLength(32);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
            builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        }
    }

    public class TermConfiguration : IEntityTypeConfiguration<Term>
    {
        public void Configure(EntityTypeBuilder<Term> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Code).IsRequired().HasMaxLength(32);
            builder.Property(x => x.AcademicYearId).IsRequired().HasMaxLength(450);

            builder.HasOne(x => x.AcademicYear)
                .WithMany(y => y.Terms)
                .HasForeignKey(x => new { x.TenantId, x.AcademicYearId })
                .HasPrincipalKey(y => new { y.TenantId, y.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.AcademicYearId, x.Code }).IsUnique();
        }
    }

    public class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
    {
        public void Configure(EntityTypeBuilder<SchoolClass> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Code).IsRequired().HasMaxLength(32);
            builder.Property(x => x.GradeId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.AcademicYearId).IsRequired().HasMaxLength(450);

            builder.HasAlternateKey(x => new { x.TenantId, x.Id });

            builder.HasOne(x => x.Grade)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.GradeId })
                .HasPrincipalKey(g => new { g.TenantId, g.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AcademicYear)
                .WithMany(y => y.Classes)
                .HasForeignKey(x => new { x.TenantId, x.AcademicYearId })
                .HasPrincipalKey(y => new { y.TenantId, y.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.AcademicYearId, x.Code }).IsUnique();
        }
    }

    public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
    {
        public void Configure(EntityTypeBuilder<Enrollment> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.SchoolClassId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.WithdrawalReason).HasMaxLength(512);

            // Same-tenant composite FK to the class (DB-enforced).
            builder.HasOne(x => x.SchoolClass)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(x => new { x.TenantId, x.SchoolClassId })
                .HasPrincipalKey(c => new { c.TenantId, c.Id })
                .OnDelete(DeleteBehavior.Restrict);

            // Student is a user: simple FK + DB trigger enforces same-tenant.
            builder.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.SchoolClassId }).IsUnique();
            builder.HasIndex(x => new { x.TenantId, x.AcademicYearId });
        }
    }

    // ---- User relationships ----

    public class ParentStudentRelationshipConfiguration : IEntityTypeConfiguration<ParentStudentRelationship>
    {
        public void Configure(EntityTypeBuilder<ParentStudentRelationship> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.ParentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);

            builder.HasOne(x => x.Parent)
                .WithMany()
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.ParentId, x.StudentId }).IsUnique();
            builder.HasIndex(x => new { x.TenantId, x.StudentId });
        }
    }

    public class TeacherSubjectAssignmentConfiguration : IEntityTypeConfiguration<TeacherSubjectAssignment>
    {
        public void Configure(EntityTypeBuilder<TeacherSubjectAssignment> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.TeacherId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.SubjectId).IsRequired().HasMaxLength(450);

            builder.HasOne(x => x.Subject)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SubjectId })
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.TeacherId, x.SubjectId }).IsUnique();
        }
    }

    public class TeacherClassAssignmentConfiguration : IEntityTypeConfiguration<TeacherClassAssignment>
    {
        public void Configure(EntityTypeBuilder<TeacherClassAssignment> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.TeacherId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.SchoolClassId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.SubjectId).HasMaxLength(450);

            builder.HasOne(x => x.SchoolClass)
                .WithMany(c => c.TeacherAssignments)
                .HasForeignKey(x => new { x.TenantId, x.SchoolClassId })
                .HasPrincipalKey(c => new { c.TenantId, c.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.TeacherId, x.SchoolClassId }).IsUnique();
        }
    }
}
