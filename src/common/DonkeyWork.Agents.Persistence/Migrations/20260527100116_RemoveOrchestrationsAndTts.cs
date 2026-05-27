using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrchestrationsAndTts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_conversations_orchestrations_orchestration_id",
                schema: "conversations",
                table: "conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_orchestrations_orchestration_versions_current_version_id",
                schema: "orchestrations",
                table: "orchestrations");

            migrationBuilder.DropTable(
                name: "orchestration_execution_logs",
                schema: "orchestrations");

            migrationBuilder.DropTable(
                name: "orchestration_node_executions",
                schema: "orchestrations");

            migrationBuilder.DropTable(
                name: "orchestration_version_credential_mappings",
                schema: "orchestrations");

            migrationBuilder.DropTable(
                name: "playback",
                schema: "tts");

            migrationBuilder.DropTable(
                name: "orchestration_executions",
                schema: "orchestrations");

            migrationBuilder.DropTable(
                name: "recordings",
                schema: "tts");

            migrationBuilder.DropTable(
                name: "collections",
                schema: "tts");

            migrationBuilder.DropTable(
                name: "orchestration_versions",
                schema: "orchestrations");

            migrationBuilder.DropTable(
                name: "orchestrations",
                schema: "orchestrations");

            migrationBuilder.DropIndex(
                name: "ix_conversations_orchestration_id",
                schema: "conversations",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "target_orchestration_id",
                schema: "scheduling",
                table: "scheduled_jobs");

            migrationBuilder.DropColumn(
                name: "orchestration_id",
                schema: "conversations",
                table: "conversations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tts");

            migrationBuilder.EnsureSchema(
                name: "orchestrations");

            migrationBuilder.AddColumn<Guid>(
                name: "target_orchestration_id",
                schema: "scheduling",
                table: "scheduled_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "orchestration_id",
                schema: "conversations",
                table: "conversations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "collections",
                schema: "tts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cover_image_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    default_model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    default_voice = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recordings",
                schema: "tts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    collection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chapter_title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    orchestration_execution_id = table.Column<Guid>(type: "uuid", nullable: true),
                    progress = table.Column<double>(type: "double precision", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    transcript = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voice = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recordings", x => x.id);
                    table.ForeignKey(
                        name: "FK_recordings_collections_collection_id",
                        column: x => x.collection_id,
                        principalSchema: "tts",
                        principalTable: "collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "playback",
                schema: "tts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    recording_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_seconds = table.Column<double>(type: "double precision", nullable: false),
                    playback_speed = table.Column<double>(type: "double precision", nullable: false, defaultValue: 1.0),
                    position_seconds = table.Column<double>(type: "double precision", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "orchestration_execution_logs",
                schema: "orchestrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    log_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    node_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orchestration_execution_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orchestration_executions",
                schema: "orchestrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    orchestration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    orchestration_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    input = table.Column<string>(type: "jsonb", nullable: false),
                    @interface = table.Column<string>(name: "interface", type: "character varying(50)", maxLength: 50, nullable: false),
                    output = table.Column<string>(type: "jsonb", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    stream_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    total_tokens_used = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orchestration_executions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orchestration_node_executions",
                schema: "orchestrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    orchestration_execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    full_response = table.Column<string>(type: "text", nullable: true),
                    input = table.Column<string>(type: "jsonb", nullable: true),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    node_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    output = table.Column<string>(type: "jsonb", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tokens_used = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orchestration_node_executions", x => x.id);
                    table.ForeignKey(
                        name: "FK_orchestration_node_executions_orchestration_executions_orch~",
                        column: x => x.orchestration_execution_id,
                        principalSchema: "orchestrations",
                        principalTable: "orchestration_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orchestration_version_credential_mappings",
                schema: "orchestrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    orchestration_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orchestration_version_credential_mappings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orchestration_versions",
                schema: "orchestrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    orchestration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    direct_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    execution_timeout_seconds = table.Column<int>(type: "integer", nullable: true),
                    input_schema = table.Column<string>(type: "jsonb", nullable: false),
                    is_draft = table.Column<bool>(type: "boolean", nullable: false),
                    mcp_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    navi_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    node_configurations = table.Column<string>(type: "jsonb", nullable: false),
                    output_schema = table.Column<string>(type: "jsonb", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    react_flow_data = table.Column<string>(type: "jsonb", nullable: false),
                    tool_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orchestration_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orchestrations",
                schema: "orchestrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    current_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    friendly_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orchestrations", x => x.id);
                    table.ForeignKey(
                        name: "FK_orchestrations_orchestration_versions_current_version_id",
                        column: x => x.current_version_id,
                        principalSchema: "orchestrations",
                        principalTable: "orchestration_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_conversations_orchestration_id",
                schema: "conversations",
                table: "conversations",
                column: "orchestration_id");

            migrationBuilder.CreateIndex(
                name: "IX_collections_user_id",
                schema: "tts",
                table: "collections",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_execution_logs_created_at",
                schema: "orchestrations",
                table: "orchestration_execution_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_execution_logs_execution_id",
                schema: "orchestrations",
                table: "orchestration_execution_logs",
                column: "execution_id");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_execution_logs_execution_id_created_at",
                schema: "orchestrations",
                table: "orchestration_execution_logs",
                columns: new[] { "execution_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_execution_logs_log_level",
                schema: "orchestrations",
                table: "orchestration_execution_logs",
                column: "log_level");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_executions_interface",
                schema: "orchestrations",
                table: "orchestration_executions",
                column: "interface");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_executions_orchestration_id",
                schema: "orchestrations",
                table: "orchestration_executions",
                column: "orchestration_id");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_executions_orchestration_id_started_at",
                schema: "orchestrations",
                table: "orchestration_executions",
                columns: new[] { "orchestration_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_executions_orchestration_version_id",
                schema: "orchestrations",
                table: "orchestration_executions",
                column: "orchestration_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_executions_started_at",
                schema: "orchestrations",
                table: "orchestration_executions",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_executions_status",
                schema: "orchestrations",
                table: "orchestration_executions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_executions_user_id",
                schema: "orchestrations",
                table: "orchestration_executions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_node_executions_node_type",
                schema: "orchestrations",
                table: "orchestration_node_executions",
                column: "node_type");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_node_executions_orchestration_execution_id",
                schema: "orchestrations",
                table: "orchestration_node_executions",
                column: "orchestration_execution_id");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_node_executions_started_at",
                schema: "orchestrations",
                table: "orchestration_node_executions",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_node_executions_status",
                schema: "orchestrations",
                table: "orchestration_node_executions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_node_executions_user_id",
                schema: "orchestrations",
                table: "orchestration_node_executions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_version_credential_mappings_credential_id",
                schema: "orchestrations",
                table: "orchestration_version_credential_mappings",
                column: "credential_id");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_version_credential_mappings_orchestration_version_id",
                schema: "orchestrations",
                table: "orchestration_version_credential_mappings",
                column: "orchestration_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_version_credential_mappings_user_id",
                schema: "orchestrations",
                table: "orchestration_version_credential_mappings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_version_credential_mappings_version_node",
                schema: "orchestrations",
                table: "orchestration_version_credential_mappings",
                columns: new[] { "orchestration_version_id", "node_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_versions_created_at",
                schema: "orchestrations",
                table: "orchestration_versions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_versions_orchestration_id",
                schema: "orchestrations",
                table: "orchestration_versions",
                column: "orchestration_id");

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_versions_orchestration_id_is_draft",
                schema: "orchestrations",
                table: "orchestration_versions",
                columns: new[] { "orchestration_id", "is_draft" });

            migrationBuilder.CreateIndex(
                name: "ix_orchestration_versions_user_id",
                schema: "orchestrations",
                table: "orchestration_versions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_orchestrations_created_at",
                schema: "orchestrations",
                table: "orchestrations",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_orchestrations_current_version_id",
                schema: "orchestrations",
                table: "orchestrations",
                column: "current_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_orchestrations_name",
                schema: "orchestrations",
                table: "orchestrations",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_orchestrations_user_id",
                schema: "orchestrations",
                table: "orchestrations",
                column: "user_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_recordings_collection_id_sequence_number",
                schema: "tts",
                table: "recordings",
                columns: new[] { "collection_id", "sequence_number" });

            migrationBuilder.CreateIndex(
                name: "IX_recordings_orchestration_execution_id",
                schema: "tts",
                table: "recordings",
                column: "orchestration_execution_id");

            migrationBuilder.CreateIndex(
                name: "IX_recordings_user_id",
                schema: "tts",
                table: "recordings",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_conversations_orchestrations_orchestration_id",
                schema: "conversations",
                table: "conversations",
                column: "orchestration_id",
                principalSchema: "orchestrations",
                principalTable: "orchestrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_orchestration_execution_logs_orchestration_executions_execu~",
                schema: "orchestrations",
                table: "orchestration_execution_logs",
                column: "execution_id",
                principalSchema: "orchestrations",
                principalTable: "orchestration_executions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_orchestration_executions_orchestration_versions_orchestrati~",
                schema: "orchestrations",
                table: "orchestration_executions",
                column: "orchestration_version_id",
                principalSchema: "orchestrations",
                principalTable: "orchestration_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_orchestration_executions_orchestrations_orchestration_id",
                schema: "orchestrations",
                table: "orchestration_executions",
                column: "orchestration_id",
                principalSchema: "orchestrations",
                principalTable: "orchestrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_orchestration_version_credential_mappings_orchestration_ver~",
                schema: "orchestrations",
                table: "orchestration_version_credential_mappings",
                column: "orchestration_version_id",
                principalSchema: "orchestrations",
                principalTable: "orchestration_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_orchestration_versions_orchestrations_orchestration_id",
                schema: "orchestrations",
                table: "orchestration_versions",
                column: "orchestration_id",
                principalSchema: "orchestrations",
                principalTable: "orchestrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
