using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTurnIdToGrainMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "turn_id",
                schema: "actors",
                table: "grain_messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "turn_id",
                schema: "actors",
                table: "agent_executions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_grain_messages_grain_key_turn_id",
                schema: "actors",
                table: "grain_messages",
                columns: new[] { "grain_key", "turn_id" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_executions_conversation_id_turn_id",
                schema: "actors",
                table: "agent_executions",
                columns: new[] { "conversation_id", "turn_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_grain_messages_grain_key_turn_id",
                schema: "actors",
                table: "grain_messages");

            migrationBuilder.DropIndex(
                name: "ix_agent_executions_conversation_id_turn_id",
                schema: "actors",
                table: "agent_executions");

            migrationBuilder.DropColumn(
                name: "turn_id",
                schema: "actors",
                table: "grain_messages");

            migrationBuilder.DropColumn(
                name: "turn_id",
                schema: "actors",
                table: "agent_executions");
        }
    }
}
