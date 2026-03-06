using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSandboxCredentialMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sandbox_credential_mappings",
                schema: "credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    BaseDomain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    HeaderName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    HeaderValuePrefix = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CredentialId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CredentialFieldType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sandbox_credential_mappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sandbox_credential_mappings_UserId",
                schema: "credentials",
                table: "sandbox_credential_mappings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_sandbox_credential_mappings_UserId_BaseDomain_HeaderName",
                schema: "credentials",
                table: "sandbox_credential_mappings",
                columns: new[] { "UserId", "BaseDomain", "HeaderName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sandbox_credential_mappings",
                schema: "credentials");
        }
    }
}
