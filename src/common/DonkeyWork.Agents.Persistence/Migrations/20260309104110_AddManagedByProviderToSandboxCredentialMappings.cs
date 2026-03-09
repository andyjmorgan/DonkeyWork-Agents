using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedByProviderToSandboxCredentialMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ManagedByProvider",
                schema: "credentials",
                table: "sandbox_credential_mappings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Backfill existing provider-managed mappings
            migrationBuilder.Sql("""
                UPDATE credentials.sandbox_credential_mappings
                SET "ManagedByProvider" = 'GitHub'
                WHERE "BaseDomain" IN ('api.github.com', 'github.com')
                AND "CredentialType" = 'OAuthToken';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManagedByProvider",
                schema: "credentials",
                table: "sandbox_credential_mappings");
        }
    }
}
