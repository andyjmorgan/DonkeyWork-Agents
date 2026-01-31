using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "projects");

            // Create projects table
            migrationBuilder.CreateTable(
                name: "projects",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    success_criteria = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                });

            // Create milestones table
            migrationBuilder.CreateTable(
                name: "milestones",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    success_criteria = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    due_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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

            // Create todos table
            migrationBuilder.CreateTable(
                name: "todos",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    completion_notes = table.Column<string>(type: "text", nullable: true),
                    due_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    milestone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_todos", x => x.id);
                    table.ForeignKey(
                        name: "FK_todos_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "projects",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_todos_milestones_milestone_id",
                        column: x => x.milestone_id,
                        principalSchema: "projects",
                        principalTable: "milestones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create notes table
            migrationBuilder.CreateTable(
                name: "notes",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    milestone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notes", x => x.id);
                    table.ForeignKey(
                        name: "FK_notes_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "projects",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notes_milestones_milestone_id",
                        column: x => x.milestone_id,
                        principalSchema: "projects",
                        principalTable: "milestones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create project_tags table
            migrationBuilder.CreateTable(
                name: "project_tags",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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

            // Create milestone_tags table
            migrationBuilder.CreateTable(
                name: "milestone_tags",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    milestone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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

            // Create todo_tags table
            migrationBuilder.CreateTable(
                name: "todo_tags",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    todo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_todo_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_todo_tags_todos_todo_id",
                        column: x => x.todo_id,
                        principalSchema: "projects",
                        principalTable: "todos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create note_tags table
            migrationBuilder.CreateTable(
                name: "note_tags",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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

            // Create project_file_references table
            migrationBuilder.CreateTable(
                name: "project_file_references",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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

            // Create milestone_file_references table
            migrationBuilder.CreateTable(
                name: "milestone_file_references",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    milestone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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

            // Create indexes for projects
            migrationBuilder.CreateIndex(
                name: "ix_projects_user_id",
                schema: "projects",
                table: "projects",
                column: "user_id");

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
                name: "ix_projects_created_at",
                schema: "projects",
                table: "projects",
                column: "created_at");

            // Create indexes for milestones
            migrationBuilder.CreateIndex(
                name: "ix_milestones_user_id",
                schema: "projects",
                table: "milestones",
                column: "user_id");

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
                name: "ix_milestones_due_date",
                schema: "projects",
                table: "milestones",
                column: "due_date");

            // Create indexes for todos
            migrationBuilder.CreateIndex(
                name: "ix_todos_user_id",
                schema: "projects",
                table: "todos",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_todos_project_id",
                schema: "projects",
                table: "todos",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_todos_milestone_id",
                schema: "projects",
                table: "todos",
                column: "milestone_id");

            migrationBuilder.CreateIndex(
                name: "ix_todos_status",
                schema: "projects",
                table: "todos",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_todos_priority",
                schema: "projects",
                table: "todos",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "ix_todos_due_date",
                schema: "projects",
                table: "todos",
                column: "due_date");

            // Create indexes for notes
            migrationBuilder.CreateIndex(
                name: "ix_notes_user_id",
                schema: "projects",
                table: "notes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_notes_project_id",
                schema: "projects",
                table: "notes",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_notes_milestone_id",
                schema: "projects",
                table: "notes",
                column: "milestone_id");

            migrationBuilder.CreateIndex(
                name: "ix_notes_created_at",
                schema: "projects",
                table: "notes",
                column: "created_at");

            // Create indexes for tags
            migrationBuilder.CreateIndex(
                name: "ix_project_tags_user_id",
                schema: "projects",
                table: "project_tags",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_tags_project_id",
                schema: "projects",
                table: "project_tags",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_tags_name",
                schema: "projects",
                table: "project_tags",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_milestone_tags_user_id",
                schema: "projects",
                table: "milestone_tags",
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
                name: "ix_todo_tags_user_id",
                schema: "projects",
                table: "todo_tags",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_todo_tags_todo_id",
                schema: "projects",
                table: "todo_tags",
                column: "todo_id");

            migrationBuilder.CreateIndex(
                name: "ix_todo_tags_name",
                schema: "projects",
                table: "todo_tags",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_note_tags_user_id",
                schema: "projects",
                table: "note_tags",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_note_tags_note_id",
                schema: "projects",
                table: "note_tags",
                column: "note_id");

            migrationBuilder.CreateIndex(
                name: "ix_note_tags_name",
                schema: "projects",
                table: "note_tags",
                column: "name");

            // Create indexes for file references
            migrationBuilder.CreateIndex(
                name: "ix_project_file_references_user_id",
                schema: "projects",
                table: "project_file_references",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_file_references_project_id",
                schema: "projects",
                table: "project_file_references",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_milestone_file_references_user_id",
                schema: "projects",
                table: "milestone_file_references",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_milestone_file_references_milestone_id",
                schema: "projects",
                table: "milestone_file_references",
                column: "milestone_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_file_references",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "project_file_references",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "note_tags",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "todo_tags",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "milestone_tags",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "project_tags",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "notes",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "todos",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "milestones",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "projects");
        }
    }
}
