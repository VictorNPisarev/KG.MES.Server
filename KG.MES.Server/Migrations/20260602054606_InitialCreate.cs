using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KG.MES.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legacy_id = table.Column<string>(type: "text", nullable: true),
                    order_number = table.Column<string>(type: "text", nullable: false),
                    ready_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    window_count = table.Column<int>(type: "integer", nullable: false),
                    window_area = table.Column<decimal>(type: "numeric", nullable: false),
                    plate_count = table.Column<int>(type: "integer", nullable: false),
                    plate_area = table.Column<decimal>(type: "numeric", nullable: false),
                    is_econom = table.Column<bool>(type: "boolean", nullable: false),
                    is_claim = table.Column<bool>(type: "boolean", nullable: false),
                    is_only_paid = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comment_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legacy_id = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "supply_conditions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    condition_code = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supply_conditions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "supply_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    unit = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supply_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workplaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legacy_id = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    previous_workplace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_workplace = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workplaces", x => x.id);
                    table.ForeignKey(
                        name: "FK_workplaces_workplaces_previous_workplace_id",
                        column: x => x.previous_workplace_id,
                        principalTable: "workplaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_supply",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comment_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_supply", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_supply_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legacy_id = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "production_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legacy_id = table.Column<string>(type: "text", nullable: true),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_workplace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    lumber = table.Column<string>(type: "text", nullable: true),
                    glazing_bead = table.Column<string>(type: "text", nullable: true),
                    is_two_side_paint = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comment_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_production_orders_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_production_orders_workplaces_current_workplace_id",
                        column: x => x.current_workplace_id,
                        principalTable: "workplaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workplace_transitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legacy_id = table.Column<string>(type: "text", nullable: true),
                    from_workplace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_workplace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transition_type = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workplace_transitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_workplace_transitions_workplaces_from_workplace_id",
                        column: x => x.from_workplace_id,
                        principalTable: "workplaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workplace_transitions_workplaces_to_workplace_id",
                        column: x => x.to_workplace_id,
                        principalTable: "workplaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comments", x => x.id);
                    table.ForeignKey(
                        name: "FK_comments_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_comments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_workplaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legacy_id = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workplace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_workplaces", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_workplaces_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_workplaces_workplaces_workplace_id",
                        column: x => x.workplace_id,
                        principalTable: "workplaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "operation_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legacy_id = table.Column<string>(type: "text", nullable: true),
                    production_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workplace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    operation_type = table.Column<string>(type: "text", nullable: false),
                    operation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operation_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_operation_logs_production_orders_production_order_id",
                        column: x => x.production_order_id,
                        principalTable: "production_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_operation_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_operation_logs_workplaces_workplace_id",
                        column: x => x.workplace_id,
                        principalTable: "workplaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_blocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legacy_id = table.Column<string>(type: "text", nullable: true),
                    production_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workplace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    blocked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    source = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_blocks", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_blocks_production_orders_production_order_id",
                        column: x => x.production_order_id,
                        principalTable: "production_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_blocks_users_resolved_by",
                        column: x => x.resolved_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_order_blocks_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_order_blocks_workplaces_workplace_id",
                        column: x => x.workplace_id,
                        principalTable: "workplaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_footprints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legacy_id = table.Column<string>(type: "text", nullable: true),
                    production_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workplace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_footprints", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_footprints_production_orders_production_order_id",
                        column: x => x.production_order_id,
                        principalTable: "production_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_footprints_workplaces_workplace_id",
                        column: x => x.workplace_id,
                        principalTable: "workplaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supply_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_supply_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supply_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    condition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric", nullable: true),
                    expected_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comment_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supply_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_supply_items_comments_comment_id",
                        column: x => x.comment_id,
                        principalTable: "comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_supply_items_order_supply_order_supply_id",
                        column: x => x.order_supply_id,
                        principalTable: "order_supply",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_supply_items_supply_conditions_condition_id",
                        column: x => x.condition_id,
                        principalTable: "supply_conditions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_supply_items_supply_types_supply_type_id",
                        column: x => x.supply_type_id,
                        principalTable: "supply_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_comments_order_id",
                table: "comments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_comments_user_id",
                table: "comments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_operation_logs_production_order_id",
                table: "operation_logs",
                column: "production_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_operation_logs_user_id",
                table: "operation_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_operation_logs_workplace_id",
                table: "operation_logs",
                column: "workplace_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_blocks_production_order_id",
                table: "order_blocks",
                column: "production_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_blocks_resolved_by",
                table: "order_blocks",
                column: "resolved_by");

            migrationBuilder.CreateIndex(
                name: "IX_order_blocks_user_id",
                table: "order_blocks",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_blocks_workplace_id",
                table: "order_blocks",
                column: "workplace_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_footprints_production_order_id_workplace_id",
                table: "order_footprints",
                columns: new[] { "production_order_id", "workplace_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_footprints_workplace_id",
                table: "order_footprints",
                column: "workplace_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_supply_order_id",
                table: "order_supply",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_legacy_id",
                table: "orders",
                column: "legacy_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_order_number",
                table: "orders",
                column: "order_number");

            migrationBuilder.CreateIndex(
                name: "IX_production_orders_current_workplace_id",
                table: "production_orders",
                column: "current_workplace_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_orders_legacy_id",
                table: "production_orders",
                column: "legacy_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_orders_order_id",
                table: "production_orders",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supply_items_comment_id",
                table: "supply_items",
                column: "comment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supply_items_condition_id",
                table: "supply_items",
                column: "condition_id");

            migrationBuilder.CreateIndex(
                name: "IX_supply_items_order_supply_id",
                table: "supply_items",
                column: "order_supply_id");

            migrationBuilder.CreateIndex(
                name: "IX_supply_items_order_supply_id_supply_type_id",
                table: "supply_items",
                columns: new[] { "order_supply_id", "supply_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supply_items_supply_type_id",
                table: "supply_items",
                column: "supply_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_workplaces_user_id_workplace_id",
                table: "user_workplaces",
                columns: new[] { "user_id", "workplace_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_workplaces_workplace_id",
                table: "user_workplaces",
                column: "workplace_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_legacy_id",
                table: "users",
                column: "legacy_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_workplace_transitions_from_workplace_id_to_workplace_id",
                table: "workplace_transitions",
                columns: new[] { "from_workplace_id", "to_workplace_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workplace_transitions_to_workplace_id",
                table: "workplace_transitions",
                column: "to_workplace_id");

            migrationBuilder.CreateIndex(
                name: "IX_workplaces_legacy_id",
                table: "workplaces",
                column: "legacy_id");

            migrationBuilder.CreateIndex(
                name: "IX_workplaces_previous_workplace_id",
                table: "workplaces",
                column: "previous_workplace_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operation_logs");

            migrationBuilder.DropTable(
                name: "order_blocks");

            migrationBuilder.DropTable(
                name: "order_footprints");

            migrationBuilder.DropTable(
                name: "supply_items");

            migrationBuilder.DropTable(
                name: "user_workplaces");

            migrationBuilder.DropTable(
                name: "workplace_transitions");

            migrationBuilder.DropTable(
                name: "production_orders");

            migrationBuilder.DropTable(
                name: "comments");

            migrationBuilder.DropTable(
                name: "order_supply");

            migrationBuilder.DropTable(
                name: "supply_conditions");

            migrationBuilder.DropTable(
                name: "supply_types");

            migrationBuilder.DropTable(
                name: "workplaces");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
