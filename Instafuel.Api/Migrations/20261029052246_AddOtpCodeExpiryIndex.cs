using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instafuel.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpCodeExpiryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_otp_codes_expires_at",
                table: "otp_codes",
                column: "expires_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_otp_codes_expires_at",
                table: "otp_codes");
        }
    }
}
