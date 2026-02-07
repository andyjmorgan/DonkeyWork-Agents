using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameTodosToTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename todo_tags table first (has FK to todos)
            migrationBuilder.RenameTable(
                name: "todo_tags",
                schema: "projects",
                newName: "task_tags",
                newSchema: "projects");

            // Rename todos table
            migrationBuilder.RenameTable(
                name: "todos",
                schema: "projects",
                newName: "tasks",
                newSchema: "projects");

            // Rename column todo_id to task_item_id in task_tags
            migrationBuilder.RenameColumn(
                name: "todo_id",
                schema: "projects",
                table: "task_tags",
                newName: "task_item_id");

            // Rename indexes on tasks table (formerly todos)
            migrationBuilder.RenameIndex(
                name: "ix_todos_user_id",
                schema: "projects",
                table: "tasks",
                newName: "ix_tasks_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_todos_project_id",
                schema: "projects",
                table: "tasks",
                newName: "ix_tasks_project_id");

            migrationBuilder.RenameIndex(
                name: "ix_todos_milestone_id",
                schema: "projects",
                table: "tasks",
                newName: "ix_tasks_milestone_id");

            migrationBuilder.RenameIndex(
                name: "ix_todos_status",
                schema: "projects",
                table: "tasks",
                newName: "ix_tasks_status");

            migrationBuilder.RenameIndex(
                name: "ix_todos_priority",
                schema: "projects",
                table: "tasks",
                newName: "ix_tasks_priority");

            migrationBuilder.RenameIndex(
                name: "ix_todos_due_date",
                schema: "projects",
                table: "tasks",
                newName: "ix_tasks_due_date");

            // Rename indexes on task_tags table (formerly todo_tags)
            migrationBuilder.RenameIndex(
                name: "ix_todo_tags_user_id",
                schema: "projects",
                table: "task_tags",
                newName: "ix_task_tags_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_todo_tags_todo_id",
                schema: "projects",
                table: "task_tags",
                newName: "ix_task_tags_task_item_id");

            migrationBuilder.RenameIndex(
                name: "ix_todo_tags_name",
                schema: "projects",
                table: "task_tags",
                newName: "ix_task_tags_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse index renames on task_tags
            migrationBuilder.RenameIndex(
                name: "ix_task_tags_name",
                schema: "projects",
                table: "task_tags",
                newName: "ix_todo_tags_name");

            migrationBuilder.RenameIndex(
                name: "ix_task_tags_task_item_id",
                schema: "projects",
                table: "task_tags",
                newName: "ix_todo_tags_todo_id");

            migrationBuilder.RenameIndex(
                name: "ix_task_tags_user_id",
                schema: "projects",
                table: "task_tags",
                newName: "ix_todo_tags_user_id");

            // Reverse index renames on tasks
            migrationBuilder.RenameIndex(
                name: "ix_tasks_due_date",
                schema: "projects",
                table: "tasks",
                newName: "ix_todos_due_date");

            migrationBuilder.RenameIndex(
                name: "ix_tasks_priority",
                schema: "projects",
                table: "tasks",
                newName: "ix_todos_priority");

            migrationBuilder.RenameIndex(
                name: "ix_tasks_status",
                schema: "projects",
                table: "tasks",
                newName: "ix_todos_status");

            migrationBuilder.RenameIndex(
                name: "ix_tasks_milestone_id",
                schema: "projects",
                table: "tasks",
                newName: "ix_todos_milestone_id");

            migrationBuilder.RenameIndex(
                name: "ix_tasks_project_id",
                schema: "projects",
                table: "tasks",
                newName: "ix_todos_project_id");

            migrationBuilder.RenameIndex(
                name: "ix_tasks_user_id",
                schema: "projects",
                table: "tasks",
                newName: "ix_todos_user_id");

            // Rename column back
            migrationBuilder.RenameColumn(
                name: "task_item_id",
                schema: "projects",
                table: "task_tags",
                newName: "todo_id");

            // Rename tables back
            migrationBuilder.RenameTable(
                name: "tasks",
                schema: "projects",
                newName: "todos",
                newSchema: "projects");

            migrationBuilder.RenameTable(
                name: "task_tags",
                schema: "projects",
                newName: "todo_tags",
                newSchema: "projects");
        }
    }
}
