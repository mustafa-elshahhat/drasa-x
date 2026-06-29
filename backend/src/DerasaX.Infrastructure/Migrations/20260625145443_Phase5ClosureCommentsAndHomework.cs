using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase5ClosureCommentsAndHomework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assignmentSubmissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AssignmentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Content = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    AttachmentFileId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    Feedback = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    GradedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GradedByTeacherId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
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
                    table.PrimaryKey("PK_assignmentSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assignmentSubmissions_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assignmentSubmissions_assignments_TenantId_AssignmentId",
                        columns: x => new { x.TenantId, x.AssignmentId },
                        principalTable: "assignments",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assignmentSubmissions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lessonMaterialComments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    MaterialId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Body = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
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
                    table.PrimaryKey("PK_lessonMaterialComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lessonMaterialComments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lessonMaterialComments_lessonMaterials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "lessonMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lessonMaterialComments_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assignmentSubmissions_StudentId",
                table: "assignmentSubmissions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_assignmentSubmissions_TenantId",
                table: "assignmentSubmissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_assignmentSubmissions_TenantId_AssignmentId_StudentId",
                table: "assignmentSubmissions",
                columns: new[] { "TenantId", "AssignmentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lessonMaterialComments_MaterialId",
                table: "lessonMaterialComments",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_lessonMaterialComments_TenantId",
                table: "lessonMaterialComments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_lessonMaterialComments_TenantId_MaterialId_CreatedAt",
                table: "lessonMaterialComments",
                columns: new[] { "TenantId", "MaterialId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_lessonMaterialComments_UserId",
                table: "lessonMaterialComments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignmentSubmissions");

            migrationBuilder.DropTable(
                name: "lessonMaterialComments");
        }
    }
}
