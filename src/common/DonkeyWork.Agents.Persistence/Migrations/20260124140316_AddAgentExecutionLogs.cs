using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentExecutionLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_execution_logs",
                schema: "agents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    log_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    node_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_execution_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_agent_execution_logs_agent_executions_execution_id",
                        column: x => x.execution_id,
                        principalSchema: "agents",
                        principalTable: "agent_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_execution_logs_created_at",
                schema: "agents",
                table: "agent_execution_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_agent_execution_logs_execution_id",
                schema: "agents",
                table: "agent_execution_logs",
                column: "execution_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_execution_logs_execution_id_created_at",
                schema: "agents",
                table: "agent_execution_logs",
                columns: new[] { "execution_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_execution_logs_log_level",
                schema: "agents",
                table: "agent_execution_logs",
                column: "log_level");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_execution_logs",
                schema: "agents");
        }
    }
}
