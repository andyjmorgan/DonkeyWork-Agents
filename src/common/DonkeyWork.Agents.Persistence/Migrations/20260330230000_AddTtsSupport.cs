using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTtsSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tts");

            migrationBuilder.CreateTable(
                name: "recordings",
                schema: "tts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    transcript = table.Column<string>(type: "text", nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    voice = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    orchestration_execution_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recordings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "playback",
                schema: "tts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recording_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_seconds = table.Column<double>(type: "double precision", nullable: false),
                    duration_seconds = table.Column<double>(type: "double precision", nullable: false),
                    completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    playback_speed = table.Column<double>(type: "double precision", nullable: false, defaultValue: 1.0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playback", x => x.id);
                    table.ForeignKey(
                        name: "FK_playback_recordings_recording_id",
                        column: x => x.recording_id,
                        principalSchema: "tts",
                        principalTable: "recordings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recordings_user_id",
                schema: "tts",
                table: "recordings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_recordings_orchestration_execution_id",
                schema: "tts",
                table: "recordings",
                column: "orchestration_execution_id");

            migrationBuilder.CreateIndex(
                name: "IX_playback_recording_id",
                schema: "tts",
                table: "playback",
                column: "recording_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_playback_user_id_recording_id",
                schema: "tts",
                table: "playback",
                columns: new[] { "user_id", "recording_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "playback",
                schema: "tts");

            migrationBuilder.DropTable(
                name: "recordings",
                schema: "tts");
        }
    }
}
