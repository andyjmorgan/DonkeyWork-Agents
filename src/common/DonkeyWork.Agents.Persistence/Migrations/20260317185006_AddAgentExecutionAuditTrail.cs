using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentExecutionAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_executions",
                schema: "actors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    grain_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    parent_grain_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    contract_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    input = table.Column<string>(type: "text", nullable: true),
                    output = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    input_tokens_used = table.Column<int>(type: "integer", nullable: true),
                    output_tokens_used = table.Column<int>(type: "integer", nullable: true),
                    model_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_executions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_executions_conversation_id",
                schema: "actors",
                table: "agent_executions",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_executions_conversation_id_started_at",
                schema: "actors",
                table: "agent_executions",
                columns: new[] { "conversation_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_executions_grain_key",
                schema: "actors",
                table: "agent_executions",
                column: "grain_key");

            migrationBuilder.CreateIndex(
                name: "ix_agent_executions_started_at",
                schema: "actors",
                table: "agent_executions",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ix_agent_executions_status",
                schema: "actors",
                table: "agent_executions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_agent_executions_user_id",
                schema: "actors",
                table: "agent_executions",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_executions",
                schema: "actors");
        }
    }
}
