using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTaskItemSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "summary",
                schema: "projects",
                table: "tasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "summary",
                schema: "projects",
                table: "tasks",
                type: "text",
                nullable: true);
        }
    }
}
