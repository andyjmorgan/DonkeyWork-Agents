using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_api_keys_KeyHash",
                schema: "credentials",
                table: "user_api_keys");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "credentials",
                table: "user_api_keys");

            migrationBuilder.DropColumn(
                name: "IsRevoked",
                schema: "credentials",
                table: "user_api_keys");

            migrationBuilder.DropColumn(
                name: "KeyHash",
                schema: "credentials",
                table: "user_api_keys");

            migrationBuilder.DropColumn(
                name: "KeyPrefix",
                schema: "credentials",
                table: "user_api_keys");

            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                schema: "credentials",
                table: "user_api_keys");

            migrationBuilder.EnsureSchema(
                name: "storage");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "credentials",
                table: "user_api_keys",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedKey",
                schema: "credentials",
                table: "user_api_keys",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_shares",
                schema: "storage");

            migrationBuilder.DropTable(
                name: "stored_files",
                schema: "storage");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "credentials",
                table: "user_api_keys");

            migrationBuilder.DropColumn(
                name: "EncryptedKey",
                schema: "credentials",
                table: "user_api_keys");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                schema: "credentials",
                table: "user_api_keys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRevoked",
                schema: "credentials",
                table: "user_api_keys",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KeyHash",
                schema: "credentials",
                table: "user_api_keys",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KeyPrefix",
                schema: "credentials",
                table: "user_api_keys",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUsedAt",
                schema: "credentials",
                table: "user_api_keys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_api_keys_KeyHash",
                schema: "credentials",
                table: "user_api_keys",
                column: "KeyHash",
                unique: true);
        }
    }
}
