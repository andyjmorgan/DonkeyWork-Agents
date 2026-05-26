using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProjectManagementAndA2aMcpTunnel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_file_references",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "milestone_tags",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "note_tags",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "project_file_references",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "project_tags",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "research_tags",
                schema: "research");

            migrationBuilder.DropTable(
                name: "task_tags",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "notes",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "tasks",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "research",
                schema: "research");

            migrationBuilder.DropTable(
                name: "milestones",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "projects");

            migrationBuilder.DropColumn(
                name: "publish_to_mcp",
                schema: "a2a",
                table: "server_configurations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "projects");

            migrationBuilder.EnsureSchema(
                name: "research");

            migrationBuilder.AddColumn<bool>(
                name: "publish_to_mcp",
                schema: "a2a",
                table: "server_configurations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completion_notes = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "research",
                schema: "research",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    plan = table.Column<string>(type: "text", nullable: true),
                    result = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestones",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completion_notes = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    due_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    success_criteria = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestones", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestones_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "projects",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_file_references",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_file_references", x => x.id);
                    table.ForeignKey(
                        name: "FK_project_file_references_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "projects",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_tags",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_project_tags_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "projects",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "research_tags",
                schema: "research",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    research_id = table.Column<Guid>(type: "uuid", nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_research_tags_research_research_id",
                        column: x => x.research_id,
                        principalSchema: "research",
                        principalTable: "research",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "milestone_file_references",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    milestone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_file_references", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_file_references_milestones_milestone_id",
                        column: x => x.milestone_id,
                        principalSchema: "projects",
                        principalTable: "milestones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "milestone_tags",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    milestone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_tags_milestones_milestone_id",
                        column: x => x.milestone_id,
                        principalSchema: "projects",
                        principalTable: "milestones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notes",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    milestone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    research_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notes", x => x.id);
                    table.ForeignKey(
                        name: "FK_notes_milestones_milestone_id",
                        column: x => x.milestone_id,
                        principalSchema: "projects",
                        principalTable: "milestones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notes_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "projects",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notes_research_research_id",
                        column: x => x.research_id,
                        principalSchema: "research",
                        principalTable: "research",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    milestone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completion_notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_tasks_milestones_milestone_id",
                        column: x => x.milestone_id,
                        principalSchema: "projects",
                        principalTable: "milestones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tasks_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "projects",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "note_tags",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_note_tags_notes_note_id",
                        column: x => x.note_id,
                        principalSchema: "projects",
                        principalTable: "notes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_tags",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    task_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_task_tags_tasks_task_item_id",
                        column: x => x.task_item_id,
                        principalSchema: "projects",
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_milestone_file_references_milestone_id",
                schema: "projects",
                table: "milestone_file_references",
                column: "milestone_id");

            migrationBuilder.CreateIndex(
                name: "ix_milestone_file_references_user_id",
                schema: "projects",
                table: "milestone_file_references",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_milestone_tags_milestone_id",
                schema: "projects",
                table: "milestone_tags",
                column: "milestone_id");

            migrationBuilder.CreateIndex(
                name: "ix_milestone_tags_name",
                schema: "projects",
                table: "milestone_tags",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_milestone_tags_user_id",
                schema: "projects",
                table: "milestone_tags",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_milestones_due_date",
                schema: "projects",
                table: "milestones",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "ix_milestones_project_id",
                schema: "projects",
                table: "milestones",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_milestones_status",
                schema: "projects",
                table: "milestones",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_milestones_user_id",
                schema: "projects",
                table: "milestones",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_note_tags_name",
                schema: "projects",
                table: "note_tags",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_note_tags_note_id",
                schema: "projects",
                table: "note_tags",
                column: "note_id");

            migrationBuilder.CreateIndex(
                name: "ix_note_tags_user_id",
                schema: "projects",
                table: "note_tags",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_notes_created_at",
                schema: "projects",
                table: "notes",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_notes_milestone_id",
                schema: "projects",
                table: "notes",
                column: "milestone_id");

            migrationBuilder.CreateIndex(
                name: "ix_notes_project_id",
                schema: "projects",
                table: "notes",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_notes_research_id",
                schema: "projects",
                table: "notes",
                column: "research_id");

            migrationBuilder.CreateIndex(
                name: "ix_notes_user_id",
                schema: "projects",
                table: "notes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_file_references_project_id",
                schema: "projects",
                table: "project_file_references",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_file_references_user_id",
                schema: "projects",
                table: "project_file_references",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_tags_name",
                schema: "projects",
                table: "project_tags",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_project_tags_project_id",
                schema: "projects",
                table: "project_tags",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_tags_user_id",
                schema: "projects",
                table: "project_tags",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_created_at",
                schema: "projects",
                table: "projects",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_projects_name",
                schema: "projects",
                table: "projects",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_projects_status",
                schema: "projects",
                table: "projects",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_projects_user_id",
                schema: "projects",
                table: "projects",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_created_at",
                schema: "research",
                table: "research",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_research_status",
                schema: "research",
                table: "research",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_research_user_id",
                schema: "research",
                table: "research",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_tags_name",
                schema: "research",
                table: "research_tags",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_research_tags_research_id",
                schema: "research",
                table: "research_tags",
                column: "research_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_tags_user_id",
                schema: "research",
                table: "research_tags",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_tags_name",
                schema: "projects",
                table: "task_tags",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_task_tags_task_item_id",
                schema: "projects",
                table: "task_tags",
                column: "task_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_tags_user_id",
                schema: "projects",
                table: "task_tags",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_milestone_id",
                schema: "projects",
                table: "tasks",
                column: "milestone_id");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_priority",
                schema: "projects",
                table: "tasks",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_project_id",
                schema: "projects",
                table: "tasks",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_status",
                schema: "projects",
                table: "tasks",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_user_id",
                schema: "projects",
                table: "tasks",
                column: "user_id");
        }
    }
}
