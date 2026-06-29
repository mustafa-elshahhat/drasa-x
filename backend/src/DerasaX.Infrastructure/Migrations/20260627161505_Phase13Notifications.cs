using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase13Notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActorUserId",
                table: "notifications",
                type: "text",
                nullable: true);

            // Enums are stored as their member name (ApplyEnumToStringConversions). Backfill existing
            // rows with valid, honest values: already-persisted notifications were delivered in-app, and
            // e-mail is not configured in this environment.
            migrationBuilder.AddColumn<string>(
                name: "DeliveryStatus",
                table: "notifications",
                type: "text",
                nullable: false,
                defaultValue: "Delivered");

            migrationBuilder.AddColumn<string>(
                name: "EmailStatus",
                table: "notifications",
                type: "text",
                nullable: false,
                defaultValue: "NotConfigured");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "notificationPreferences",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    InAppEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notificationPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notificationPreferences_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notificationPreferences_TenantId",
                table: "notificationPreferences",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_notificationPreferences_TenantId_UserId_Category",
                table: "notificationPreferences",
                columns: new[] { "TenantId", "UserId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notificationPreferences_UserId",
                table: "notificationPreferences",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notificationPreferences");

            migrationBuilder.DropColumn(
                name: "ActorUserId",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "EmailStatus",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "notifications");
        }
    }
}
