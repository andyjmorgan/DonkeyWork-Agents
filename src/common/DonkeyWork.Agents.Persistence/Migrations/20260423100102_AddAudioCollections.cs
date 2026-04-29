using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "chapter_title",
                schema: "tts",
                table: "recordings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "collection_id",
                schema: "tts",
                table: "recordings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_message",
                schema: "tts",
                table: "recordings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "progress",
                schema: "tts",
                table: "recordings",
                type: "double precision",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<int>(
                name: "sequence_number",
                schema: "tts",
                table: "recordings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "tts",
                table: "recordings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Ready");

            migrationBuilder.CreateTable(
                name: "collections",
                schema: "tts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    cover_image_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    default_voice = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    default_model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collections", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recordings_collection_id_sequence_number",
                schema: "tts",
                table: "recordings",
                columns: new[] { "collection_id", "sequence_number" });

            migrationBuilder.CreateIndex(
                name: "IX_collections_user_id",
                schema: "tts",
                table: "collections",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_recordings_collections_collection_id",
                schema: "tts",
                table: "recordings",
                column: "collection_id",
                principalSchema: "tts",
                principalTable: "collections",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_recordings_collections_collection_id",
                schema: "tts",
                table: "recordings");

            migrationBuilder.DropTable(
                name: "collections",
                schema: "tts");

            migrationBuilder.DropIndex(
                name: "IX_recordings_collection_id_sequence_number",
                schema: "tts",
                table: "recordings");

            migrationBuilder.DropColumn(
                name: "chapter_title",
                schema: "tts",
                table: "recordings");

            migrationBuilder.DropColumn(
                name: "collection_id",
                schema: "tts",
                table: "recordings");

            migrationBuilder.DropColumn(
                name: "error_message",
                schema: "tts",
                table: "recordings");

            migrationBuilder.DropColumn(
                name: "progress",
                schema: "tts",
                table: "recordings");

            migrationBuilder.DropColumn(
                name: "sequence_number",
                schema: "tts",
                table: "recordings");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "tts",
                table: "recordings");
        }
    }
}
