using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MultiSelectInterfaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert existing single-object interface values to arrays.
            // Old format: {"type":"DirectInterfaceConfig",...}
            // New format: [{"type":"DirectInterfaceConfig",...}]
            migrationBuilder.Sql("""
                UPDATE orchestrations.versions
                SET interface = jsonb_build_array(interface)
                WHERE jsonb_typeof(interface) = 'object';
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Convert arrays back to single objects (take first element).
            migrationBuilder.Sql("""
                UPDATE orchestrations.versions
                SET interface = interface->0
                WHERE jsonb_typeof(interface) = 'array' AND jsonb_array_length(interface) > 0;
            """);
        }
    }
}
