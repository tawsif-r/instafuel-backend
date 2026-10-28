using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instafuel.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "checkout_idempotency_key",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "checkout_request_fingerprint",
                table: "orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_orders_customer_id_checkout_idempotency_key",
                table: "orders",
                columns: new[] { "customer_id", "checkout_idempotency_key" },
                unique: true,
                filter: "checkout_idempotency_key <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_orders_customer_id_checkout_idempotency_key",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "checkout_idempotency_key",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "checkout_request_fingerprint",
                table: "orders");
        }
    }
}
