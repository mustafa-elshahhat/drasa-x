using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase14ClosureGaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EligibleGradeId",
                table: "communities",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "competitionSubmissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CompetitionId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Content = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_competitionSubmissions", x => x.Id);
                    table.UniqueConstraint("AK_competitionSubmissions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_competitionSubmissions_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_competitionSubmissions_competitions_TenantId_CompetitionId",
                        columns: x => new { x.TenantId, x.CompetitionId },
                        principalTable: "competitions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_competitionSubmissions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_communities_EligibleGradeId",
                table: "communities",
                column: "EligibleGradeId");

            migrationBuilder.CreateIndex(
                name: "IX_communities_TenantId_EligibleGradeId",
                table: "communities",
                columns: new[] { "TenantId", "EligibleGradeId" });

            migrationBuilder.CreateIndex(
                name: "IX_competitionSubmissions_StudentId",
                table: "competitionSubmissions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_competitionSubmissions_TenantId",
                table: "competitionSubmissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_competitionSubmissions_TenantId_CompetitionId_StudentId",
                table: "competitionSubmissions",
                columns: new[] { "TenantId", "CompetitionId", "StudentId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_communities_grades_EligibleGradeId",
                table: "communities",
                column: "EligibleGradeId",
                principalTable: "grades",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Phase 14 (closure) — defence-in-depth tenant integrity: the StudentId on a competition
            // submission MUST belong to the same tenant as the row (mirrors the Phase 4 engagement and
            // Phase 14 ledger triggers). Reuses the existing derasax_assert_user_tenant() helper.
            migrationBuilder.Sql(@"
CREATE TRIGGER trg_competition_submission_student_tenant
    BEFORE INSERT OR UPDATE ON ""competitionSubmissions""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trg_competition_submission_student_tenant ON ""competitionSubmissions"";");

            migrationBuilder.DropForeignKey(
                name: "FK_communities_grades_EligibleGradeId",
                table: "communities");

            migrationBuilder.DropTable(
                name: "competitionSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_communities_EligibleGradeId",
                table: "communities");

            migrationBuilder.DropIndex(
                name: "IX_communities_TenantId_EligibleGradeId",
                table: "communities");

            migrationBuilder.DropColumn(
                name: "EligibleGradeId",
                table: "communities");
        }
    }
}
