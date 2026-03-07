using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHeaderValueFormatToSandboxCredentialMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BasicAuthUsername",
                schema: "credentials",
                table: "sandbox_credential_mappings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeaderValueFormat",
                schema: "credentials",
                table: "sandbox_credential_mappings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Raw");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BasicAuthUsername",
                schema: "credentials",
                table: "sandbox_credential_mappings");

            migrationBuilder.DropColumn(
                name: "HeaderValueFormat",
                schema: "credentials",
                table: "sandbox_credential_mappings");
        }
    }
}
