using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGrainCompactionMarkers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "grain_compaction_markers",
                schema: "actors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    grain_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    at_sequence_number = table.Column<int>(type: "integer", nullable: false),
                    at_turn_id = table.Column<Guid>(type: "uuid", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grain_compaction_markers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_grain_compaction_markers_grain_key_at_sequence_number",
                schema: "actors",
                table: "grain_compaction_markers",
                columns: new[] { "grain_key", "at_sequence_number" });

            migrationBuilder.CreateIndex(
                name: "ix_grain_compaction_markers_user_id",
                schema: "actors",
                table: "grain_compaction_markers",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "grain_compaction_markers",
                schema: "actors");
        }
    }
}
