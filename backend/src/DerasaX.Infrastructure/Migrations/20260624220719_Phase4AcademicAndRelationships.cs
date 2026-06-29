using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase4AcademicAndRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_subjects_TenantId_Id",
                table: "subjects",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_grades_TenantId_Id",
                table: "grades",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "academicYears",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_academicYears", x => x.Id);
                    table.UniqueConstraint("AK_academicYears_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_academicYears_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parentStudentRelationships",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ParentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Relationship = table.Column<string>(type: "text", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CanViewProgress = table.Column<bool>(type: "boolean", nullable: false),
                    CanRequestDocuments = table.Column<bool>(type: "boolean", nullable: false),
                    CanContactTeachers = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ActiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parentStudentRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_parentStudentRelationships_Parent_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Parent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_parentStudentRelationships_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_parentStudentRelationships_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teacherSubjectAssignments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TeacherId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SubjectId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AcademicYearId = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ActiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teacherSubjectAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_teacherSubjectAssignments_Teacher_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teacher",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teacherSubjectAssignments_subjects_TenantId_SubjectId",
                        columns: x => new { x.TenantId, x.SubjectId },
                        principalTable: "subjects",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teacherSubjectAssignments_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "schoolClasses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    GradeId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AcademicYearId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schoolClasses", x => x.Id);
                    table.UniqueConstraint("AK_schoolClasses_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_schoolClasses_academicYears_TenantId_AcademicYearId",
                        columns: x => new { x.TenantId, x.AcademicYearId },
                        principalTable: "academicYears",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_schoolClasses_grades_TenantId_GradeId",
                        columns: x => new { x.TenantId, x.GradeId },
                        principalTable: "grades",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_schoolClasses_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "terms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcademicYearId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_terms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_terms_academicYears_TenantId_AcademicYearId",
                        columns: x => new { x.TenantId, x.AcademicYearId },
                        principalTable: "academicYears",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_terms_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "enrollments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SchoolClassId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AcademicYearId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WithdrawnAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WithdrawalReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_enrollments_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_enrollments_schoolClasses_TenantId_SchoolClassId",
                        columns: x => new { x.TenantId, x.SchoolClassId },
                        principalTable: "schoolClasses",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_enrollments_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teacherClassAssignments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TeacherId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SchoolClassId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SubjectId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Role = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ActiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teacherClassAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_teacherClassAssignments_Teacher_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teacher",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teacherClassAssignments_schoolClasses_TenantId_SchoolClassId",
                        columns: x => new { x.TenantId, x.SchoolClassId },
                        principalTable: "schoolClasses",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teacherClassAssignments_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_grades_TenantId_Name",
                table: "grades",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_academicYears_TenantId",
                table: "academicYears",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_academicYears_TenantId_Code",
                table: "academicYears",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_StudentId",
                table: "enrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_TenantId",
                table: "enrollments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_TenantId_AcademicYearId",
                table: "enrollments",
                columns: new[] { "TenantId", "AcademicYearId" });

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_TenantId_SchoolClassId",
                table: "enrollments",
                columns: new[] { "TenantId", "SchoolClassId" });

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_TenantId_StudentId_SchoolClassId",
                table: "enrollments",
                columns: new[] { "TenantId", "StudentId", "SchoolClassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parentStudentRelationships_ParentId",
                table: "parentStudentRelationships",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_parentStudentRelationships_StudentId",
                table: "parentStudentRelationships",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_parentStudentRelationships_TenantId",
                table: "parentStudentRelationships",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_parentStudentRelationships_TenantId_ParentId_StudentId",
                table: "parentStudentRelationships",
                columns: new[] { "TenantId", "ParentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parentStudentRelationships_TenantId_StudentId",
                table: "parentStudentRelationships",
                columns: new[] { "TenantId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_schoolClasses_TenantId",
                table: "schoolClasses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_schoolClasses_TenantId_AcademicYearId_Code",
                table: "schoolClasses",
                columns: new[] { "TenantId", "AcademicYearId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_schoolClasses_TenantId_GradeId",
                table: "schoolClasses",
                columns: new[] { "TenantId", "GradeId" });

            migrationBuilder.CreateIndex(
                name: "IX_teacherClassAssignments_TeacherId",
                table: "teacherClassAssignments",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_teacherClassAssignments_TenantId",
                table: "teacherClassAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_teacherClassAssignments_TenantId_SchoolClassId",
                table: "teacherClassAssignments",
                columns: new[] { "TenantId", "SchoolClassId" });

            migrationBuilder.CreateIndex(
                name: "IX_teacherClassAssignments_TenantId_TeacherId_SchoolClassId",
                table: "teacherClassAssignments",
                columns: new[] { "TenantId", "TeacherId", "SchoolClassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teacherSubjectAssignments_TeacherId",
                table: "teacherSubjectAssignments",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_teacherSubjectAssignments_TenantId",
                table: "teacherSubjectAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_teacherSubjectAssignments_TenantId_SubjectId",
                table: "teacherSubjectAssignments",
                columns: new[] { "TenantId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_teacherSubjectAssignments_TenantId_TeacherId_SubjectId",
                table: "teacherSubjectAssignments",
                columns: new[] { "TenantId", "TeacherId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_terms_TenantId",
                table: "terms",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_terms_TenantId_AcademicYearId_Code",
                table: "terms",
                columns: new[] { "TenantId", "AcademicYearId", "Code" },
                unique: true);

            // ----------------------------------------------------------------------
            // Same-tenant integrity for USER references (Parent/Student/Teacher).
            // ApplicationUser.TenantId is legitimately nullable (platform SystemAdmin),
            // so it cannot take part in an alternate key / composite FK. Instead a
            // reusable trigger asserts that any referenced user belongs to the same
            // tenant as the owning row. This makes cross-tenant user relationships
            // database-rejected, not merely application-checked.
            // ----------------------------------------------------------------------
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION derasax_assert_user_tenant() RETURNS trigger AS $$
DECLARE
    uid text;
    utenant text;
BEGIN
    uid := row_to_json(NEW) ->> TG_ARGV[0];
    IF uid IS NULL THEN
        RETURN NEW;
    END IF;
    SELECT ""TenantId"" INTO utenant FROM ""AspNetUsers"" WHERE ""Id"" = uid;
    IF utenant IS DISTINCT FROM NEW.""TenantId"" THEN
        RAISE EXCEPTION 'cross-tenant user reference in %.%: user % belongs to tenant %, row tenant is %',
            TG_TABLE_NAME, TG_ARGV[0], uid, utenant, NEW.""TenantId"";
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_enrollment_student_tenant
    BEFORE INSERT OR UPDATE ON ""enrollments""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');

CREATE TRIGGER trg_psr_parent_tenant
    BEFORE INSERT OR UPDATE ON ""parentStudentRelationships""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('ParentId');

CREATE TRIGGER trg_psr_student_tenant
    BEFORE INSERT OR UPDATE ON ""parentStudentRelationships""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');

CREATE TRIGGER trg_tsa_teacher_tenant
    BEFORE INSERT OR UPDATE ON ""teacherSubjectAssignments""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('TeacherId');

CREATE TRIGGER trg_tca_teacher_tenant
    BEFORE INSERT OR UPDATE ON ""teacherClassAssignments""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('TeacherId');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_tca_teacher_tenant ON ""teacherClassAssignments"";
DROP TRIGGER IF EXISTS trg_tsa_teacher_tenant ON ""teacherSubjectAssignments"";
DROP TRIGGER IF EXISTS trg_psr_student_tenant ON ""parentStudentRelationships"";
DROP TRIGGER IF EXISTS trg_psr_parent_tenant ON ""parentStudentRelationships"";
DROP TRIGGER IF EXISTS trg_enrollment_student_tenant ON ""enrollments"";
DROP FUNCTION IF EXISTS derasax_assert_user_tenant();
");

            migrationBuilder.DropTable(
                name: "enrollments");

            migrationBuilder.DropTable(
                name: "parentStudentRelationships");

            migrationBuilder.DropTable(
                name: "teacherClassAssignments");

            migrationBuilder.DropTable(
                name: "teacherSubjectAssignments");

            migrationBuilder.DropTable(
                name: "terms");

            migrationBuilder.DropTable(
                name: "schoolClasses");

            migrationBuilder.DropTable(
                name: "academicYears");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_subjects_TenantId_Id",
                table: "subjects");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_grades_TenantId_Id",
                table: "grades");

            migrationBuilder.DropIndex(
                name: "IX_grades_TenantId_Name",
                table: "grades");
        }
    }
}
