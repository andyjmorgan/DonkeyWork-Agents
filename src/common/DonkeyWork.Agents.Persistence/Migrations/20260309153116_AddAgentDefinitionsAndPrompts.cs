using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentDefinitionsAndPrompts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "agent_definitions");

            migrationBuilder.EnsureSchema(
                name: "prompts");

            migrationBuilder.CreateTable(
                name: "agent_definitions",
                schema: "agent_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    contract = table.Column<string>(type: "jsonb", nullable: false),
                    react_flow_data = table.Column<string>(type: "jsonb", nullable: true),
                    node_configurations = table.Column<string>(type: "jsonb", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prompts",
                schema: "prompts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    prompt_type = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_definitions_created_at",
                schema: "agent_definitions",
                table: "agent_definitions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_agent_definitions_user_id",
                schema: "agent_definitions",
                table: "agent_definitions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_prompts_created_at",
                schema: "prompts",
                table: "prompts",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_prompts_prompt_type",
                schema: "prompts",
                table: "prompts",
                column: "prompt_type");

            migrationBuilder.CreateIndex(
                name: "ix_prompts_user_id",
                schema: "prompts",
                table: "prompts",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_definitions",
                schema: "agent_definitions");

            migrationBuilder.DropTable(
                name: "prompts",
                schema: "prompts");
        }
    }
}
