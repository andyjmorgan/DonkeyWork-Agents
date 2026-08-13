using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "providers");

            migrationBuilder.CreateTable(
                name: "custom_models",
                schema: "providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    WireFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ApiKeyEncrypted = table.Column<string>(type: "text", nullable: true),
                    MaxInputTokens = table.Column<int>(type: "integer", nullable: false),
                    MaxOutputTokens = table.Column<int>(type: "integer", nullable: false),
                    SupportsTools = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custom_models", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_custom_models_UserId_Name",
                schema: "providers",
                table: "custom_models",
                columns: new[] { "UserId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "custom_models",
                schema: "providers");
        }
    }
}
