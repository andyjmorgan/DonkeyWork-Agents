using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingProductTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "scheduling");

            migrationBuilder.CreateTable(
                name: "scheduled_jobs",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    job_type = table.Column<string>(type: "text", nullable: false),
                    schedule_mode = table.Column<string>(type: "text", nullable: false),
                    cron_expression = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    run_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Europe/Dublin"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    target_type = table.Column<string>(type: "text", nullable: false),
                    target_agent_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_orchestration_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quartz_job_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quartz_trigger_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    creator_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    creator_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    creator_username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_job_executions",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    scheduled_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quartz_fire_instance_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    trigger_source = table.Column<string>(type: "text", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    error_details = table.Column<string>(type: "text", nullable: true),
                    output_summary = table.Column<string>(type: "text", nullable: true),
                    executing_node_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_job_executions", x => x.id);
                    table.ForeignKey(
                        name: "FK_scheduled_job_executions_scheduled_jobs_scheduled_job_id",
                        column: x => x.scheduled_job_id,
                        principalSchema: "scheduling",
                        principalTable: "scheduled_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_job_payloads",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    scheduled_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_prompt = table.Column<string>(type: "text", nullable: false),
                    input_context = table.Column<string>(type: "jsonb", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_job_payloads", x => x.id);
                    table.ForeignKey(
                        name: "FK_scheduled_job_payloads_scheduled_jobs_scheduled_job_id",
                        column: x => x.scheduled_job_id,
                        principalSchema: "scheduling",
                        principalTable: "scheduled_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_job_executions_correlation_id",
                schema: "scheduling",
                table: "scheduled_job_executions",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_job_executions_job_started",
                schema: "scheduling",
                table: "scheduled_job_executions",
                columns: new[] { "scheduled_job_id", "started_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_job_executions_status",
                schema: "scheduling",
                table: "scheduled_job_executions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_job_payloads_scheduled_job_id",
                schema: "scheduling",
                table: "scheduled_job_payloads",
                column: "scheduled_job_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_jobs_is_enabled",
                schema: "scheduling",
                table: "scheduled_jobs",
                column: "is_enabled");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_jobs_is_system",
                schema: "scheduling",
                table: "scheduled_jobs",
                column: "is_system");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_jobs_job_type",
                schema: "scheduling",
                table: "scheduled_jobs",
                column: "job_type");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_jobs_target_type",
                schema: "scheduling",
                table: "scheduled_jobs",
                column: "target_type");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_jobs_user_id",
                schema: "scheduling",
                table: "scheduled_jobs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduled_job_executions",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "scheduled_job_payloads",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "scheduled_jobs",
                schema: "scheduling");
        }
    }
}
