using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "agents");

            migrationBuilder.CreateTable(
                name: "agent_executions",
                schema: "agents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    input = table.Column<string>(type: "jsonb", nullable: false),
                    output = table.Column<string>(type: "jsonb", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    total_tokens_used = table.Column<int>(type: "integer", nullable: true),
                    stream_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_executions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agent_node_executions",
                schema: "agents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    agent_execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    node_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    input = table.Column<string>(type: "jsonb", nullable: true),
                    output = table.Column<string>(type: "jsonb", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tokens_used = table.Column<int>(type: "integer", nullable: true),
                    full_response = table.Column<string>(type: "text", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_node_executions", x => x.id);
                    table.ForeignKey(
                        name: "FK_agent_node_executions_agent_executions_agent_execution_id",
                        column: x => x.agent_execution_id,
                        principalSchema: "agents",
                        principalTable: "agent_executions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_version_credential_mappings",
                schema: "agents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    agent_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_version_credential_mappings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agent_versions",
                schema: "agents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    is_draft = table.Column<bool>(type: "boolean", nullable: false),
                    input_schema = table.Column<string>(type: "jsonb", nullable: false),
                    output_schema = table.Column<string>(type: "jsonb", nullable: true),
                    react_flow_data = table.Column<string>(type: "jsonb", nullable: false),
                    node_configurations = table.Column<string>(type: "jsonb", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agents",
                schema: "agents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    current_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agents", x => x.id);
                    table.ForeignKey(
                        name: "FK_agents_agent_versions_current_version_id",
                        column: x => x.current_version_id,
                        principalSchema: "agents",
                        principalTable: "agent_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_executions_agent_id",
                schema: "agents",
                table: "agent_executions",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_executions_agent_id_started_at",
                schema: "agents",
                table: "agent_executions",
                columns: new[] { "agent_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_executions_agent_version_id",
                schema: "agents",
                table: "agent_executions",
                column: "agent_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_executions_started_at",
                schema: "agents",
                table: "agent_executions",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ix_agent_executions_status",
                schema: "agents",
                table: "agent_executions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_agent_executions_user_id",
                schema: "agents",
                table: "agent_executions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_node_executions_agent_execution_id",
                schema: "agents",
                table: "agent_node_executions",
                column: "agent_execution_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_node_executions_node_type",
                schema: "agents",
                table: "agent_node_executions",
                column: "node_type");

            migrationBuilder.CreateIndex(
                name: "ix_agent_node_executions_started_at",
                schema: "agents",
                table: "agent_node_executions",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ix_agent_node_executions_status",
                schema: "agents",
                table: "agent_node_executions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_agent_node_executions_user_id",
                schema: "agents",
                table: "agent_node_executions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_version_credential_mappings_agent_version_id",
                schema: "agents",
                table: "agent_version_credential_mappings",
                column: "agent_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_version_credential_mappings_credential_id",
                schema: "agents",
                table: "agent_version_credential_mappings",
                column: "credential_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_version_credential_mappings_user_id",
                schema: "agents",
                table: "agent_version_credential_mappings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_version_credential_mappings_version_node",
                schema: "agents",
                table: "agent_version_credential_mappings",
                columns: new[] { "agent_version_id", "node_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_agent_versions_agent_id",
                schema: "agents",
                table: "agent_versions",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_versions_agent_id_is_draft",
                schema: "agents",
                table: "agent_versions",
                columns: new[] { "agent_id", "is_draft" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_versions_created_at",
                schema: "agents",
                table: "agent_versions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_agent_versions_user_id",
                schema: "agents",
                table: "agent_versions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_agents_created_at",
                schema: "agents",
                table: "agents",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_agents_current_version_id",
                schema: "agents",
                table: "agents",
                column: "current_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_agents_name",
                schema: "agents",
                table: "agents",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_agents_user_id",
                schema: "agents",
                table: "agents",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_agent_executions_agent_versions_agent_version_id",
                schema: "agents",
                table: "agent_executions",
                column: "agent_version_id",
                principalSchema: "agents",
                principalTable: "agent_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_agent_executions_agents_agent_id",
                schema: "agents",
                table: "agent_executions",
                column: "agent_id",
                principalSchema: "agents",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_agent_version_credential_mappings_agent_versions_agent_vers~",
                schema: "agents",
                table: "agent_version_credential_mappings",
                column: "agent_version_id",
                principalSchema: "agents",
                principalTable: "agent_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_agent_versions_agents_agent_id",
                schema: "agents",
                table: "agent_versions",
                column: "agent_id",
                principalSchema: "agents",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_agents_agent_versions_current_version_id",
                schema: "agents",
                table: "agents");

            migrationBuilder.DropTable(
                name: "agent_node_executions",
                schema: "agents");

            migrationBuilder.DropTable(
                name: "agent_version_credential_mappings",
                schema: "agents");

            migrationBuilder.DropTable(
                name: "agent_executions",
                schema: "agents");

            migrationBuilder.DropTable(
                name: "agent_versions",
                schema: "agents");

            migrationBuilder.DropTable(
                name: "agents",
                schema: "agents");
        }
    }
}
