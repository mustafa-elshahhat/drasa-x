using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase4TenantSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subscriptionPlanDefinitions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Tier = table.Column<string>(type: "text", nullable: false),
                    BillingPeriod = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    MaxStudents = table.Column<int>(type: "integer", nullable: true),
                    MaxTeachers = table.Column<int>(type: "integer", nullable: true),
                    MaxStorageMb = table.Column<int>(type: "integer", nullable: true),
                    MaxAiGenerationsPerMonth = table.Column<int>(type: "integer", nullable: true),
                    TrialDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptionPlanDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenantUsageCounters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SnapshotAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StudentsCount = table.Column<int>(type: "integer", nullable: false),
                    TeachersCount = table.Column<int>(type: "integer", nullable: false),
                    StorageUsedMb = table.Column<int>(type: "integer", nullable: false),
                    AiGenerationsUsed = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_tenantUsageCounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenantUsageCounters_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenantSubscriptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PlanDefinitionId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsTrial = table.Column<bool>(type: "boolean", nullable: false),
                    TrialEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AutoRenew = table.Column<bool>(type: "boolean", nullable: false),
                    SuspendedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReactivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_tenantSubscriptions", x => x.Id);
                    table.UniqueConstraint("AK_tenantSubscriptions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_tenantSubscriptions_subscriptionPlanDefinitions_PlanDefinit~",
                        column: x => x.PlanDefinitionId,
                        principalTable: "subscriptionPlanDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenantSubscriptions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscriptionRenewals",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantSubscriptionId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NewExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_subscriptionRenewals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscriptionRenewals_tenantSubscriptions_TenantId_TenantSub~",
                        columns: x => new { x.TenantId, x.TenantSubscriptionId },
                        principalTable: "tenantSubscriptions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subscriptionRenewals_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subscriptionPlanDefinitions_Code",
                table: "subscriptionPlanDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptionRenewals_TenantId",
                table: "subscriptionRenewals",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptionRenewals_TenantId_TenantSubscriptionId",
                table: "subscriptionRenewals",
                columns: new[] { "TenantId", "TenantSubscriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_tenantSubscriptions_PlanDefinitionId",
                table: "tenantSubscriptions",
                column: "PlanDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_tenantSubscriptions_TenantId",
                table: "tenantSubscriptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tenantSubscriptions_TenantId_ExpiresAt",
                table: "tenantSubscriptions",
                columns: new[] { "TenantId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tenantSubscriptions_TenantId_Status",
                table: "tenantSubscriptions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_tenantUsageCounters_TenantId",
                table: "tenantUsageCounters",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tenantUsageCounters_TenantId_PeriodStart",
                table: "tenantUsageCounters",
                columns: new[] { "TenantId", "PeriodStart" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subscriptionRenewals");

            migrationBuilder.DropTable(
                name: "tenantUsageCounters");

            migrationBuilder.DropTable(
                name: "tenantSubscriptions");

            migrationBuilder.DropTable(
                name: "subscriptionPlanDefinitions");
        }
    }
}
