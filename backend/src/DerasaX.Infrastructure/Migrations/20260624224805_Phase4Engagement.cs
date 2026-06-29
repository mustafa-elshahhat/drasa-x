using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase4Engagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_posts_AspNetUsers_UserId",
                table: "posts");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "posts",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "CommunityId",
                table: "posts",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_posts_TenantId_Id",
                table: "posts",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "badges",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_badges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "communities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Visibility = table.Column<string>(type: "text", nullable: false),
                    SchoolClassId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
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
                    table.PrimaryKey("PK_communities", x => x.Id);
                    table.UniqueConstraint("AK_communities_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_communities_schoolClasses_TenantId_SchoolClassId",
                        columns: x => new { x.TenantId, x.SchoolClassId },
                        principalTable: "schoolClasses",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_communities_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "competitions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_competitions", x => x.Id);
                    table.UniqueConstraint("AK_competitions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_competitions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "officeHourSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TeacherId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_officeHourSessions", x => x.Id);
                    table.UniqueConstraint("AK_officeHourSessions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_officeHourSessions_Teacher_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teacher",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_officeHourSessions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "postComments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PostId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Body = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_postComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_postComments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_postComments_posts_TenantId_PostId",
                        columns: x => new { x.TenantId, x.PostId },
                        principalTable: "posts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_postComments_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "postReports",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PostId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ReportedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_postReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_postReports_AspNetUsers_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_postReports_posts_TenantId_PostId",
                        columns: x => new { x.TenantId, x.PostId },
                        principalTable: "posts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_postReports_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "studentStreaks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CurrentCount = table.Column<int>(type: "integer", nullable: false),
                    LongestCount = table.Column<int>(type: "integer", nullable: false),
                    LastActivityDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_studentStreaks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_studentStreaks_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_studentStreaks_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "studentBadges",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    BadgeId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AwardedReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
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
                    table.PrimaryKey("PK_studentBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_studentBadges_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_studentBadges_badges_BadgeId",
                        column: x => x.BadgeId,
                        principalTable: "badges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_studentBadges_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "communityMemberships",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CommunityId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_communityMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_communityMemberships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_communityMemberships_communities_TenantId_CommunityId",
                        columns: x => new { x.TenantId, x.CommunityId },
                        principalTable: "communities",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_communityMemberships_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "competitionEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CompetitionId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    EnteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_competitionEntries", x => x.Id);
                    table.UniqueConstraint("AK_competitionEntries_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_competitionEntries_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_competitionEntries_competitions_TenantId_CompetitionId",
                        columns: x => new { x.TenantId, x.CompetitionId },
                        principalTable: "competitions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_competitionEntries_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leaderboardEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CompetitionId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_leaderboardEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leaderboardEntries_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_leaderboardEntries_competitions_TenantId_CompetitionId",
                        columns: x => new { x.TenantId, x.CompetitionId },
                        principalTable: "competitions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leaderboardEntries_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "officeHourBookings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    OfficeHourSessionId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    BookedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_officeHourBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_officeHourBookings_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_officeHourBookings_officeHourSessions_TenantId_OfficeHourSe~",
                        columns: x => new { x.TenantId, x.OfficeHourSessionId },
                        principalTable: "officeHourSessions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_officeHourBookings_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "competitionScores",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CompetitionEntryId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    ScoredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_competitionScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_competitionScores_competitionEntries_TenantId_CompetitionEn~",
                        columns: x => new { x.TenantId, x.CompetitionEntryId },
                        principalTable: "competitionEntries",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_competitionScores_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_posts_TenantId_CommunityId_CreatedAt",
                table: "posts",
                columns: new[] { "TenantId", "CommunityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_badges_Code",
                table: "badges",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communities_TenantId",
                table: "communities",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_communities_TenantId_Name",
                table: "communities",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communities_TenantId_SchoolClassId",
                table: "communities",
                columns: new[] { "TenantId", "SchoolClassId" });

            migrationBuilder.CreateIndex(
                name: "IX_communityMemberships_TenantId",
                table: "communityMemberships",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_communityMemberships_TenantId_CommunityId_UserId",
                table: "communityMemberships",
                columns: new[] { "TenantId", "CommunityId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communityMemberships_UserId",
                table: "communityMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_competitionEntries_StudentId",
                table: "competitionEntries",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_competitionEntries_TenantId",
                table: "competitionEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_competitionEntries_TenantId_CompetitionId_StudentId",
                table: "competitionEntries",
                columns: new[] { "TenantId", "CompetitionId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competitions_TenantId",
                table: "competitions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_competitions_TenantId_Status_StartsAt",
                table: "competitions",
                columns: new[] { "TenantId", "Status", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_competitionScores_TenantId",
                table: "competitionScores",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_competitionScores_TenantId_CompetitionEntryId",
                table: "competitionScores",
                columns: new[] { "TenantId", "CompetitionEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_leaderboardEntries_StudentId",
                table: "leaderboardEntries",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_leaderboardEntries_TenantId",
                table: "leaderboardEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_leaderboardEntries_TenantId_CompetitionId_Rank",
                table: "leaderboardEntries",
                columns: new[] { "TenantId", "CompetitionId", "Rank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_officeHourBookings_StudentId",
                table: "officeHourBookings",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_officeHourBookings_TenantId",
                table: "officeHourBookings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_officeHourBookings_TenantId_OfficeHourSessionId_StudentId",
                table: "officeHourBookings",
                columns: new[] { "TenantId", "OfficeHourSessionId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_officeHourSessions_TeacherId",
                table: "officeHourSessions",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_officeHourSessions_TenantId",
                table: "officeHourSessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_officeHourSessions_TenantId_TeacherId_StartsAt",
                table: "officeHourSessions",
                columns: new[] { "TenantId", "TeacherId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_postComments_TenantId",
                table: "postComments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_postComments_TenantId_PostId",
                table: "postComments",
                columns: new[] { "TenantId", "PostId" });

            migrationBuilder.CreateIndex(
                name: "IX_postComments_UserId",
                table: "postComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_postReports_ReportedByUserId",
                table: "postReports",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_postReports_TenantId",
                table: "postReports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_postReports_TenantId_PostId",
                table: "postReports",
                columns: new[] { "TenantId", "PostId" });

            migrationBuilder.CreateIndex(
                name: "IX_postReports_TenantId_Status",
                table: "postReports",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_studentBadges_BadgeId",
                table: "studentBadges",
                column: "BadgeId");

            migrationBuilder.CreateIndex(
                name: "IX_studentBadges_StudentId",
                table: "studentBadges",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_studentBadges_TenantId",
                table: "studentBadges",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_studentBadges_TenantId_StudentId_BadgeId",
                table: "studentBadges",
                columns: new[] { "TenantId", "StudentId", "BadgeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_studentStreaks_StudentId",
                table: "studentStreaks",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_studentStreaks_TenantId",
                table: "studentStreaks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_studentStreaks_TenantId_StudentId",
                table: "studentStreaks",
                columns: new[] { "TenantId", "StudentId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_posts_AspNetUsers_UserId",
                table: "posts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_posts_communities_TenantId_CommunityId",
                table: "posts",
                columns: new[] { "TenantId", "CommunityId" },
                principalTable: "communities",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
CREATE TRIGGER trg_post_user_tenant
    BEFORE INSERT OR UPDATE ON ""posts""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('UserId');

CREATE TRIGGER trg_community_membership_user_tenant
    BEFORE INSERT OR UPDATE ON ""communityMemberships""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('UserId');

CREATE TRIGGER trg_post_comment_user_tenant
    BEFORE INSERT OR UPDATE ON ""postComments""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('UserId');

CREATE TRIGGER trg_post_report_user_tenant
    BEFORE INSERT OR UPDATE ON ""postReports""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('ReportedByUserId');

CREATE TRIGGER trg_competition_entry_student_tenant
    BEFORE INSERT OR UPDATE ON ""competitionEntries""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');

CREATE TRIGGER trg_leaderboard_student_tenant
    BEFORE INSERT OR UPDATE ON ""leaderboardEntries""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');

CREATE TRIGGER trg_student_badge_student_tenant
    BEFORE INSERT OR UPDATE ON ""studentBadges""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');

CREATE TRIGGER trg_student_streak_student_tenant
    BEFORE INSERT OR UPDATE ON ""studentStreaks""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');

CREATE TRIGGER trg_office_hour_teacher_tenant
    BEFORE INSERT OR UPDATE ON ""officeHourSessions""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('TeacherId');

CREATE TRIGGER trg_office_hour_booking_student_tenant
    BEFORE INSERT OR UPDATE ON ""officeHourBookings""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_posts_AspNetUsers_UserId",
                table: "posts");

            migrationBuilder.DropForeignKey(
                name: "FK_posts_communities_TenantId_CommunityId",
                table: "posts");

            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_office_hour_booking_student_tenant ON ""officeHourBookings"";
DROP TRIGGER IF EXISTS trg_office_hour_teacher_tenant ON ""officeHourSessions"";
DROP TRIGGER IF EXISTS trg_student_streak_student_tenant ON ""studentStreaks"";
DROP TRIGGER IF EXISTS trg_student_badge_student_tenant ON ""studentBadges"";
DROP TRIGGER IF EXISTS trg_leaderboard_student_tenant ON ""leaderboardEntries"";
DROP TRIGGER IF EXISTS trg_competition_entry_student_tenant ON ""competitionEntries"";
DROP TRIGGER IF EXISTS trg_post_report_user_tenant ON ""postReports"";
DROP TRIGGER IF EXISTS trg_post_comment_user_tenant ON ""postComments"";
DROP TRIGGER IF EXISTS trg_community_membership_user_tenant ON ""communityMemberships"";
DROP TRIGGER IF EXISTS trg_post_user_tenant ON ""posts"";
");

            migrationBuilder.DropTable(
                name: "communityMemberships");

            migrationBuilder.DropTable(
                name: "competitionScores");

            migrationBuilder.DropTable(
                name: "leaderboardEntries");

            migrationBuilder.DropTable(
                name: "officeHourBookings");

            migrationBuilder.DropTable(
                name: "postComments");

            migrationBuilder.DropTable(
                name: "postReports");

            migrationBuilder.DropTable(
                name: "studentBadges");

            migrationBuilder.DropTable(
                name: "studentStreaks");

            migrationBuilder.DropTable(
                name: "communities");

            migrationBuilder.DropTable(
                name: "competitionEntries");

            migrationBuilder.DropTable(
                name: "officeHourSessions");

            migrationBuilder.DropTable(
                name: "badges");

            migrationBuilder.DropTable(
                name: "competitions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_posts_TenantId_Id",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_posts_TenantId_CommunityId_CreatedAt",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "CommunityId",
                table: "posts");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "posts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AddForeignKey(
                name: "FK_posts_AspNetUsers_UserId",
                table: "posts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
