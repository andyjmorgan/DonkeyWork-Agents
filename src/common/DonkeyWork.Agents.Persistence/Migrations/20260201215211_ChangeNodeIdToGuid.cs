using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeNodeIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete all agent data first (cascades to versions, executions, mappings)
            migrationBuilder.Sql("DELETE FROM agents.agents;");

            // Now alter the columns (tables are empty so no conversion needed)
            migrationBuilder.Sql(
                "ALTER TABLE agents.agent_version_credential_mappings DROP COLUMN node_id;");
            migrationBuilder.Sql(
                "ALTER TABLE agents.agent_version_credential_mappings ADD COLUMN node_id uuid NOT NULL;");

            migrationBuilder.Sql(
                "ALTER TABLE agents.agent_node_executions DROP COLUMN node_id;");
            migrationBuilder.Sql(
                "ALTER TABLE agents.agent_node_executions ADD COLUMN node_id uuid NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "node_id",
                schema: "agents",
                table: "agent_version_credential_mappings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "node_id",
                schema: "agents",
                table: "agent_node_executions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
