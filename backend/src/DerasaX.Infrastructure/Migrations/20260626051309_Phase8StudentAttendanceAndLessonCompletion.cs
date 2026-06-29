using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase8StudentAttendanceAndLessonCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "studentAttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SchoolClassId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    SessionKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AttendanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
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
                    table.PrimaryKey("PK_studentAttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_studentAttendanceRecords_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_studentAttendanceRecords_schoolClasses_TenantId_SchoolClass~",
                        columns: x => new { x.TenantId, x.SchoolClassId },
                        principalTable: "schoolClasses",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_studentAttendanceRecords_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_studentAttendanceRecords_StudentId",
                table: "studentAttendanceRecords",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_studentAttendanceRecords_TenantId",
                table: "studentAttendanceRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_studentAttendanceRecords_TenantId_SchoolClassId_AttendanceD~",
                table: "studentAttendanceRecords",
                columns: new[] { "TenantId", "SchoolClassId", "AttendanceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_studentAttendanceRecords_TenantId_StudentId_AttendanceDate",
                table: "studentAttendanceRecords",
                columns: new[] { "TenantId", "StudentId", "AttendanceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_studentAttendanceRecords_TenantId_StudentId_AttendanceDate_~",
                table: "studentAttendanceRecords",
                columns: new[] { "TenantId", "StudentId", "AttendanceDate", "SessionKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "studentAttendanceRecords");
        }
    }
}
