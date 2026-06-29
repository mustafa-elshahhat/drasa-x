using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase4ProgressInsights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_studentInsights_Student_StudentId",
                table: "studentInsights");

            migrationBuilder.DropForeignKey(
                name: "FK_studentLessonProgresses_Student_StudentId",
                table: "studentLessonProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_studentLessonProgresses_lessons_LessonId",
                table: "studentLessonProgresses");

            migrationBuilder.DropIndex(
                name: "IX_studentLessonProgresses_LessonId",
                table: "studentLessonProgresses");

            migrationBuilder.AlterColumn<string>(
                name: "StudentId",
                table: "studentLessonProgresses",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LessonId",
                table: "studentLessonProgresses",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<decimal>(
                name: "CompletionPercentage",
                table: "studentLessonProgresses",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "studentLessonProgresses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "studentLessonProgresses",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessedAt",
                table: "studentLessonProgresses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "studentLessonProgresses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeSpentSeconds",
                table: "studentLessonProgresses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "studentLessonProgresses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "studentLessonProgresses",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WatchedMaterialsCount",
                table: "studentLessonProgresses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "studentLessonProgresses",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AlterColumn<string>(
                name: "StudentId",
                table: "studentInsights",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "ConfidenceScore",
                table: "studentInsights",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "studentInsights",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "studentInsights",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceJson",
                table: "studentInsights",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendationText",
                table: "studentInsights",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "studentInsights",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "studentInsights",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "studentInsights",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "studentInsights",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_studentInsights_TenantId_Id",
                table: "studentInsights",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_lessons_TenantId_Id",
                table: "lessons",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "painPoints",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    StudentInsightId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_painPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_painPoints_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_painPoints_studentInsights_TenantId_StudentInsightId",
                        columns: x => new { x.TenantId, x.StudentInsightId },
                        principalTable: "studentInsights",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_painPoints_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "predictionRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    PredictedScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Level = table.Column<string>(type: "text", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ModelVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InputSnapshotJson = table.Column<string>(type: "text", nullable: true),
                    PredictedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_predictionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_predictionRecords_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_predictionRecords_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "studentMetricHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    MetricType = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    MeasuredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceEntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SourceEntityId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_studentMetricHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_studentMetricHistories_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_studentMetricHistories_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "studentRecommendations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    StudentInsightId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_studentRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_studentRecommendations_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_studentRecommendations_studentInsights_TenantId_StudentInsi~",
                        columns: x => new { x.TenantId, x.StudentInsightId },
                        principalTable: "studentInsights",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_studentRecommendations_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subjectProgresses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SubjectId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CompletionPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    AverageScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    LessonsCompleted = table.Column<int>(type: "integer", nullable: false),
                    TotalLessons = table.Column<int>(type: "integer", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_subjectProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subjectProgresses_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subjectProgresses_subjects_TenantId_SubjectId",
                        columns: x => new { x.TenantId, x.SubjectId },
                        principalTable: "subjects",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subjectProgresses_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_studentLessonProgresses_TenantId_LessonId",
                table: "studentLessonProgresses",
                columns: new[] { "TenantId", "LessonId" });

            migrationBuilder.CreateIndex(
                name: "IX_studentLessonProgresses_TenantId_StudentId_LessonId",
                table: "studentLessonProgresses",
                columns: new[] { "TenantId", "StudentId", "LessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_studentInsights_TenantId_StudentId_PeriodStart_PeriodEnd",
                table: "studentInsights",
                columns: new[] { "TenantId", "StudentId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_painPoints_StudentId",
                table: "painPoints",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_painPoints_TenantId",
                table: "painPoints",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_painPoints_TenantId_StudentId_IsResolved",
                table: "painPoints",
                columns: new[] { "TenantId", "StudentId", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_painPoints_TenantId_StudentInsightId",
                table: "painPoints",
                columns: new[] { "TenantId", "StudentInsightId" });

            migrationBuilder.CreateIndex(
                name: "IX_predictionRecords_StudentId",
                table: "predictionRecords",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_predictionRecords_TenantId",
                table: "predictionRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_predictionRecords_TenantId_StudentId_Kind_PredictedAt",
                table: "predictionRecords",
                columns: new[] { "TenantId", "StudentId", "Kind", "PredictedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_studentMetricHistories_StudentId",
                table: "studentMetricHistories",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_studentMetricHistories_TenantId",
                table: "studentMetricHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_studentMetricHistories_TenantId_StudentId_MetricType_Measur~",
                table: "studentMetricHistories",
                columns: new[] { "TenantId", "StudentId", "MetricType", "MeasuredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_studentRecommendations_StudentId",
                table: "studentRecommendations",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_studentRecommendations_TenantId",
                table: "studentRecommendations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_studentRecommendations_TenantId_StudentId_Status",
                table: "studentRecommendations",
                columns: new[] { "TenantId", "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_studentRecommendations_TenantId_StudentInsightId",
                table: "studentRecommendations",
                columns: new[] { "TenantId", "StudentInsightId" });

            migrationBuilder.CreateIndex(
                name: "IX_subjectProgresses_StudentId",
                table: "subjectProgresses",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_subjectProgresses_TenantId",
                table: "subjectProgresses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_subjectProgresses_TenantId_StudentId_SubjectId",
                table: "subjectProgresses",
                columns: new[] { "TenantId", "StudentId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subjectProgresses_TenantId_SubjectId",
                table: "subjectProgresses",
                columns: new[] { "TenantId", "SubjectId" });

            migrationBuilder.AddForeignKey(
                name: "FK_studentInsights_Student_StudentId",
                table: "studentInsights",
                column: "StudentId",
                principalTable: "Student",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_studentLessonProgresses_Student_StudentId",
                table: "studentLessonProgresses",
                column: "StudentId",
                principalTable: "Student",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_studentLessonProgresses_lessons_TenantId_LessonId",
                table: "studentLessonProgresses",
                columns: new[] { "TenantId", "LessonId" },
                principalTable: "lessons",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
CREATE TRIGGER trg_slp_student_tenant
    BEFORE INSERT OR UPDATE ON ""studentLessonProgresses""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');

CREATE TRIGGER trg_student_insight_student_tenant
    BEFORE INSERT OR UPDATE ON ""studentInsights""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');

CREATE TRIGGER trg_subject_progress_student_tenant
    BEFORE INSERT OR UPDATE ON ""subjectProgresses""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');

CREATE TRIGGER trg_metric_history_student_tenant
    BEFORE INSERT OR UPDATE ON ""studentMetricHistories""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');

CREATE TRIGGER trg_pain_point_student_tenant
    BEFORE INSERT OR UPDATE ON ""painPoints""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');

CREATE TRIGGER trg_recommendation_student_tenant
    BEFORE INSERT OR UPDATE ON ""studentRecommendations""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');

CREATE TRIGGER trg_prediction_student_tenant
    BEFORE INSERT OR UPDATE ON ""predictionRecords""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_prediction_student_tenant ON ""predictionRecords"";
DROP TRIGGER IF EXISTS trg_recommendation_student_tenant ON ""studentRecommendations"";
DROP TRIGGER IF EXISTS trg_pain_point_student_tenant ON ""painPoints"";
DROP TRIGGER IF EXISTS trg_metric_history_student_tenant ON ""studentMetricHistories"";
DROP TRIGGER IF EXISTS trg_subject_progress_student_tenant ON ""subjectProgresses"";
DROP TRIGGER IF EXISTS trg_student_insight_student_tenant ON ""studentInsights"";
DROP TRIGGER IF EXISTS trg_slp_student_tenant ON ""studentLessonProgresses"";
");

            migrationBuilder.DropForeignKey(
                name: "FK_studentInsights_Student_StudentId",
                table: "studentInsights");

            migrationBuilder.DropForeignKey(
                name: "FK_studentLessonProgresses_Student_StudentId",
                table: "studentLessonProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_studentLessonProgresses_lessons_TenantId_LessonId",
                table: "studentLessonProgresses");

            migrationBuilder.DropTable(
                name: "painPoints");

            migrationBuilder.DropTable(
                name: "predictionRecords");

            migrationBuilder.DropTable(
                name: "studentMetricHistories");

            migrationBuilder.DropTable(
                name: "studentRecommendations");

            migrationBuilder.DropTable(
                name: "subjectProgresses");

            migrationBuilder.DropIndex(
                name: "IX_studentLessonProgresses_TenantId_LessonId",
                table: "studentLessonProgresses");

            migrationBuilder.DropIndex(
                name: "IX_studentLessonProgresses_TenantId_StudentId_LessonId",
                table: "studentLessonProgresses");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_studentInsights_TenantId_Id",
                table: "studentInsights");

            migrationBuilder.DropIndex(
                name: "IX_studentInsights_TenantId_StudentId_PeriodStart_PeriodEnd",
                table: "studentInsights");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_lessons_TenantId_Id",
                table: "lessons");

            migrationBuilder.DropColumn(
                name: "CompletionPercentage",
                table: "studentLessonProgresses");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "studentLessonProgresses");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "studentLessonProgresses");

            migrationBuilder.DropColumn(
                name: "LastAccessedAt",
                table: "studentLessonProgresses");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "studentLessonProgresses");

            migrationBuilder.DropColumn(
                name: "TimeSpentSeconds",
                table: "studentLessonProgresses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "studentLessonProgresses");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "studentLessonProgresses");

            migrationBuilder.DropColumn(
                name: "WatchedMaterialsCount",
                table: "studentLessonProgresses");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "studentLessonProgresses");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "studentInsights");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "studentInsights");

            migrationBuilder.DropColumn(
                name: "EvidenceJson",
                table: "studentInsights");

            migrationBuilder.DropColumn(
                name: "RecommendationText",
                table: "studentInsights");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "studentInsights");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "studentInsights");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "studentInsights");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "studentInsights");

            migrationBuilder.AlterColumn<string>(
                name: "StudentId",
                table: "studentLessonProgresses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "LessonId",
                table: "studentLessonProgresses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "StudentId",
                table: "studentInsights",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<double>(
                name: "ConfidenceScore",
                table: "studentInsights",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,4)",
                oldPrecision: 5,
                oldScale: 4);

            migrationBuilder.CreateIndex(
                name: "IX_studentLessonProgresses_LessonId",
                table: "studentLessonProgresses",
                column: "LessonId");

            migrationBuilder.AddForeignKey(
                name: "FK_studentInsights_Student_StudentId",
                table: "studentInsights",
                column: "StudentId",
                principalTable: "Student",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_studentLessonProgresses_Student_StudentId",
                table: "studentLessonProgresses",
                column: "StudentId",
                principalTable: "Student",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_studentLessonProgresses_lessons_LessonId",
                table: "studentLessonProgresses",
                column: "LessonId",
                principalTable: "lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
