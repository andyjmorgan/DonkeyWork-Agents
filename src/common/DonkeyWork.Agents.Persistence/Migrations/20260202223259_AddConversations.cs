using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "conversations");

            migrationBuilder.CreateTable(
                name: "conversations",
                schema: "conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    orchestration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.id);
                    table.ForeignKey(
                        name: "FK_conversations_orchestrations_orchestration_id",
                        column: x => x.orchestration_id,
                        principalSchema: "orchestrations",
                        principalTable: "orchestrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conversation_messages",
                schema: "conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    input_tokens = table.Column<int>(type: "integer", nullable: true),
                    output_tokens = table.Column<int>(type: "integer", nullable: true),
                    total_tokens = table.Column<int>(type: "integer", nullable: true),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    model = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_conversation_messages_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalSchema: "conversations",
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_conversation_messages_conversation_id",
                schema: "conversations",
                table: "conversation_messages",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversation_messages_created_at",
                schema: "conversations",
                table: "conversation_messages",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_conversation_messages_user_id",
                schema: "conversations",
                table: "conversation_messages",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_created_at",
                schema: "conversations",
                table: "conversations",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_orchestration_id",
                schema: "conversations",
                table: "conversations",
                column: "orchestration_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_user_id",
                schema: "conversations",
                table: "conversations",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversation_messages",
                schema: "conversations");

            migrationBuilder.DropTable(
                name: "conversations",
                schema: "conversations");
        }
    }
}
