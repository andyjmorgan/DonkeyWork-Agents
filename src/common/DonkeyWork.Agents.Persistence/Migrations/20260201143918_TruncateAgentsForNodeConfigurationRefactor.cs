using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TruncateAgentsForNodeConfigurationRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Body",
                schema: "projects",
                table: "projects",
                type: "text",
                nullable: true);

            // Truncate all agent-related tables for the Unified Node Configuration Architecture refactor.
            // This is a clean break - all existing agents and executions will be deleted.
            migrationBuilder.Sql(@"
                TRUNCATE TABLE agents.agent_node_executions CASCADE;
                TRUNCATE TABLE agents.agent_executions CASCADE;
                TRUNCATE TABLE agents.agent_version_credential_mappings CASCADE;
                TRUNCATE TABLE agents.agent_versions CASCADE;
                TRUNCATE TABLE agents.agents CASCADE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Body",
                schema: "projects",
                table: "projects");
        }
    }
}
