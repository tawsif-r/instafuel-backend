using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instafuel.Api.Migrations
{
    /// <inheritdoc />
    public partial class SupplierPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_order_status_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_order_status_histories", x => x.id);
                    table.CheckConstraint("ck_supplier_order_status_histories_status", "status IN ('pending', 'accepted', 'rejected', 'preparing', 'ready', 'dispatched', 'delivered', 'cancelled')");
                    table.ForeignKey(
                        name: "fk_supplier_order_status_histories_supplier_orders_supplier_or",
                        column: x => x.supplier_order_id,
                        principalTable: "supplier_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_order_status_histories_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_order_status_histories_recorded_by_user_id",
                table: "supplier_order_status_histories",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_order_status_histories_supplier_order_id_created_at",
                table: "supplier_order_status_histories",
                columns: new[] { "supplier_order_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_order_status_histories");
        }
    }
}
