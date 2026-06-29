using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase16FileStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConsentObtained",
                table: "fileRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ConsentReference",
                table: "fileRecords",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "fileRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "fileRecords",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "fileRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RelatedEntityId",
                table: "fileRecords",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedEntityType",
                table: "fileRecords",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetentionUntil",
                table: "fileRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SafeStoredFileName",
                table: "fileRecords",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScanStatus",
                table: "fileRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StorageBucket",
                table: "fileRecords",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageProvider",
                table: "fileRecords",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "fileRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_fileRecords_TenantId_Purpose",
                table: "fileRecords",
                columns: new[] { "TenantId", "Purpose" });

            migrationBuilder.CreateIndex(
                name: "IX_fileRecords_TenantId_RelatedEntityType_RelatedEntityId",
                table: "fileRecords",
                columns: new[] { "TenantId", "RelatedEntityType", "RelatedEntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_fileRecords_TenantId_Purpose",
                table: "fileRecords");

            migrationBuilder.DropIndex(
                name: "IX_fileRecords_TenantId_RelatedEntityType_RelatedEntityId",
                table: "fileRecords");

            migrationBuilder.DropColumn(
                name: "ConsentObtained",
                table: "fileRecords");

            migrationBuilder.DropColumn(
                name: "ConsentReference",
                table: "fileRecords");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "fileRecords");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "fileRecords");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "fileRecords");

            migrationBuilder.DropColumn(
                name: "RelatedEntityId",
                table: "fileRecords");

            migrationBuilder.DropColumn(
                name: "RelatedEntityType",
                table: "fileRecords");

            migrationBuilder.DropColumn(
                name: "RetentionUntil",
                table: "fileRecords");

            migrationBuilder.DropColumn(
                name: "SafeStoredFileName",
                table: "fileRecords");

            migrationBuilder.DropColumn(
                name: "ScanStatus",
                table: "fileRecords");

            migrationBuilder.DropColumn(
                name: "StorageBucket",
                table: "fileRecords");

            migrationBuilder.DropColumn(
                name: "StorageProvider",
                table: "fileRecords");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "fileRecords");
        }
    }
}
