using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase16WorkflowFileLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssetRetentionUntil",
                table: "studentFaceEnrollments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConsentObtained",
                table: "studentFaceEnrollments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ConsentReference",
                table: "studentFaceEnrollments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileRecordId",
                table: "studentFaceEnrollments",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileRecordId",
                table: "parentRequests",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileRecordId",
                table: "parentRequestResponses",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileRecordId",
                table: "messageAttachments",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileRecordId",
                table: "lessonMaterials",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssetRetentionUntil",
                table: "studentFaceEnrollments");

            migrationBuilder.DropColumn(
                name: "ConsentObtained",
                table: "studentFaceEnrollments");

            migrationBuilder.DropColumn(
                name: "ConsentReference",
                table: "studentFaceEnrollments");

            migrationBuilder.DropColumn(
                name: "FileRecordId",
                table: "studentFaceEnrollments");

            migrationBuilder.DropColumn(
                name: "FileRecordId",
                table: "parentRequests");

            migrationBuilder.DropColumn(
                name: "FileRecordId",
                table: "parentRequestResponses");

            migrationBuilder.DropColumn(
                name: "FileRecordId",
                table: "messageAttachments");

            migrationBuilder.DropColumn(
                name: "FileRecordId",
                table: "lessonMaterials");
        }
    }
}
