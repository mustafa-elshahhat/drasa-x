using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase6StudentLearningProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "studentLearningProfiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AgeYears = table.Column<int>(type: "integer", nullable: false),
                    SchoolType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    InternetAccess = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TravelTime = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExtraActivities = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StudyMethod = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FeatureSchemaVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_studentLearningProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_studentLearningProfiles_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_studentLearningProfiles_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_studentLearningProfiles_StudentId",
                table: "studentLearningProfiles",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_studentLearningProfiles_TenantId",
                table: "studentLearningProfiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_studentLearningProfiles_TenantId_StudentId",
                table: "studentLearningProfiles",
                columns: new[] { "TenantId", "StudentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "studentLearningProfiles");
        }
    }
}
