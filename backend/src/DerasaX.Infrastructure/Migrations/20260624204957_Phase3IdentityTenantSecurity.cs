using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3IdentityTenantSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_tenants_TenantId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "Token",
                table: "RefreshToken",
                newName: "TokenHash");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "tenants",
                type: "text",
                nullable: false,
                // Existing tenants default to Active (enum stored as string).
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "FamilyId",
                table: "RefreshToken",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Jti",
                table: "RefreshToken",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReplacedByTokenHash",
                table: "RefreshToken",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevokedReason",
                table: "RefreshToken",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_tenants_TenantId",
                table: "AspNetUsers",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_tenants_TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "RefreshToken");

            migrationBuilder.DropColumn(
                name: "Jti",
                table: "RefreshToken");

            migrationBuilder.DropColumn(
                name: "ReplacedByTokenHash",
                table: "RefreshToken");

            migrationBuilder.DropColumn(
                name: "RevokedReason",
                table: "RefreshToken");

            migrationBuilder.RenameColumn(
                name: "TokenHash",
                table: "RefreshToken",
                newName: "Token");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_tenants_TenantId",
                table: "AspNetUsers",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
