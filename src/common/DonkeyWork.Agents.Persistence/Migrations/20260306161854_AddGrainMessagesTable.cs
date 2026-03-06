using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGrainMessagesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "actors");

            migrationBuilder.CreateTable(
                name: "grain_messages",
                schema: "actors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    grain_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "jsonb", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grain_messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_grain_messages_grain_key_sequence_number",
                schema: "actors",
                table: "grain_messages",
                columns: new[] { "grain_key", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_grain_messages_user_id",
                schema: "actors",
                table: "grain_messages",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "grain_messages",
                schema: "actors");
        }
    }
}
