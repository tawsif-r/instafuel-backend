using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instafuel.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleDocumentFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "CustomerVehicleDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "CustomerVehicleDocuments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "CustomerVehicleDocuments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "CustomerVehicleDocuments");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "CustomerVehicleDocuments");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "CustomerVehicleDocuments");
        }
    }
}
