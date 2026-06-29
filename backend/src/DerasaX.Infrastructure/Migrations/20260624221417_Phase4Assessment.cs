using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase4Assessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_questionOptions_questions_QuestionId",
                table: "questionOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_questions_quizzes_QuizId",
                table: "questions");

            migrationBuilder.DropForeignKey(
                name: "FK_quizGenerations_quizzes_QuizId",
                table: "quizGenerations");

            migrationBuilder.DropForeignKey(
                name: "FK_quizSubmissions_Student_StudentId",
                table: "quizSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_quizSubmissions_quizzes_QuizId",
                table: "quizSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_submissionAnswers_questionOptions_SelectedOptionId",
                table: "submissionAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_submissionAnswers_questions_QuestionId",
                table: "submissionAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_submissionAnswers_quizSubmissions_QuizSubmissionId",
                table: "submissionAnswers");

            migrationBuilder.DropIndex(
                name: "IX_submissionAnswers_QuestionId",
                table: "submissionAnswers");

            migrationBuilder.DropIndex(
                name: "IX_submissionAnswers_QuizSubmissionId",
                table: "submissionAnswers");

            migrationBuilder.DropIndex(
                name: "IX_quizSubmissions_QuizId",
                table: "quizSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_quizGenerations_QuizId",
                table: "quizGenerations");

            migrationBuilder.DropIndex(
                name: "IX_questions_QuizId",
                table: "questions");

            migrationBuilder.DropIndex(
                name: "IX_questionOptions_QuestionId",
                table: "questionOptions");

            migrationBuilder.AlterColumn<string>(
                name: "QuizSubmissionId",
                table: "submissionAnswers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "QuestionId",
                table: "submissionAnswers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "AnswerText",
                table: "submissionAnswers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "submissionAnswers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GradedAt",
                table: "submissionAnswers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradedByTeacherId",
                table: "submissionAnswers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradingMethod",
                table: "submissionAnswers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "quizzes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByTeacherId",
                table: "quizzes",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxAttempts",
                table: "quizzes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "quizzes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "quizzes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByTeacherId",
                table: "quizzes",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "quizzes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "quizzes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StudentId",
                table: "quizSubmissions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "QuizId",
                table: "quizSubmissions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentId",
                table: "quizSubmissions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "quizSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "GradedAt",
                table: "quizSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradedByTeacherId",
                table: "quizSubmissions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradingMethod",
                table: "quizSubmissions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsLatestAttempt",
                table: "quizSubmissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "quizSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "QuizId",
                table: "quizGenerations",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "AiModel",
                table: "quizGenerations",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiProvider",
                table: "quizGenerations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "quizGenerations",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorCategory",
                table: "quizGenerations",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "quizGenerations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptVersion",
                table: "quizGenerations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "quizGenerations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByTeacherId",
                table: "quizGenerations",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "quizGenerations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TokensUsed",
                table: "quizGenerations",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "QuizId",
                table: "questions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "CorrectAnswerText",
                table: "questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "questions",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "QuestionId",
                table: "questionOptions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_quizzes_TenantId_Id",
                table: "quizzes",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_quizSubmissions_TenantId_Id",
                table: "quizSubmissions",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_questions_TenantId_Id",
                table: "questions",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "assignments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AvailableFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxScore = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    AssignedByTeacherId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    QuizId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    SubjectId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LessonId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
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
                    table.PrimaryKey("PK_assignments", x => x.Id);
                    table.UniqueConstraint("AK_assignments_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_assignments_quizzes_TenantId_QuizId",
                        columns: x => new { x.TenantId, x.QuizId },
                        principalTable: "quizzes",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assignments_subjects_TenantId_SubjectId",
                        columns: x => new { x.TenantId, x.SubjectId },
                        principalTable: "subjects",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assignments_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assignmentTargets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AssignmentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TargetType = table.Column<string>(type: "text", nullable: false),
                    SchoolClassId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    GradeId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
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
                    table.PrimaryKey("PK_assignmentTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assignmentTargets_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assignmentTargets_assignments_TenantId_AssignmentId",
                        columns: x => new { x.TenantId, x.AssignmentId },
                        principalTable: "assignments",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assignmentTargets_schoolClasses_TenantId_SchoolClassId",
                        columns: x => new { x.TenantId, x.SchoolClassId },
                        principalTable: "schoolClasses",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assignmentTargets_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_submissionAnswers_TenantId_QuestionId",
                table: "submissionAnswers",
                columns: new[] { "TenantId", "QuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_submissionAnswers_TenantId_QuizSubmissionId",
                table: "submissionAnswers",
                columns: new[] { "TenantId", "QuizSubmissionId" });

            migrationBuilder.CreateIndex(
                name: "IX_quizzes_TenantId_Status",
                table: "quizzes",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_quizSubmissions_TenantId_AssignmentId",
                table: "quizSubmissions",
                columns: new[] { "TenantId", "AssignmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_quizSubmissions_TenantId_QuizId",
                table: "quizSubmissions",
                columns: new[] { "TenantId", "QuizId" });

            migrationBuilder.CreateIndex(
                name: "IX_quizSubmissions_TenantId_StudentId_QuizId_AttemptNumber",
                table: "quizSubmissions",
                columns: new[] { "TenantId", "StudentId", "QuizId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quizGenerations_TenantId_QuizId",
                table: "quizGenerations",
                columns: new[] { "TenantId", "QuizId" });

            migrationBuilder.CreateIndex(
                name: "IX_questions_TenantId_QuizId",
                table: "questions",
                columns: new[] { "TenantId", "QuizId" });

            migrationBuilder.CreateIndex(
                name: "IX_questionOptions_TenantId_QuestionId",
                table: "questionOptions",
                columns: new[] { "TenantId", "QuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_assignments_TenantId",
                table: "assignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_assignments_TenantId_QuizId",
                table: "assignments",
                columns: new[] { "TenantId", "QuizId" });

            migrationBuilder.CreateIndex(
                name: "IX_assignments_TenantId_Status",
                table: "assignments",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_assignments_TenantId_SubjectId",
                table: "assignments",
                columns: new[] { "TenantId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_assignmentTargets_StudentId",
                table: "assignmentTargets",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_assignmentTargets_TenantId",
                table: "assignmentTargets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_assignmentTargets_TenantId_AssignmentId_TargetType",
                table: "assignmentTargets",
                columns: new[] { "TenantId", "AssignmentId", "TargetType" });

            migrationBuilder.CreateIndex(
                name: "IX_assignmentTargets_TenantId_SchoolClassId",
                table: "assignmentTargets",
                columns: new[] { "TenantId", "SchoolClassId" });

            migrationBuilder.AddForeignKey(
                name: "FK_questionOptions_questions_TenantId_QuestionId",
                table: "questionOptions",
                columns: new[] { "TenantId", "QuestionId" },
                principalTable: "questions",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_questions_quizzes_TenantId_QuizId",
                table: "questions",
                columns: new[] { "TenantId", "QuizId" },
                principalTable: "quizzes",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_quizGenerations_quizzes_TenantId_QuizId",
                table: "quizGenerations",
                columns: new[] { "TenantId", "QuizId" },
                principalTable: "quizzes",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_quizSubmissions_Student_StudentId",
                table: "quizSubmissions",
                column: "StudentId",
                principalTable: "Student",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_quizSubmissions_quizzes_TenantId_QuizId",
                table: "quizSubmissions",
                columns: new[] { "TenantId", "QuizId" },
                principalTable: "quizzes",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_submissionAnswers_questionOptions_SelectedOptionId",
                table: "submissionAnswers",
                column: "SelectedOptionId",
                principalTable: "questionOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_submissionAnswers_questions_TenantId_QuestionId",
                table: "submissionAnswers",
                columns: new[] { "TenantId", "QuestionId" },
                principalTable: "questions",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_submissionAnswers_quizSubmissions_TenantId_QuizSubmissionId",
                table: "submissionAnswers",
                columns: new[] { "TenantId", "QuizSubmissionId" },
                principalTable: "quizSubmissions",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            // Same-tenant integrity for user references that cannot use composite FKs
            // (student is an ApplicationUser whose tenant is nullable). Reuses the
            // trigger function created in the academic-structure migration.
            migrationBuilder.Sql(@"
CREATE TRIGGER trg_submission_student_tenant
    BEFORE INSERT OR UPDATE ON ""quizSubmissions""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');

CREATE TRIGGER trg_assignmenttarget_student_tenant
    BEFORE INSERT OR UPDATE ON ""assignmentTargets""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_assignmenttarget_student_tenant ON ""assignmentTargets"";
DROP TRIGGER IF EXISTS trg_submission_student_tenant ON ""quizSubmissions"";
");

            migrationBuilder.DropForeignKey(
                name: "FK_questionOptions_questions_TenantId_QuestionId",
                table: "questionOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_questions_quizzes_TenantId_QuizId",
                table: "questions");

            migrationBuilder.DropForeignKey(
                name: "FK_quizGenerations_quizzes_TenantId_QuizId",
                table: "quizGenerations");

            migrationBuilder.DropForeignKey(
                name: "FK_quizSubmissions_Student_StudentId",
                table: "quizSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_quizSubmissions_quizzes_TenantId_QuizId",
                table: "quizSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_submissionAnswers_questionOptions_SelectedOptionId",
                table: "submissionAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_submissionAnswers_questions_TenantId_QuestionId",
                table: "submissionAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_submissionAnswers_quizSubmissions_TenantId_QuizSubmissionId",
                table: "submissionAnswers");

            migrationBuilder.DropTable(
                name: "assignmentTargets");

            migrationBuilder.DropTable(
                name: "assignments");

            migrationBuilder.DropIndex(
                name: "IX_submissionAnswers_TenantId_QuestionId",
                table: "submissionAnswers");

            migrationBuilder.DropIndex(
                name: "IX_submissionAnswers_TenantId_QuizSubmissionId",
                table: "submissionAnswers");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_quizzes_TenantId_Id",
                table: "quizzes");

            migrationBuilder.DropIndex(
                name: "IX_quizzes_TenantId_Status",
                table: "quizzes");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_quizSubmissions_TenantId_Id",
                table: "quizSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_quizSubmissions_TenantId_AssignmentId",
                table: "quizSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_quizSubmissions_TenantId_QuizId",
                table: "quizSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_quizSubmissions_TenantId_StudentId_QuizId_AttemptNumber",
                table: "quizSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_quizGenerations_TenantId_QuizId",
                table: "quizGenerations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_questions_TenantId_Id",
                table: "questions");

            migrationBuilder.DropIndex(
                name: "IX_questions_TenantId_QuizId",
                table: "questions");

            migrationBuilder.DropIndex(
                name: "IX_questionOptions_TenantId_QuestionId",
                table: "questionOptions");

            migrationBuilder.DropColumn(
                name: "AnswerText",
                table: "submissionAnswers");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "submissionAnswers");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "submissionAnswers");

            migrationBuilder.DropColumn(
                name: "GradedByTeacherId",
                table: "submissionAnswers");

            migrationBuilder.DropColumn(
                name: "GradingMethod",
                table: "submissionAnswers");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "ApprovedByTeacherId",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "MaxAttempts",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "ReviewedByTeacherId",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "AssignmentId",
                table: "quizSubmissions");

            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "quizSubmissions");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "quizSubmissions");

            migrationBuilder.DropColumn(
                name: "GradedByTeacherId",
                table: "quizSubmissions");

            migrationBuilder.DropColumn(
                name: "GradingMethod",
                table: "quizSubmissions");

            migrationBuilder.DropColumn(
                name: "IsLatestAttempt",
                table: "quizSubmissions");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "quizSubmissions");

            migrationBuilder.DropColumn(
                name: "AiModel",
                table: "quizGenerations");

            migrationBuilder.DropColumn(
                name: "AiProvider",
                table: "quizGenerations");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "quizGenerations");

            migrationBuilder.DropColumn(
                name: "ErrorCategory",
                table: "quizGenerations");

            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "quizGenerations");

            migrationBuilder.DropColumn(
                name: "PromptVersion",
                table: "quizGenerations");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "quizGenerations");

            migrationBuilder.DropColumn(
                name: "ReviewedByTeacherId",
                table: "quizGenerations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "quizGenerations");

            migrationBuilder.DropColumn(
                name: "TokensUsed",
                table: "quizGenerations");

            migrationBuilder.DropColumn(
                name: "CorrectAnswerText",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "questions");

            migrationBuilder.AlterColumn<string>(
                name: "QuizSubmissionId",
                table: "submissionAnswers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "QuestionId",
                table: "submissionAnswers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "StudentId",
                table: "quizSubmissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "QuizId",
                table: "quizSubmissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "QuizId",
                table: "quizGenerations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "QuizId",
                table: "questions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "QuestionId",
                table: "questionOptions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.CreateIndex(
                name: "IX_submissionAnswers_QuestionId",
                table: "submissionAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_submissionAnswers_QuizSubmissionId",
                table: "submissionAnswers",
                column: "QuizSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_quizSubmissions_QuizId",
                table: "quizSubmissions",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_quizGenerations_QuizId",
                table: "quizGenerations",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_questions_QuizId",
                table: "questions",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_questionOptions_QuestionId",
                table: "questionOptions",
                column: "QuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_questionOptions_questions_QuestionId",
                table: "questionOptions",
                column: "QuestionId",
                principalTable: "questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_questions_quizzes_QuizId",
                table: "questions",
                column: "QuizId",
                principalTable: "quizzes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_quizGenerations_quizzes_QuizId",
                table: "quizGenerations",
                column: "QuizId",
                principalTable: "quizzes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_quizSubmissions_Student_StudentId",
                table: "quizSubmissions",
                column: "StudentId",
                principalTable: "Student",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_quizSubmissions_quizzes_QuizId",
                table: "quizSubmissions",
                column: "QuizId",
                principalTable: "quizzes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_submissionAnswers_questionOptions_SelectedOptionId",
                table: "submissionAnswers",
                column: "SelectedOptionId",
                principalTable: "questionOptions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_submissionAnswers_questions_QuestionId",
                table: "submissionAnswers",
                column: "QuestionId",
                principalTable: "questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_submissionAnswers_quizSubmissions_QuizSubmissionId",
                table: "submissionAnswers",
                column: "QuizSubmissionId",
                principalTable: "quizSubmissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
