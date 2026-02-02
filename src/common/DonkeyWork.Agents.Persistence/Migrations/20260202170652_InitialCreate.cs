using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing schemas and tables if they exist (for clean re-creation)
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS orchestrations CASCADE;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS credentials CASCADE;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS projects CASCADE;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS storage CASCADE;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS mcp CASCADE;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS system CASCADE;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS agents CASCADE;"); // Legacy schema
            migrationBuilder.Sql("DROP TABLE IF EXISTS public.\"DataProtectionKeys\" CASCADE;"); // Legacy table

            migrationBuilder.EnsureSchema(
                name: "system");

            migrationBuilder.EnsureSchema(
                name: "credentials");

            migrationBuilder.EnsureSchema(
                name: "storage");

            migrationBuilder.EnsureSchema(
                name: "projects");

            migrationBuilder.EnsureSchema(
                name: "orchestrations");

            migrationBuilder.EnsureSchema(
                name: "mcp");

            migrationBuilder.CreateTable(
                name: "data_protection_keys",
                schema: "system",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_protection_keys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "external_api_keys",
                schema: "credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FieldsEncrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_api_keys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oauth_provider_configs",
                schema: "credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClientIdEncrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    ClientSecretEncrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    RedirectUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oauth_provider_configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oauth_tokens",
                schema: "credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalUserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AccessTokenEncrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    RefreshTokenEncrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    ScopesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastRefreshedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oauth_tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    success_criteria = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stored_files",
                schema: "storage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    BucketName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ChecksumSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MarkedForDeletionAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stored_files", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tool_invocation_logs",
                schema: "mcp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tool_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    tool_provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    request_params = table.Column<string>(type: "jsonb", nullable: false),
                    response_content = table.Column<string>(type: "jsonb", nullable: true),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    invoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    client_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    client_ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    session_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tool_invocation_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_api_keys",
                schema: "credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EncryptedKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_api_keys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "milestones",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    success_criteria = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    due_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "project_file_references",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "project_tags",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "file_shares",
                schema: "storage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShareToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxDownloads = table.Column<int>(type: "integer", nullable: true),
                    DownloadCount = table.Column<int>(type: "integer", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_shares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_file_shares_stored_files_FileId",
                        column: x => x.FileId,
                        principalSchema: "storage",
                        principalTable: "stored_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "milestone_file_references",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    milestone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "milestone_tags",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    milestone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "notes",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    milestone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "todos",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
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
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_todos", x => x.id);
                    table.ForeignKey(
                        name: "FK_todos_milestones_milestone_id",
                        column: x => x.milestone_id,
                        principalSchema: "projects",
                        principalTable: "milestones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_todos_projects_project_id",
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
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "todo_tags",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    todo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "orchestration_execution_logs",
                schema: "orchestrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    log_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    node_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                    @interface = table.Column<string>(name: "interface", type: "character varying(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_orchestration_executions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orchestration_node_executions",
                schema: "orchestrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    orchestration_execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    node_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    action_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    is_draft = table.Column<bool>(type: "boolean", nullable: false),
                    input_schema = table.Column<string>(type: "jsonb", nullable: false),
                    output_schema = table.Column<string>(type: "jsonb", nullable: true),
                    react_flow_data = table.Column<string>(type: "jsonb", nullable: false),
                    node_configurations = table.Column<string>(type: "jsonb", nullable: false),
                    interfaces = table.Column<string>(type: "jsonb", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    current_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "IX_external_api_keys_UserId",
                schema: "credentials",
                table: "external_api_keys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_external_api_keys_UserId_Provider",
                schema: "credentials",
                table: "external_api_keys",
                columns: new[] { "UserId", "Provider" });

            migrationBuilder.CreateIndex(
                name: "IX_file_shares_ExpiresAt",
                schema: "storage",
                table: "file_shares",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_file_shares_FileId",
                schema: "storage",
                table: "file_shares",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_file_shares_ShareToken",
                schema: "storage",
                table: "file_shares",
                column: "ShareToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_file_shares_UserId",
                schema: "storage",
                table: "file_shares",
                column: "UserId");

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
                name: "ix_notes_user_id",
                schema: "projects",
                table: "notes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_oauth_provider_configs_UserId",
                schema: "credentials",
                table: "oauth_provider_configs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_oauth_provider_configs_UserId_Provider",
                schema: "credentials",
                table: "oauth_provider_configs",
                columns: new[] { "UserId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oauth_tokens_ExpiresAt",
                schema: "credentials",
                table: "oauth_tokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_oauth_tokens_UserId",
                schema: "credentials",
                table: "oauth_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_oauth_tokens_UserId_Provider",
                schema: "credentials",
                table: "oauth_tokens",
                columns: new[] { "UserId", "Provider" },
                unique: true);

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
                name: "IX_stored_files_ObjectKey",
                schema: "storage",
                table: "stored_files",
                column: "ObjectKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stored_files_Status",
                schema: "storage",
                table: "stored_files",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_stored_files_UserId",
                schema: "storage",
                table: "stored_files",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_todo_tags_name",
                schema: "projects",
                table: "todo_tags",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_todo_tags_todo_id",
                schema: "projects",
                table: "todo_tags",
                column: "todo_id");

            migrationBuilder.CreateIndex(
                name: "ix_todo_tags_user_id",
                schema: "projects",
                table: "todo_tags",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_todos_due_date",
                schema: "projects",
                table: "todos",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "ix_todos_milestone_id",
                schema: "projects",
                table: "todos",
                column: "milestone_id");

            migrationBuilder.CreateIndex(
                name: "ix_todos_priority",
                schema: "projects",
                table: "todos",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "ix_todos_project_id",
                schema: "projects",
                table: "todos",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_todos_status",
                schema: "projects",
                table: "todos",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_todos_user_id",
                schema: "projects",
                table: "todos",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_tool_invocation_logs_invoked_at",
                schema: "mcp",
                table: "tool_invocation_logs",
                column: "invoked_at");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_tool_invocation_logs_tool_name",
                schema: "mcp",
                table: "tool_invocation_logs",
                column: "tool_name");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_tool_invocation_logs_tool_name_invoked_at",
                schema: "mcp",
                table: "tool_invocation_logs",
                columns: new[] { "tool_name", "invoked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_mcp_tool_invocation_logs_user_id",
                schema: "mcp",
                table: "tool_invocation_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_tool_invocation_logs_user_id_invoked_at",
                schema: "mcp",
                table: "tool_invocation_logs",
                columns: new[] { "user_id", "invoked_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_api_keys_UserId",
                schema: "credentials",
                table: "user_api_keys",
                column: "UserId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orchestrations_orchestration_versions_current_version_id",
                schema: "orchestrations",
                table: "orchestrations");

            migrationBuilder.DropTable(
                name: "data_protection_keys",
                schema: "system");

            migrationBuilder.DropTable(
                name: "external_api_keys",
                schema: "credentials");

            migrationBuilder.DropTable(
                name: "file_shares",
                schema: "storage");

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
                name: "oauth_provider_configs",
                schema: "credentials");

            migrationBuilder.DropTable(
                name: "oauth_tokens",
                schema: "credentials");

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
                name: "project_file_references",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "project_tags",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "todo_tags",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "tool_invocation_logs",
                schema: "mcp");

            migrationBuilder.DropTable(
                name: "user_api_keys",
                schema: "credentials");

            migrationBuilder.DropTable(
                name: "stored_files",
                schema: "storage");

            migrationBuilder.DropTable(
                name: "notes",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "orchestration_executions",
                schema: "orchestrations");

            migrationBuilder.DropTable(
                name: "todos",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "milestones",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "orchestration_versions",
                schema: "orchestrations");

            migrationBuilder.DropTable(
                name: "orchestrations",
                schema: "orchestrations");
        }
    }
}
