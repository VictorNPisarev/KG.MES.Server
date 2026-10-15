using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KG.MES.Server.Migrations
{
	/// <inheritdoc />
	public partial class AddLicensesAndDevices : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropIndex(
				name: "idx_user_devices_activation_key",
				table: "user_devices");

			migrationBuilder.DropIndex(
				name: "idx_user_devices_active",
				table: "user_devices");

			migrationBuilder.DropIndex(
				name: "idx_user_devices_last_used",
				table: "user_devices");

			migrationBuilder.DropIndex(
				name: "idx_user_devices_user_device_active",
				table: "user_devices");

			migrationBuilder.DropIndex(
				name: "idx_user_devices_user_id",
				table: "user_devices");

			migrationBuilder.DropColumn(
				name: "activation_key",
				table: "user_devices");

			migrationBuilder.DropColumn(
				name: "device_name",
				table: "user_devices");

			migrationBuilder.DropColumn(
				name: "is_active",
				table: "user_devices");

			migrationBuilder.DropColumn(
				name: "is_primary",
				table: "user_devices");

			migrationBuilder.DropColumn(
				name: "last_ip",
				table: "user_devices");

			migrationBuilder.DropColumn(
				name: "notes",
				table: "user_devices");

			migrationBuilder.DropColumn(
				name: "revoked_at",
				table: "user_devices");

			migrationBuilder.DropColumn(
				name: "registered_at",
				table: "user_devices");

			migrationBuilder.DropColumn(
				name: "updated_at",
				table: "user_devices");

			migrationBuilder.DropColumn(
				name: "device_id",
				table: "user_devices");

			migrationBuilder.AddColumn<Guid>(
				name: "device_id",
				table: "user_devices",
				type: "uuid",
				nullable: false);

			migrationBuilder.DropColumn(
				name: "is_device_check_enabled",
				table: "users");

			migrationBuilder.CreateTable(
				name: "licenses",
				columns: table => new
				{
					id = table.Column<Guid>(type: "uuid", nullable: false),
					key_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
					is_active = table.Column<bool>(type: "boolean", nullable: false),
					created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
					expires_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
					revoked_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
					notes = table.Column<string>(type: "text", nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_licenses", x => x.id);
				});

			migrationBuilder.CreateTable(
				name: "devices",
				columns: table => new
				{
					id = table.Column<Guid>(type: "uuid", nullable: false),
					device_hardware_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
					device_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
					license_id = table.Column<Guid>(type: "uuid", nullable: false),
					registered_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
					last_used_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
					last_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_devices", x => x.id);
					table.ForeignKey(
						name: "FK_devices_licenses_license_id",
						column: x => x.license_id,
						principalTable: "licenses",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_user_devices_user_id_device_id",
				table: "user_devices",
				columns: new[] { "user_id", "device_id" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_devices_device_hardware_id",
				table: "devices",
				column: "device_hardware_id",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_devices_license_id",
				table: "devices",
				column: "license_id",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_licenses_key_code",
				table: "licenses",
				column: "key_code",
				unique: true);

			migrationBuilder.AddForeignKey(
				name: "FK_user_devices_devices_device_id",
				table: "user_devices",
				column: "device_id",
				principalTable: "devices",
				principalColumn: "id",
				onDelete: ReferentialAction.Cascade);

		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<bool>(
				name: "is_device_check_enabled",
				table: "users",
				type: "boolean",
				nullable: false,
				defaultValue: false);

			migrationBuilder.DropForeignKey(
				name: "FK_user_devices_devices_device_id",
				table: "user_devices");

			migrationBuilder.DropTable(
				name: "devices");

			migrationBuilder.DropTable(
				name: "licenses");

			migrationBuilder.DropIndex(
				name: "IX_user_devices_user_id_device_id",
				table: "user_devices");

			migrationBuilder.DropColumn(
				name: "is_device_check_enabled",
				table: "users");

			migrationBuilder.RenameColumn(
				name: "created_at",
				table: "user_devices",
				newName: "registered_at");

			migrationBuilder.RenameIndex(
				name: "IX_user_devices_device_id",
				table: "user_devices",
				newName: "idx_user_devices_device_id");

			migrationBuilder.AlterColumn<string>(
				name: "device_id",
				table: "user_devices",
				type: "text",
				nullable: false,
				oldClrType: typeof(Guid),
				oldType: "uuid");

			migrationBuilder.AddColumn<string>(
				name: "activation_key",
				table: "user_devices",
				type: "text",
				nullable: true);

			migrationBuilder.AddColumn<string>(
				name: "device_name",
				table: "user_devices",
				type: "text",
				nullable: true);

			migrationBuilder.AddColumn<bool>(
				name: "is_active",
				table: "user_devices",
				type: "boolean",
				nullable: false,
				defaultValue: false);

			migrationBuilder.AddColumn<bool>(
				name: "is_primary",
				table: "user_devices",
				type: "boolean",
				nullable: false,
				defaultValue: false);

			migrationBuilder.AddColumn<string>(
				name: "last_ip",
				table: "user_devices",
				type: "text",
				nullable: true);

			migrationBuilder.AddColumn<string>(
				name: "notes",
				table: "user_devices",
				type: "text",
				nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				name: "revoked_at",
				table: "user_devices",
				type: "timestamp without time zone",
				nullable: true);

			migrationBuilder.CreateIndex(
				name: "idx_user_devices_activation_key",
				table: "user_devices",
				column: "activation_key",
				filter: "\"activation_key\" IS NOT NULL");

			migrationBuilder.CreateIndex(
				name: "idx_user_devices_active",
				table: "user_devices",
				column: "is_active",
				filter: "\"is_active\" = true");

			migrationBuilder.CreateIndex(
				name: "idx_user_devices_last_used",
				table: "user_devices",
				column: "last_used_at",
				filter: "\"last_used_at\" IS NOT NULL");

			migrationBuilder.CreateIndex(
				name: "idx_user_devices_user_device_active",
				table: "user_devices",
				columns: new[] { "user_id", "device_id" },
				unique: true,
				filter: "\"is_active\" = true");

			migrationBuilder.CreateIndex(
				name: "idx_user_devices_user_id",
				table: "user_devices",
				column: "user_id");
		}
	}
}
