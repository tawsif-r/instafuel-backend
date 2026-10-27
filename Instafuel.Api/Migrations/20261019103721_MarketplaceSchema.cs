using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instafuel.Api.Migrations
{
    /// <inheritdoc />
    public partial class MarketplaceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Tests",
                table: "Tests");

            migrationBuilder.RenameTable(
                name: "Tests",
                newName: "tests");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "tests",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "tests",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "tests",
                newName: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_tests",
                table: "tests",
                column: "id");

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    slug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.CheckConstraint("ck_categories_name_not_empty", "length(trim(name)) > 0");
                    table.CheckConstraint("ck_categories_slug_not_empty", "length(trim(slug)) > 0");
                    table.ForeignKey(
                        name: "fk_categories_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                    table.CheckConstraint("ck_suppliers_status", "status IN ('pending', 'active', 'suspended', 'rejected')");
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("ck_users_full_name_not_empty", "length(trim(full_name)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    product_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    unit_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fuel_grade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.CheckConstraint("ck_products_fuel_grade_required", "product_type <> 'fuel' OR (fuel_grade IS NOT NULL AND length(trim(fuel_grade)) > 0)");
                    table.CheckConstraint("ck_products_name_not_empty", "length(trim(name)) > 0");
                    table.CheckConstraint("ck_products_type", "product_type IN ('fuel', 'general')");
                    table.CheckConstraint("ck_products_unit_code_not_empty", "length(trim(unit_code)) > 0");
                    table.ForeignKey(
                        name: "fk_products_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    address_line = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_locations", x => x.id);
                    table.CheckConstraint("ck_supplier_locations_latitude", "latitude IS NULL OR latitude BETWEEN -90 AND 90");
                    table.CheckConstraint("ck_supplier_locations_longitude", "longitude IS NULL OR longitude BETWEEN -180 AND 180");
                    table.CheckConstraint("ck_supplier_locations_name_not_empty", "length(trim(name)) > 0");
                    table.ForeignKey(
                        name: "fk_supplier_locations_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "BDT"),
                    subtotal = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    delivery_fee = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    recipient_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    recipient_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    delivery_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    delivery_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    delivery_latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    delivery_longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    customer_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.CheckConstraint("ck_orders_amounts_non_negative", "subtotal >= 0 AND delivery_fee >= 0 AND discount_amount >= 0 AND total_amount >= 0");
                    table.CheckConstraint("ck_orders_discount_limit", "discount_amount <= subtotal + delivery_fee");
                    table.CheckConstraint("ck_orders_latitude", "delivery_latitude IS NULL OR delivery_latitude BETWEEN -90 AND 90");
                    table.CheckConstraint("ck_orders_longitude", "delivery_longitude IS NULL OR delivery_longitude BETWEEN -180 AND 180");
                    table.CheckConstraint("ck_orders_status", "status IN ('pending', 'confirmed', 'processing', 'partially_fulfilled', 'completed', 'cancelled', 'failed')");
                    table.CheckConstraint("ck_orders_total_amount", "total_amount = subtotal + delivery_fee - discount_amount");
                    table.ForeignKey(
                        name: "fk_orders_users_customer_id",
                        column: x => x.customer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_members",
                columns: table => new
                {
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "staff"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_members", x => new { x.supplier_id, x.user_id });
                    table.CheckConstraint("ck_supplier_members_role", "member_role IN ('owner', 'manager', 'staff')");
                    table.ForeignKey(
                        name: "fk_supplier_members_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "BDT"),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "active"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_offers", x => x.id);
                    table.CheckConstraint("ck_supplier_offers_status", "status IN ('active', 'paused', 'discontinued')");
                    table.CheckConstraint("ck_supplier_offers_unit_price", "unit_price >= 0");
                    table.ForeignKey(
                        name: "fk_supplier_offers_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_offers_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "BDT"),
                    method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    provider_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.CheckConstraint("ck_payments_amount", "amount > 0");
                    table.CheckConstraint("ck_payments_method", "method IN ('cash', 'card', 'mobile_banking', 'bank_transfer')");
                    table.CheckConstraint("ck_payments_status", "status IN ('pending', 'successful', 'failed', 'cancelled')");
                    table.ForeignKey(
                        name: "fk_payments_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    subtotal = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_orders", x => x.id);
                    table.CheckConstraint("ck_supplier_orders_status", "status IN ('pending', 'accepted', 'rejected', 'preparing', 'ready', 'dispatched', 'delivered', 'cancelled')");
                    table.CheckConstraint("ck_supplier_orders_subtotal", "subtotal >= 0");
                    table.ForeignKey(
                        name: "fk_supplier_orders_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_orders_supplier_locations_supplier_location_id",
                        column: x => x.supplier_location_id,
                        principalTable: "supplier_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_orders_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_on_hand = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    quantity_reserved = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_balances", x => x.id);
                    table.CheckConstraint("ck_inventory_balances_quantity_on_hand", "quantity_on_hand >= 0");
                    table.CheckConstraint("ck_inventory_balances_quantity_reserved", "quantity_reserved >= 0");
                    table.CheckConstraint("ck_inventory_balances_reserved_lte_on_hand", "quantity_reserved <= quantity_on_hand");
                    table.ForeignKey(
                        name: "fk_inventory_balances_supplier_locations_supplier_location_id",
                        column: x => x.supplier_location_id,
                        principalTable: "supplier_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_balances_supplier_offers_supplier_offer_id",
                        column: x => x.supplier_offer_id,
                        principalTable: "supplier_offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refunds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    provider_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refunds", x => x.id);
                    table.CheckConstraint("ck_refunds_amount", "amount > 0");
                    table.CheckConstraint("ck_refunds_status", "status IN ('pending', 'successful', 'failed', 'cancelled')");
                    table.ForeignKey(
                        name: "fk_refunds_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    picked_up_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deliveries", x => x.id);
                    table.CheckConstraint("ck_deliveries_status", "status IN ('pending', 'assigned', 'picked_up', 'in_transit', 'delivered', 'failed', 'cancelled')");
                    table.CheckConstraint("ck_deliveries_timestamps", "(picked_up_at IS NULL OR assigned_at IS NOT NULL) AND (delivered_at IS NULL OR picked_up_at IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_deliveries_supplier_orders_supplier_order_id",
                        column: x => x.supplier_order_id,
                        principalTable: "supplier_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_deliveries_users_delivery_agent_id",
                        column: x => x.delivery_agent_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    seller_name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    unit_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_items", x => x.id);
                    table.CheckConstraint("ck_order_items_line_total", "line_total = round(quantity * unit_price, 2)");
                    table.CheckConstraint("ck_order_items_quantity", "quantity > 0");
                    table.CheckConstraint("ck_order_items_unit_price", "unit_price >= 0");
                    table.ForeignKey(
                        name: "fk_order_items_supplier_offers_supplier_offer_id",
                        column: x => x.supplier_offer_id,
                        principalTable: "supplier_offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_items_supplier_orders_supplier_order_id",
                        column: x => x.supplier_order_id,
                        principalTable: "supplier_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_balance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quantity_delta = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_movements", x => x.id);
                    table.CheckConstraint("ck_inventory_movements_quantity_delta", "quantity_delta <> 0");
                    table.CheckConstraint("ck_inventory_movements_type", "movement_type IN ('receipt', 'reservation', 'release', 'sale', 'adjustment', 'return')");
                    table.ForeignKey(
                        name: "fk_inventory_movements_inventory_balances_inventory_balance_id",
                        column: x => x.inventory_balance_id,
                        principalTable: "inventory_balances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "delivery_status_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_status_histories", x => x.id);
                    table.CheckConstraint("ck_delivery_status_histories_status", "status IN ('pending', 'assigned', 'picked_up', 'in_transit', 'delivered', 'failed', 'cancelled')");
                    table.ForeignKey(
                        name: "fk_delivery_status_histories_deliveries_delivery_id",
                        column: x => x.delivery_id,
                        principalTable: "deliveries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_delivery_status_histories_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_id",
                table: "categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_categories_slug",
                table: "categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_deliveries_delivery_agent_id",
                table: "deliveries",
                column: "delivery_agent_id");

            migrationBuilder.CreateIndex(
                name: "ix_deliveries_status",
                table: "deliveries",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_deliveries_supplier_order_id",
                table: "deliveries",
                column: "supplier_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_status_histories_created_at",
                table: "delivery_status_histories",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_status_histories_delivery_id",
                table: "delivery_status_histories",
                column: "delivery_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_status_histories_recorded_by_user_id",
                table: "delivery_status_histories",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_balances_supplier_location_id_supplier_offer_id",
                table: "inventory_balances",
                columns: new[] { "supplier_location_id", "supplier_offer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_balances_supplier_offer_id",
                table: "inventory_balances",
                column: "supplier_offer_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_created_at",
                table: "inventory_movements",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_inventory_balance_id",
                table: "inventory_movements",
                column: "inventory_balance_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_reference_type_reference_id",
                table: "inventory_movements",
                columns: new[] { "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "ix_order_items_supplier_offer_id",
                table: "order_items",
                column: "supplier_offer_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_supplier_order_id",
                table: "order_items",
                column: "supplier_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_created_at",
                table: "orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_orders_customer_id",
                table: "orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_order_number",
                table: "orders",
                column: "order_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_status",
                table: "orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_payments_idempotency_key",
                table: "payments",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key <> ''");

            migrationBuilder.CreateIndex(
                name: "ix_payments_order_id",
                table: "payments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_status",
                table: "payments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_name",
                table: "products",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_products_product_type",
                table: "products",
                column: "product_type");

            migrationBuilder.CreateIndex(
                name: "ix_refunds_payment_id",
                table: "refunds",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_refunds_status",
                table: "refunds",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_locations_city_is_active",
                table: "supplier_locations",
                columns: new[] { "city", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_locations_supplier_id",
                table: "supplier_locations",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_members_member_role",
                table: "supplier_members",
                column: "member_role");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_members_user_id",
                table: "supplier_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_offers_product_id",
                table: "supplier_offers",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_offers_status",
                table: "supplier_offers",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_offers_supplier_id",
                table: "supplier_offers",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_offers_supplier_id_product_id",
                table: "supplier_offers",
                columns: new[] { "supplier_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_orders_order_id",
                table: "supplier_orders",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_orders_status",
                table: "supplier_orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_orders_supplier_id",
                table: "supplier_orders",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_orders_supplier_location_id",
                table: "supplier_orders",
                column: "supplier_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_status",
                table: "suppliers",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true,
                filter: "email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_users_phone",
                table: "users",
                column: "phone",
                unique: true,
                filter: "phone IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "delivery_status_histories");

            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "refunds");

            migrationBuilder.DropTable(
                name: "supplier_members");

            migrationBuilder.DropTable(
                name: "deliveries");

            migrationBuilder.DropTable(
                name: "inventory_balances");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "supplier_orders");

            migrationBuilder.DropTable(
                name: "supplier_offers");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "supplier_locations");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropPrimaryKey(
                name: "pk_tests",
                table: "tests");

            migrationBuilder.RenameTable(
                name: "tests",
                newName: "Tests");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Tests",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "Tests",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Tests",
                newName: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tests",
                table: "Tests",
                column: "Id");
        }
    }
}
