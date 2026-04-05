using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMcpTracesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "traces",
                schema: "mcp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    method = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    jsonrpc_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    request_body = table.Column<string>(type: "jsonb", nullable: false),
                    response_body = table.Column<string>(type: "jsonb", nullable: true),
                    http_status_code = table.Column<int>(type: "integer", nullable: false),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    client_ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_traces", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mcp_traces_method",
                schema: "mcp",
                table: "traces",
                column: "method");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_traces_method_started_at",
                schema: "mcp",
                table: "traces",
                columns: new[] { "method", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_mcp_traces_started_at",
                schema: "mcp",
                table: "traces",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_traces_user_id",
                schema: "mcp",
                table: "traces",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_traces_user_id_started_at",
                schema: "mcp",
                table: "traces",
                columns: new[] { "user_id", "started_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "traces",
                schema: "mcp");
        }
    }
}
