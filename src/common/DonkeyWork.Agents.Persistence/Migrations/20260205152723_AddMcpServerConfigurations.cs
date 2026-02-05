using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMcpServerConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "server_configurations",
                schema: "mcp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    transport_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_configurations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "http_configurations",
                schema: "mcp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    mcp_server_configuration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    transport_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    auth_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_http_configurations", x => x.id);
                    table.ForeignKey(
                        name: "FK_http_configurations_server_configurations_mcp_server_config~",
                        column: x => x.mcp_server_configuration_id,
                        principalSchema: "mcp",
                        principalTable: "server_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stdio_configurations",
                schema: "mcp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    mcp_server_configuration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    command = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    arguments = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    environment_variables = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    pre_exec_scripts = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    working_directory = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stdio_configurations", x => x.id);
                    table.ForeignKey(
                        name: "FK_stdio_configurations_server_configurations_mcp_server_confi~",
                        column: x => x.mcp_server_configuration_id,
                        principalSchema: "mcp",
                        principalTable: "server_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "http_header_configurations",
                schema: "mcp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    mcp_http_configuration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    header_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    header_value_encrypted = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_http_header_configurations", x => x.id);
                    table.ForeignKey(
                        name: "FK_http_header_configurations_http_configurations_mcp_http_con~",
                        column: x => x.mcp_http_configuration_id,
                        principalSchema: "mcp",
                        principalTable: "http_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "http_oauth_configurations",
                schema: "mcp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    mcp_http_configuration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    client_secret_encrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    redirect_uri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    scopes = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    authorization_endpoint = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    token_endpoint = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_http_oauth_configurations", x => x.id);
                    table.ForeignKey(
                        name: "FK_http_oauth_configurations_http_configurations_mcp_http_conf~",
                        column: x => x.mcp_http_configuration_id,
                        principalSchema: "mcp",
                        principalTable: "http_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mcp_http_configurations_mcp_server_configuration_id",
                schema: "mcp",
                table: "http_configurations",
                column: "mcp_server_configuration_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mcp_http_header_configurations_config_id_header_name",
                schema: "mcp",
                table: "http_header_configurations",
                columns: new[] { "mcp_http_configuration_id", "header_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mcp_http_header_configurations_mcp_http_configuration_id",
                schema: "mcp",
                table: "http_header_configurations",
                column: "mcp_http_configuration_id");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_http_oauth_configurations_mcp_http_configuration_id",
                schema: "mcp",
                table: "http_oauth_configurations",
                column: "mcp_http_configuration_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mcp_server_configurations_user_id",
                schema: "mcp",
                table: "server_configurations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_server_configurations_user_id_is_enabled",
                schema: "mcp",
                table: "server_configurations",
                columns: new[] { "user_id", "is_enabled" });

            migrationBuilder.CreateIndex(
                name: "ix_mcp_server_configurations_user_id_name",
                schema: "mcp",
                table: "server_configurations",
                columns: new[] { "user_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mcp_stdio_configurations_mcp_server_configuration_id",
                schema: "mcp",
                table: "stdio_configurations",
                column: "mcp_server_configuration_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "http_header_configurations",
                schema: "mcp");

            migrationBuilder.DropTable(
                name: "http_oauth_configurations",
                schema: "mcp");

            migrationBuilder.DropTable(
                name: "stdio_configurations",
                schema: "mcp");

            migrationBuilder.DropTable(
                name: "http_configurations",
                schema: "mcp");

            migrationBuilder.DropTable(
                name: "server_configurations",
                schema: "mcp");
        }
    }
}
