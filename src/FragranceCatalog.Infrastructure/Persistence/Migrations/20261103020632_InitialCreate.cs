using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FragranceCatalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "brands",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_brands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "price_ranges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    min_cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    max_cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_ranges", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pricing_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    active_base_strategy = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    base_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    rounding_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    min_profit = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    cost_change_alert_percent = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pricing_settings", x => x.id);
                    table.CheckConstraint("ck_pricing_settings_single_row", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "store_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    store_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    whatsapp_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    whatsapp_message_template = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_store_settings", x => x.id);
                    table.CheckConstraint("ck_store_settings_single_row", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    base_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    trigger = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_count = table.Column<int>(type: "integer", nullable: false),
                    price_changed_count = table.Column<int>(type: "integer", nullable: false),
                    discontinued_count = table.Column<int>(type: "integer", nullable: false),
                    reactivated_count = table.Column<int>(type: "integer", nullable: false),
                    error_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    is_daily_sync_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    daily_sync_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    failure_threshold_percent = table.Column<int>(type: "integer", nullable: false),
                    delay_between_requests_ms = table.Column<int>(type: "integer", nullable: false),
                    request_timeout_seconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_settings", x => x.id);
                    table.CheckConstraint("ck_sync_settings_single_row", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_product_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gender = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    size_ml = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    is_discontinued = table.Column<bool>(type: "boolean", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_products_brands_brand_id",
                        column: x => x.brand_id,
                        principalTable: "brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sync_run_errors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sync_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_run_errors", x => x.id);
                    table.ForeignKey(
                        name: "fk_sync_run_errors_sync_runs_sync_run_id",
                        column: x => x.sync_run_id,
                        principalTable: "sync_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pricing_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pricing_rules", x => x.id);
                    table.CheckConstraint("ck_pricing_rules_target_matches_scope", "(scope = 'Brand' AND brand_id IS NOT NULL AND product_id IS NULL) OR (scope = 'Product' AND product_id IS NOT NULL AND brand_id IS NULL)");
                    table.ForeignKey(
                        name: "fk_pricing_rules_brands_brand_id",
                        column: x => x.brand_id,
                        principalTable: "brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pricing_rules_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_images_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplier_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    regular_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    sale_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_prices_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "price_ranges",
                columns: new[] { "id", "action", "max_cost", "min_cost", "value" },
                values: new object[,]
                {
                    { new Guid("0193a6f0-0000-7000-8000-000000000101"), "AddAmount", 1000m, 0m, 200m },
                    { new Guid("0193a6f0-0000-7000-8000-000000000102"), "AddAmount", null, 1000m, 300m }
                });

            migrationBuilder.InsertData(
                table: "pricing_settings",
                columns: new[] { "id", "active_base_strategy", "base_percent", "cost_change_alert_percent", "min_profit", "rounding_mode" },
                values: new object[] { 1, "Ranges", 10m, 15, 100m, "None" });

            migrationBuilder.InsertData(
                table: "store_settings",
                columns: new[] { "id", "store_name", "whatsapp_message_template", "whatsapp_number" },
                values: new object[] { 1, "Dolce Aroma", "Hola, me interesa el {product} ({price})", "" });

            migrationBuilder.InsertData(
                table: "suppliers",
                columns: new[] { "id", "base_url", "is_active", "name" },
                values: new object[] { new Guid("0193a6f0-0000-7000-8000-000000000001"), "https://magnaperfumes.com", true, "Magna Perfumes" });

            migrationBuilder.InsertData(
                table: "sync_settings",
                columns: new[] { "id", "daily_sync_time", "delay_between_requests_ms", "failure_threshold_percent", "is_daily_sync_enabled", "request_timeout_seconds", "time_zone_id" },
                values: new object[] { 1, new TimeOnly(4, 0, 0), 1000, 20, true, 30, "America/Mexico_City" });

            migrationBuilder.CreateIndex(
                name: "ix_brands_slug",
                table: "brands",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pricing_rules_active_brand_id",
                table: "pricing_rules",
                column: "brand_id",
                unique: true,
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_pricing_rules_active_product_id",
                table: "pricing_rules",
                column: "product_id",
                unique: true,
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_id_sort_order",
                table: "product_images",
                columns: new[] { "product_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_products_brand_id",
                table: "products",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_slug",
                table: "products",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_supplier_id_supplier_product_id",
                table: "products",
                columns: new[] { "supplier_id", "supplier_product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_prices_product_id_captured_at",
                table: "supplier_prices",
                columns: new[] { "product_id", "captured_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sync_run_errors_sync_run_id",
                table: "sync_run_errors",
                column: "sync_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_sync_runs_started_at",
                table: "sync_runs",
                column: "started_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "price_ranges");

            migrationBuilder.DropTable(
                name: "pricing_rules");

            migrationBuilder.DropTable(
                name: "pricing_settings");

            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "store_settings");

            migrationBuilder.DropTable(
                name: "supplier_prices");

            migrationBuilder.DropTable(
                name: "sync_run_errors");

            migrationBuilder.DropTable(
                name: "sync_settings");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "sync_runs");

            migrationBuilder.DropTable(
                name: "brands");

            migrationBuilder.DropTable(
                name: "suppliers");
        }
    }
}
