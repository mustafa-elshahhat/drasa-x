using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase6PainPointAnalysisReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiProvider",
                table: "painPoints",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "painPoints",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Escalation",
                table: "painPoints",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "painPoints",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptVersion",
                table: "painPoints",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recommendation",
                table: "painPoints",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewStatus",
                table: "painPoints",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "painPoints",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByTeacherId",
                table: "painPoints",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiProvider",
                table: "painPoints");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "painPoints");

            migrationBuilder.DropColumn(
                name: "Escalation",
                table: "painPoints");

            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "painPoints");

            migrationBuilder.DropColumn(
                name: "PromptVersion",
                table: "painPoints");

            migrationBuilder.DropColumn(
                name: "Recommendation",
                table: "painPoints");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "painPoints");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "painPoints");

            migrationBuilder.DropColumn(
                name: "ReviewedByTeacherId",
                table: "painPoints");
        }
    }
}
