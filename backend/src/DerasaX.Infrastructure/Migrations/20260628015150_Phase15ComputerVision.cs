using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase15ComputerVision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "classroomVisionSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TeacherId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SchoolClassId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    SubjectId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LessonId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SessionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FrameCount = table.Column<int>(type: "integer", nullable: false),
                    RecognitionThreshold = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    EngineKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Degraded = table.Column<bool>(type: "boolean", nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    StoreRawFrames = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
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
                    table.PrimaryKey("PK_classroomVisionSessions", x => x.Id);
                    table.UniqueConstraint("AK_classroomVisionSessions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_classroomVisionSessions_schoolClasses_TenantId_SchoolClassId",
                        columns: x => new { x.TenantId, x.SchoolClassId },
                        principalTable: "schoolClasses",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_classroomVisionSessions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "studentFaceEnrollments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ExternalLabelId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayLabel = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_studentFaceEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_studentFaceEnrollments_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_studentFaceEnrollments_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attendanceDetectionCandidates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SessionId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TrackId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExternalLabelId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MappedStudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    BestRecognitionConfidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    RecognitionStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DetectionCount = table.Column<int>(type: "integer", nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Degraded = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewStatus = table.Column<string>(type: "text", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ResolvedStudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ResolvedStatus = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    AttendanceRecordId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
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
                    table.PrimaryKey("PK_attendanceDetectionCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendanceDetectionCandidates_classroomVisionSessions_Tenan~",
                        columns: x => new { x.TenantId, x.SessionId },
                        principalTable: "classroomVisionSessions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendanceDetectionCandidates_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "classroomVisionFrameAnalyses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SessionId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    FrameIndex = table.Column<int>(type: "integer", nullable: false),
                    CaptureLabel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FacesDetected = table.Column<int>(type: "integer", nullable: false),
                    EngineKind = table.Column<string>(type: "text", nullable: false),
                    Degraded = table.Column<bool>(type: "boolean", nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    QualityFlags = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    AnalyzedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_classroomVisionFrameAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_classroomVisionFrameAnalyses_classroomVisionSessions_Tenant~",
                        columns: x => new { x.TenantId, x.SessionId },
                        principalTable: "classroomVisionSessions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_classroomVisionFrameAnalyses_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "classroomVisionSessionSummaries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SessionId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TotalFrames = table.Column<int>(type: "integer", nullable: false),
                    TotalFaceObservations = table.Column<int>(type: "integer", nullable: false),
                    DistinctTracks = table.Column<int>(type: "integer", nullable: false),
                    PendingCandidates = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedAttendance = table.Column<int>(type: "integer", nullable: false),
                    RejectedCandidates = table.Column<int>(type: "integer", nullable: false),
                    OverriddenCandidates = table.Column<int>(type: "integer", nullable: false),
                    EngagedObservations = table.Column<int>(type: "integer", nullable: false),
                    DisengagedObservations = table.Column<int>(type: "integer", nullable: false),
                    NotReadyObservations = table.Column<int>(type: "integer", nullable: false),
                    AverageEngagementConfidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Degraded = table.Column<bool>(type: "boolean", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_classroomVisionSessionSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_classroomVisionSessionSummaries_classroomVisionSessions_Ten~",
                        columns: x => new { x.TenantId, x.SessionId },
                        principalTable: "classroomVisionSessions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_classroomVisionSessionSummaries_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "studentEngagementObservations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SessionId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    FrameAnalysisId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    TrackId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExternalLabelId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Emotion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EmotionConfidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Engagement = table.Column<string>(type: "text", nullable: false),
                    EngagementConfidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    EngagementFrames = table.Column<int>(type: "integer", nullable: false),
                    EngagementReady = table.Column<bool>(type: "boolean", nullable: false),
                    EngineKind = table.Column<string>(type: "text", nullable: false),
                    Degraded = table.Column<bool>(type: "boolean", nullable: false),
                    ObservedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_studentEngagementObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_studentEngagementObservations_classroomVisionSessions_Tenan~",
                        columns: x => new { x.TenantId, x.SessionId },
                        principalTable: "classroomVisionSessions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_studentEngagementObservations_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendanceDetectionCandidates_TenantId",
                table: "attendanceDetectionCandidates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_attendanceDetectionCandidates_TenantId_SessionId_ReviewStat~",
                table: "attendanceDetectionCandidates",
                columns: new[] { "TenantId", "SessionId", "ReviewStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_attendanceDetectionCandidates_TenantId_SessionId_TrackId",
                table: "attendanceDetectionCandidates",
                columns: new[] { "TenantId", "SessionId", "TrackId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_classroomVisionFrameAnalyses_TenantId",
                table: "classroomVisionFrameAnalyses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_classroomVisionFrameAnalyses_TenantId_SessionId_FrameIndex",
                table: "classroomVisionFrameAnalyses",
                columns: new[] { "TenantId", "SessionId", "FrameIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_classroomVisionSessions_TenantId",
                table: "classroomVisionSessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_classroomVisionSessions_TenantId_SchoolClassId_SessionDate",
                table: "classroomVisionSessions",
                columns: new[] { "TenantId", "SchoolClassId", "SessionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_classroomVisionSessions_TenantId_Status",
                table: "classroomVisionSessions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_classroomVisionSessions_TenantId_TeacherId_SessionDate",
                table: "classroomVisionSessions",
                columns: new[] { "TenantId", "TeacherId", "SessionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_classroomVisionSessionSummaries_TenantId",
                table: "classroomVisionSessionSummaries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_classroomVisionSessionSummaries_TenantId_SessionId",
                table: "classroomVisionSessionSummaries",
                columns: new[] { "TenantId", "SessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_studentEngagementObservations_TenantId",
                table: "studentEngagementObservations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_studentEngagementObservations_TenantId_SessionId",
                table: "studentEngagementObservations",
                columns: new[] { "TenantId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_studentEngagementObservations_TenantId_StudentId_ObservedAt",
                table: "studentEngagementObservations",
                columns: new[] { "TenantId", "StudentId", "ObservedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_studentFaceEnrollments_StudentId",
                table: "studentFaceEnrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_studentFaceEnrollments_TenantId",
                table: "studentFaceEnrollments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_studentFaceEnrollments_TenantId_ExternalLabelId",
                table: "studentFaceEnrollments",
                columns: new[] { "TenantId", "ExternalLabelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_studentFaceEnrollments_TenantId_StudentId",
                table: "studentFaceEnrollments",
                columns: new[] { "TenantId", "StudentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendanceDetectionCandidates");

            migrationBuilder.DropTable(
                name: "classroomVisionFrameAnalyses");

            migrationBuilder.DropTable(
                name: "classroomVisionSessionSummaries");

            migrationBuilder.DropTable(
                name: "studentEngagementObservations");

            migrationBuilder.DropTable(
                name: "studentFaceEnrollments");

            migrationBuilder.DropTable(
                name: "classroomVisionSessions");
        }
    }
}
