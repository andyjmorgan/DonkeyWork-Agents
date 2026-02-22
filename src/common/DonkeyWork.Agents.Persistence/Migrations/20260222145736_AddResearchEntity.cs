using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddResearchEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "research");

            migrationBuilder.AddColumn<Guid>(
                name: "research_id",
                schema: "projects",
                table: "notes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "research",
                schema: "research",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    completion_notes = table.Column<string>(type: "text", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "research_tags",
                schema: "research",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    research_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_research_tags_research_research_id",
                        column: x => x.research_id,
                        principalSchema: "research",
                        principalTable: "research",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notes_research_id",
                schema: "projects",
                table: "notes",
                column: "research_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_created_at",
                schema: "research",
                table: "research",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_research_status",
                schema: "research",
                table: "research",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_research_user_id",
                schema: "research",
                table: "research",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_tags_name",
                schema: "research",
                table: "research_tags",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_research_tags_research_id",
                schema: "research",
                table: "research_tags",
                column: "research_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_tags_user_id",
                schema: "research",
                table: "research_tags",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_notes_research_research_id",
                schema: "projects",
                table: "notes",
                column: "research_id",
                principalSchema: "research",
                principalTable: "research",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notes_research_research_id",
                schema: "projects",
                table: "notes");

            migrationBuilder.DropTable(
                name: "research_tags",
                schema: "research");

            migrationBuilder.DropTable(
                name: "research",
                schema: "research");

            migrationBuilder.DropIndex(
                name: "ix_notes_research_id",
                schema: "projects",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "research_id",
                schema: "projects",
                table: "notes");
        }
    }
}
