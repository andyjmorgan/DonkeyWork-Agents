using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "credentials");

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
                name: "user_api_keys",
                schema: "credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    KeyPrefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_api_keys", x => x.Id);
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
                name: "IX_user_api_keys_KeyHash",
                schema: "credentials",
                table: "user_api_keys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_api_keys_UserId",
                schema: "credentials",
                table: "user_api_keys",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_api_keys",
                schema: "credentials");

            migrationBuilder.DropTable(
                name: "oauth_provider_configs",
                schema: "credentials");

            migrationBuilder.DropTable(
                name: "oauth_tokens",
                schema: "credentials");

            migrationBuilder.DropTable(
                name: "user_api_keys",
                schema: "credentials");
        }
    }
}
