using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase4TenantIntegrityFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safety backfill (runs BEFORE the NOT NULL conversions below). On a clean
            // database this is a no-op. If any pre-existing tenant-owned row has a NULL
            // tenant, it is quarantined under a reserved, suspended tenant so the
            // NOT NULL + FK constraints can be applied WITHOUT losing the row. This makes
            // the nullable->required tenant change safe to run against existing data.
            migrationBuilder.Sql(@"
DO $$
DECLARE
    t text;
    n int;
    tables text[] := ARRAY[
        'announcements','grades','""lessonMaterials""','lessons','notifications','posts',
        '""questionOptions""','questions','""quizGenerations""','""quizSubmissions""','quizzes',
        '""studentInsights""','""studentLessonProgresses""','subjects','""submissionAnswers""',
        '""supportRequests""','units'];
    need_quarantine boolean := false;
BEGIN
    FOREACH t IN ARRAY tables LOOP
        EXECUTE format('SELECT count(*) FROM %s WHERE ""TenantId"" IS NULL', t) INTO n;
        IF n > 0 THEN need_quarantine := true; END IF;
    END LOOP;

    IF need_quarantine THEN
        INSERT INTO tenants (""Id"", ""Name"", ""Domain"", ""SubscriptionPlan"", ""Type"", ""Status"")
        VALUES ('__quarantine__', 'Quarantined Legacy Data', '', 'Free', 'National', 'Suspended')
        ON CONFLICT (""Id"") DO NOTHING;

        FOREACH t IN ARRAY tables LOOP
            EXECUTE format('UPDATE %s SET ""TenantId"" = ''__quarantine__'' WHERE ""TenantId"" IS NULL', t);
        END LOOP;
    END IF;
END $$;
");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "units",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "supportRequests",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "submissionAnswers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "subjects",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "studentLessonProgresses",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "studentInsights",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "quizzes",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "quizSubmissions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "quizGenerations",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "questions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "questionOptions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "posts",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "notifications",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "lessons",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "lessonMaterials",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "grades",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "announcements",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_announcements_tenants_TenantId",
                table: "announcements",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_grades_tenants_TenantId",
                table: "grades",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_lessonMaterials_tenants_TenantId",
                table: "lessonMaterials",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_lessons_tenants_TenantId",
                table: "lessons",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_tenants_TenantId",
                table: "notifications",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_posts_tenants_TenantId",
                table: "posts",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_questionOptions_tenants_TenantId",
                table: "questionOptions",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_questions_tenants_TenantId",
                table: "questions",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_quizGenerations_tenants_TenantId",
                table: "quizGenerations",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_quizSubmissions_tenants_TenantId",
                table: "quizSubmissions",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_quizzes_tenants_TenantId",
                table: "quizzes",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_studentInsights_tenants_TenantId",
                table: "studentInsights",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_studentLessonProgresses_tenants_TenantId",
                table: "studentLessonProgresses",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_subjects_tenants_TenantId",
                table: "subjects",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_submissionAnswers_tenants_TenantId",
                table: "submissionAnswers",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_supportRequests_tenants_TenantId",
                table: "supportRequests",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_units_tenants_TenantId",
                table: "units",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_announcements_tenants_TenantId",
                table: "announcements");

            migrationBuilder.DropForeignKey(
                name: "FK_grades_tenants_TenantId",
                table: "grades");

            migrationBuilder.DropForeignKey(
                name: "FK_lessonMaterials_tenants_TenantId",
                table: "lessonMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_lessons_tenants_TenantId",
                table: "lessons");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_tenants_TenantId",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_posts_tenants_TenantId",
                table: "posts");

            migrationBuilder.DropForeignKey(
                name: "FK_questionOptions_tenants_TenantId",
                table: "questionOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_questions_tenants_TenantId",
                table: "questions");

            migrationBuilder.DropForeignKey(
                name: "FK_quizGenerations_tenants_TenantId",
                table: "quizGenerations");

            migrationBuilder.DropForeignKey(
                name: "FK_quizSubmissions_tenants_TenantId",
                table: "quizSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_quizzes_tenants_TenantId",
                table: "quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_studentInsights_tenants_TenantId",
                table: "studentInsights");

            migrationBuilder.DropForeignKey(
                name: "FK_studentLessonProgresses_tenants_TenantId",
                table: "studentLessonProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_subjects_tenants_TenantId",
                table: "subjects");

            migrationBuilder.DropForeignKey(
                name: "FK_submissionAnswers_tenants_TenantId",
                table: "submissionAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_supportRequests_tenants_TenantId",
                table: "supportRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_units_tenants_TenantId",
                table: "units");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "units",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "supportRequests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "submissionAnswers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "subjects",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "studentLessonProgresses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "studentInsights",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "quizzes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "quizSubmissions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "quizGenerations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "questions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "questionOptions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "posts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "notifications",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "lessons",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "lessonMaterials",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "grades",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "announcements",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);
        }
    }
}
