using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionPlanAdditionalLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxAiTokensPerMonth",
                table: "subscriptionPlanDefinitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxClasses",
                table: "subscriptionPlanDefinitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxLessonMaterials",
                table: "subscriptionPlanDefinitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxParents",
                table: "subscriptionPlanDefinitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxSchoolAdmins",
                table: "subscriptionPlanDefinitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxSubjects",
                table: "subscriptionPlanDefinitions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxAiTokensPerMonth",
                table: "subscriptionPlanDefinitions");

            migrationBuilder.DropColumn(
                name: "MaxClasses",
                table: "subscriptionPlanDefinitions");

            migrationBuilder.DropColumn(
                name: "MaxLessonMaterials",
                table: "subscriptionPlanDefinitions");

            migrationBuilder.DropColumn(
                name: "MaxParents",
                table: "subscriptionPlanDefinitions");

            migrationBuilder.DropColumn(
                name: "MaxSchoolAdmins",
                table: "subscriptionPlanDefinitions");

            migrationBuilder.DropColumn(
                name: "MaxSubjects",
                table: "subscriptionPlanDefinitions");
        }
    }
}
