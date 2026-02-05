using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConversationContentParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Truncate all conversation messages and conversations due to content structure change
            // Messages stored with old format are incompatible with the new ContentPart array format
            migrationBuilder.Sql("TRUNCATE TABLE conversations.conversation_messages CASCADE;");
            migrationBuilder.Sql("TRUNCATE TABLE conversations.conversations CASCADE;");

            migrationBuilder.DropColumn(
                name: "input_tokens",
                schema: "conversations",
                table: "conversation_messages");

            migrationBuilder.DropColumn(
                name: "model",
                schema: "conversations",
                table: "conversation_messages");

            migrationBuilder.DropColumn(
                name: "output_tokens",
                schema: "conversations",
                table: "conversation_messages");

            migrationBuilder.DropColumn(
                name: "provider",
                schema: "conversations",
                table: "conversation_messages");

            migrationBuilder.DropColumn(
                name: "total_tokens",
                schema: "conversations",
                table: "conversation_messages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "input_tokens",
                schema: "conversations",
                table: "conversation_messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "model",
                schema: "conversations",
                table: "conversation_messages",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "output_tokens",
                schema: "conversations",
                table: "conversation_messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider",
                schema: "conversations",
                table: "conversation_messages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "total_tokens",
                schema: "conversations",
                table: "conversation_messages",
                type: "integer",
                nullable: true);
        }
    }
}
