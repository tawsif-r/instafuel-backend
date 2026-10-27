using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instafuel.Api.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeRoleConstraintNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normalize only the role constraints created by the preceding
            // migration. The older marketplace tables already use snake_case.
            migrationBuilder.DropForeignKey("FK_user_roles_roles_role_id", "user_roles");
            migrationBuilder.DropForeignKey("FK_user_roles_users_user_id", "user_roles");
            migrationBuilder.DropPrimaryKey("PK_user_roles", "user_roles");
            migrationBuilder.DropPrimaryKey("PK_roles", "roles");

            migrationBuilder.RenameIndex(
                name: "IX_roles_name",
                table: "roles",
                newName: "ix_roles_name");
            migrationBuilder.RenameIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                newName: "ix_user_roles_role_id");

            migrationBuilder.AddPrimaryKey("pk_roles", "roles", "id");
            migrationBuilder.AddPrimaryKey("pk_user_roles", "user_roles", new[] { "user_id", "role_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_user_roles_roles_role_id",
                table: "user_roles",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "fk_user_roles_users_user_id",
                table: "user_roles",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("fk_user_roles_roles_role_id", "user_roles");
            migrationBuilder.DropForeignKey("fk_user_roles_users_user_id", "user_roles");
            migrationBuilder.DropPrimaryKey("pk_user_roles", "user_roles");
            migrationBuilder.DropPrimaryKey("pk_roles", "roles");

            migrationBuilder.RenameIndex(
                name: "ix_roles_name",
                table: "roles",
                newName: "IX_roles_name");
            migrationBuilder.RenameIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                newName: "IX_user_roles_role_id");

            migrationBuilder.AddPrimaryKey("PK_roles", "roles", "id");
            migrationBuilder.AddPrimaryKey("PK_user_roles", "user_roles", new[] { "user_id", "role_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_user_roles_roles_role_id",
                table: "user_roles",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_user_roles_users_user_id",
                table: "user_roles",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
