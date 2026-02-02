using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameMilestoneDescriptionToContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "description",
                schema: "projects",
                table: "milestones",
                newName: "content");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "content",
                schema: "projects",
                table: "milestones",
                newName: "description");
        }
    }
}
