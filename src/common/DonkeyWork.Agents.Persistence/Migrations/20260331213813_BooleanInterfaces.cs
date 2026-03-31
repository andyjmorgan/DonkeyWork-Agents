using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BooleanInterfaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "interface",
                schema: "orchestrations",
                table: "orchestration_versions");

            migrationBuilder.AddColumn<string>(
                name: "friendly_name",
                schema: "orchestrations",
                table: "orchestrations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "direct_enabled",
                schema: "orchestrations",
                table: "orchestration_versions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "mcp_enabled",
                schema: "orchestrations",
                table: "orchestration_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "navi_enabled",
                schema: "orchestrations",
                table: "orchestration_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "tool_enabled",
                schema: "orchestrations",
                table: "orchestration_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "friendly_name",
                schema: "orchestrations",
                table: "orchestrations");

            migrationBuilder.DropColumn(
                name: "direct_enabled",
                schema: "orchestrations",
                table: "orchestration_versions");

            migrationBuilder.DropColumn(
                name: "mcp_enabled",
                schema: "orchestrations",
                table: "orchestration_versions");

            migrationBuilder.DropColumn(
                name: "navi_enabled",
                schema: "orchestrations",
                table: "orchestration_versions");

            migrationBuilder.DropColumn(
                name: "tool_enabled",
                schema: "orchestrations",
                table: "orchestration_versions");

            migrationBuilder.AddColumn<string>(
                name: "interface",
                schema: "orchestrations",
                table: "orchestration_versions",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
        }
    }
}
