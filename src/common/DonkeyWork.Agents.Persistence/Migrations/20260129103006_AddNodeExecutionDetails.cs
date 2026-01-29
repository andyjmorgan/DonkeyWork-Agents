using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeExecutionDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActionType",
                schema: "agents",
                table: "agent_node_executions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NodeName",
                schema: "agents",
                table: "agent_node_executions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActionType",
                schema: "agents",
                table: "agent_node_executions");

            migrationBuilder.DropColumn(
                name: "NodeName",
                schema: "agents",
                table: "agent_node_executions");
        }
    }
}
