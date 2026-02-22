using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompletionNotesToProjectsAndMilestones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                schema: "projects",
                table: "projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "completion_notes",
                schema: "projects",
                table: "projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                schema: "projects",
                table: "milestones",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "completion_notes",
                schema: "projects",
                table: "milestones",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "completed_at",
                schema: "projects",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "completion_notes",
                schema: "projects",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "completed_at",
                schema: "projects",
                table: "milestones");

            migrationBuilder.DropColumn(
                name: "completion_notes",
                schema: "projects",
                table: "milestones");
        }
    }
}
