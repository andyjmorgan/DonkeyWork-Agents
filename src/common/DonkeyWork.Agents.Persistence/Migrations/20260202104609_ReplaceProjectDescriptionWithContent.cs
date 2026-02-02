using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceProjectDescriptionWithContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Body",
                schema: "projects",
                table: "projects");

            migrationBuilder.RenameColumn(
                name: "description",
                schema: "projects",
                table: "projects",
                newName: "content");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "content",
                schema: "projects",
                table: "projects",
                newName: "description");

            migrationBuilder.AddColumn<string>(
                name: "Body",
                schema: "projects",
                table: "projects",
                type: "text",
                nullable: true);
        }
    }
}
