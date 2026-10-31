using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instafuel.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneOtpVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "phone_verified_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "otp_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    purpose = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_otp_codes", x => x.id);
                    table.CheckConstraint("ck_otp_codes_attempts", "attempts >= 0 AND attempts <= 5");
                    table.CheckConstraint("ck_otp_codes_purpose", "purpose IN ('signup', 'login', 'reset')");
                    table.ForeignKey(
                        name: "fk_otp_codes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_otp_codes_phone_purpose_created_at",
                table: "otp_codes",
                columns: new[] { "phone", "purpose", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_otp_codes_user_id_purpose_expires_at",
                table: "otp_codes",
                columns: new[] { "user_id", "purpose", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "otp_codes");

            migrationBuilder.DropColumn(
                name: "phone_verified_at",
                table: "users");
        }
    }
}
