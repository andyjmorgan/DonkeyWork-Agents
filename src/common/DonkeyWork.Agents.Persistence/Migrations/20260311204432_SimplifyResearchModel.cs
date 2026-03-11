using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyResearchModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "completion_notes",
                schema: "research",
                table: "research");

            migrationBuilder.RenameColumn(
                name: "summary",
                schema: "research",
                table: "research",
                newName: "result");

            migrationBuilder.RenameColumn(
                name: "subject",
                schema: "research",
                table: "research",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "content",
                schema: "research",
                table: "research",
                newName: "plan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "title",
                schema: "research",
                table: "research",
                newName: "subject");

            migrationBuilder.RenameColumn(
                name: "result",
                schema: "research",
                table: "research",
                newName: "summary");

            migrationBuilder.RenameColumn(
                name: "plan",
                schema: "research",
                table: "research",
                newName: "content");

            migrationBuilder.AddColumn<string>(
                name: "completion_notes",
                schema: "research",
                table: "research",
                type: "text",
                nullable: true);
        }
    }
}
