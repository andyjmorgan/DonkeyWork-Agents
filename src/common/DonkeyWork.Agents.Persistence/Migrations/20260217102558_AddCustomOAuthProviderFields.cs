using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomOAuthProviderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_oauth_provider_configs_UserId_Provider",
                schema: "credentials",
                table: "oauth_provider_configs");

            migrationBuilder.AddColumn<string>(
                name: "AuthorizationUrl",
                schema: "credentials",
                table: "oauth_provider_configs",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomProviderName",
                schema: "credentials",
                table: "oauth_provider_configs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScopesJson",
                schema: "credentials",
                table: "oauth_provider_configs",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenUrl",
                schema: "credentials",
                table: "oauth_provider_configs",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserInfoUrl",
                schema: "credentials",
                table: "oauth_provider_configs",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_oauth_provider_configs_UserId_Provider",
                schema: "credentials",
                table: "oauth_provider_configs",
                columns: new[] { "UserId", "Provider" },
                unique: true,
                filter: "\"Provider\" != 'Custom'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_oauth_provider_configs_UserId_Provider",
                schema: "credentials",
                table: "oauth_provider_configs");

            migrationBuilder.DropColumn(
                name: "AuthorizationUrl",
                schema: "credentials",
                table: "oauth_provider_configs");

            migrationBuilder.DropColumn(
                name: "CustomProviderName",
                schema: "credentials",
                table: "oauth_provider_configs");

            migrationBuilder.DropColumn(
                name: "ScopesJson",
                schema: "credentials",
                table: "oauth_provider_configs");

            migrationBuilder.DropColumn(
                name: "TokenUrl",
                schema: "credentials",
                table: "oauth_provider_configs");

            migrationBuilder.DropColumn(
                name: "UserInfoUrl",
                schema: "credentials",
                table: "oauth_provider_configs");

            migrationBuilder.CreateIndex(
                name: "IX_oauth_provider_configs_UserId_Provider",
                schema: "credentials",
                table: "oauth_provider_configs",
                columns: new[] { "UserId", "Provider" },
                unique: true);
        }
    }
}
