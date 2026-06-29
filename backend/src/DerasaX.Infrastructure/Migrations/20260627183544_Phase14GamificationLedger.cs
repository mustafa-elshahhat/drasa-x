using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase14GamificationLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gamificationRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Trigger = table.Column<string>(type: "text", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    BadgeId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_gamificationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gamificationRules_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "studentPointTransactions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SourceType = table.Column<string>(type: "text", nullable: false),
                    SourceId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GamificationRuleId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    AwardedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_studentPointTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_studentPointTransactions_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_studentPointTransactions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gamificationRules_TenantId",
                table: "gamificationRules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_gamificationRules_TenantId_Code",
                table: "gamificationRules",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_studentPointTransactions_StudentId",
                table: "studentPointTransactions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_studentPointTransactions_TenantId",
                table: "studentPointTransactions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_studentPointTransactions_TenantId_IdempotencyKey",
                table: "studentPointTransactions",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_studentPointTransactions_TenantId_StudentId_AwardedAt",
                table: "studentPointTransactions",
                columns: new[] { "TenantId", "StudentId", "AwardedAt" });

            // Phase 14 — defence-in-depth tenant integrity: the StudentId on a ledger row MUST belong
            // to the same tenant as the row (mirrors the Phase 4 engagement triggers). Reuses the
            // existing derasax_assert_user_tenant() PL/pgSQL helper created in Phase 4.
            migrationBuilder.Sql(@"
CREATE TRIGGER trg_student_point_tx_student_tenant
    BEFORE INSERT OR UPDATE ON ""studentPointTransactions""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trg_student_point_tx_student_tenant ON ""studentPointTransactions"";");

            migrationBuilder.DropTable(
                name: "gamificationRules");

            migrationBuilder.DropTable(
                name: "studentPointTransactions");
        }
    }
}
