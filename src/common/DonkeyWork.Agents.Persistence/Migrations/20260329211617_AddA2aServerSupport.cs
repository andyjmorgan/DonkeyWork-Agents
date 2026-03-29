using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddA2aServerSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "a2a");

            migrationBuilder.CreateTable(
                name: "server_configurations",
                schema: "a2a",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    address = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    connect_to_navi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_a2a_server_configurations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "header_configurations",
                schema: "a2a",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    a2a_server_configuration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    header_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    header_value_encrypted = table.Column<byte[]>(type: "bytea", nullable: true),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credential_field_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_header_configurations", x => x.id);
                    table.CheckConstraint("ck_a2a_header_value_or_credential", "(header_value_encrypted IS NOT NULL AND credential_id IS NULL AND credential_field_type IS NULL) OR (header_value_encrypted IS NULL AND credential_id IS NOT NULL AND credential_field_type IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_header_configurations_server_configurations_a2a_server_conf~",
                        column: x => x.a2a_server_configuration_id,
                        principalSchema: "a2a",
                        principalTable: "server_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_a2a_header_configurations_a2a_server_configuration_id",
                schema: "a2a",
                table: "header_configurations",
                column: "a2a_server_configuration_id");

            migrationBuilder.CreateIndex(
                name: "ix_a2a_header_configurations_config_id_header_name",
                schema: "a2a",
                table: "header_configurations",
                columns: new[] { "a2a_server_configuration_id", "header_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_a2a_server_configurations_user_id",
                schema: "a2a",
                table: "server_configurations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_a2a_server_configurations_user_id_is_enabled",
                schema: "a2a",
                table: "server_configurations",
                columns: new[] { "user_id", "is_enabled" });

            migrationBuilder.CreateIndex(
                name: "ix_a2a_server_configurations_user_id_name",
                schema: "a2a",
                table: "server_configurations",
                columns: new[] { "user_id", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "header_configurations",
                schema: "a2a");

            migrationBuilder.DropTable(
                name: "server_configurations",
                schema: "a2a");
        }
    }
}
