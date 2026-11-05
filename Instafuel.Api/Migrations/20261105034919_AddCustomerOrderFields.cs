using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instafuel.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerOrderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedDeliveryAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Orders placed before this field existed: use the time they were placed.
            migrationBuilder.Sql("UPDATE \"Orders\" SET \"RequestedDeliveryAt\" = \"CreatedAt\";");

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerVehicleId",
                table: "OrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuelType",
                table: "OrderItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlateVerifiedAt",
                table: "OrderItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleName",
                table: "OrderItems",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleRegistration",
                table: "OrderItems",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_CustomerVehicleId",
                table: "OrderItems",
                column: "CustomerVehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_CustomerVehicles_CustomerVehicleId",
                table: "OrderItems",
                column: "CustomerVehicleId",
                principalTable: "CustomerVehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_CustomerVehicles_CustomerVehicleId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_CustomerVehicleId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "RequestedDeliveryAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerVehicleId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "FuelType",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "PlateVerifiedAt",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "VehicleName",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "VehicleRegistration",
                table: "OrderItems");
        }
    }
}
