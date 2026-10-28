using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instafuel.Api.Migrations
{
    /// <inheritdoc />
    public partial class RiderDeliveryWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "cash_collected_amount",
                table: "deliveries",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "location_updated_at",
                table: "deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "proof_of_delivery_reference",
                table: "deliveries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rider_latitude",
                table: "deliveries",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rider_longitude",
                table: "deliveries",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "safety_checklist_completed_at",
                table: "deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "safety_checklist_json",
                table: "deliveries",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "safety_checklist_notes",
                table: "deliveries",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "rider_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    verification_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    vehicle_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    vehicle_registration_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rider_profiles", x => x.id);
                    table.CheckConstraint("ck_rider_profiles_registration_length", "vehicle_registration_number IS NULL OR length(vehicle_registration_number) <= 50");
                    table.CheckConstraint("ck_rider_profiles_vehicle_type_length", "vehicle_type IS NULL OR length(vehicle_type) <= 50");
                    table.CheckConstraint("ck_rider_profiles_verification_status", "verification_status IN ('pending', 'verified', 'rejected', 'suspended')");
                    table.ForeignKey(
                        name: "fk_rider_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rider_preferred_areas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rider_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rider_preferred_areas", x => x.id);
                    table.CheckConstraint("ck_rider_preferred_areas_city_not_empty", "length(trim(city)) > 0");
                    table.ForeignKey(
                        name: "fk_rider_preferred_areas_rider_profiles_rider_profile_id",
                        column: x => x.rider_profile_id,
                        principalTable: "rider_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payments_cash_active_order_id",
                table: "payments",
                column: "order_id",
                unique: true,
                filter: "method = 'cash' AND status IN ('pending', 'successful')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_deliveries_cash_collected_non_negative",
                table: "deliveries",
                sql: "cash_collected_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_deliveries_rider_latitude",
                table: "deliveries",
                sql: "rider_latitude IS NULL OR rider_latitude BETWEEN -90 AND 90");

            migrationBuilder.AddCheckConstraint(
                name: "ck_deliveries_rider_longitude",
                table: "deliveries",
                sql: "rider_longitude IS NULL OR rider_longitude BETWEEN -180 AND 180");

            migrationBuilder.CreateIndex(
                name: "ix_rider_preferred_areas_rider_profile_id_city",
                table: "rider_preferred_areas",
                columns: new[] { "rider_profile_id", "city" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rider_profiles_user_id",
                table: "rider_profiles",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rider_preferred_areas");

            migrationBuilder.DropTable(
                name: "rider_profiles");

            migrationBuilder.DropIndex(
                name: "ix_payments_cash_active_order_id",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_deliveries_cash_collected_non_negative",
                table: "deliveries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_deliveries_rider_latitude",
                table: "deliveries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_deliveries_rider_longitude",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "cash_collected_amount",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "location_updated_at",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "proof_of_delivery_reference",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "rider_latitude",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "rider_longitude",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "safety_checklist_completed_at",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "safety_checklist_json",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "safety_checklist_notes",
                table: "deliveries");
        }
    }
}
