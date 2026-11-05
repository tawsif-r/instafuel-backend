using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instafuel.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerVehicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerVehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nickname = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    VehicleType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FuelType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VerificationStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerVehicles", x => x.Id);
                    table.CheckConstraint("CK_CustomerVehicle_FuelType", "\"FuelType\" IN ('octane', 'petrol', 'diesel')");
                    table.CheckConstraint("CK_CustomerVehicle_Type", "\"VehicleType\" IN ('car', 'motorcycle', 'microbus', 'pickup', 'truck', 'generator')");
                    table.CheckConstraint("CK_CustomerVehicle_VerificationStatus", "\"VerificationStatus\" IN ('pending', 'verified', 'rejected')");
                    table.ForeignKey(
                        name: "FK_CustomerVehicles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerVehicleDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerVehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerVehicleDocuments", x => x.Id);
                    table.CheckConstraint("CK_CustomerVehicleDocument_Kind", "\"Kind\" IN ('registration_certificate', 'fitness_certificate', 'number_plate_photo')");
                    table.ForeignKey(
                        name: "FK_CustomerVehicleDocuments_CustomerVehicles_CustomerVehicleId",
                        column: x => x.CustomerVehicleId,
                        principalTable: "CustomerVehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVehicleDocuments_CustomerVehicleId_Kind",
                table: "CustomerVehicleDocuments",
                columns: new[] { "CustomerVehicleId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVehicles_RegistrationNumber",
                table: "CustomerVehicles",
                column: "RegistrationNumber",
                unique: true,
                filter: "\"IsActive\"");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVehicles_UserId",
                table: "CustomerVehicles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerVehicleDocuments");

            migrationBuilder.DropTable(
                name: "CustomerVehicles");
        }
    }
}
