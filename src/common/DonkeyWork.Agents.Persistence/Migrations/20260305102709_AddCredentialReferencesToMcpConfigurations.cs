using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCredentialReferencesToMcpConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the new environment variables table first (before dropping the JSON column)
            migrationBuilder.CreateTable(
                name: "stdio_environment_variables",
                schema: "mcp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    mcp_stdio_configuration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credential_field_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stdio_environment_variables", x => x.id);
                    table.CheckConstraint("ck_stdio_env_var_value_or_credential", "(value IS NOT NULL AND credential_id IS NULL AND credential_field_type IS NULL) OR (value IS NULL AND credential_id IS NOT NULL AND credential_field_type IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_stdio_environment_variables_stdio_configurations_mcp_stdio_~",
                        column: x => x.mcp_stdio_configuration_id,
                        principalSchema: "mcp",
                        principalTable: "stdio_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mcp_stdio_env_vars_config_id_name",
                schema: "mcp",
                table: "stdio_environment_variables",
                columns: new[] { "mcp_stdio_configuration_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mcp_stdio_env_vars_mcp_stdio_configuration_id",
                schema: "mcp",
                table: "stdio_environment_variables",
                column: "mcp_stdio_configuration_id");

            // Migrate existing JSON environment variables to the new table
            migrationBuilder.Sql("""
                INSERT INTO mcp.stdio_environment_variables (id, mcp_stdio_configuration_id, name, value)
                SELECT gen_random_uuid(), sc.id, kv.key, kv.value
                FROM mcp.stdio_configurations sc,
                LATERAL jsonb_each_text(sc.environment_variables::jsonb) kv
                WHERE sc.environment_variables IS NOT NULL
                  AND sc.environment_variables != '{}'
                  AND sc.environment_variables != 'null';
                """);

            // Now safe to drop the JSON column
            migrationBuilder.DropColumn(
                name: "environment_variables",
                schema: "mcp",
                table: "stdio_configurations");

            // Make header_value_encrypted nullable (for credential references)
            migrationBuilder.AlterColumn<byte[]>(
                name: "header_value_encrypted",
                schema: "mcp",
                table: "http_header_configurations",
                type: "bytea",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "bytea");

            migrationBuilder.AddColumn<string>(
                name: "credential_field_type",
                schema: "mcp",
                table: "http_header_configurations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "credential_id",
                schema: "mcp",
                table: "http_header_configurations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_http_header_value_or_credential",
                schema: "mcp",
                table: "http_header_configurations",
                sql: "(header_value_encrypted IS NOT NULL AND credential_id IS NULL AND credential_field_type IS NULL) OR (header_value_encrypted IS NULL AND credential_id IS NOT NULL AND credential_field_type IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stdio_environment_variables",
                schema: "mcp");

            migrationBuilder.DropCheckConstraint(
                name: "ck_http_header_value_or_credential",
                schema: "mcp",
                table: "http_header_configurations");

            migrationBuilder.DropColumn(
                name: "credential_field_type",
                schema: "mcp",
                table: "http_header_configurations");

            migrationBuilder.DropColumn(
                name: "credential_id",
                schema: "mcp",
                table: "http_header_configurations");

            migrationBuilder.AddColumn<string>(
                name: "environment_variables",
                schema: "mcp",
                table: "stdio_configurations",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AlterColumn<byte[]>(
                name: "header_value_encrypted",
                schema: "mcp",
                table: "http_header_configurations",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true);
        }
    }
}
