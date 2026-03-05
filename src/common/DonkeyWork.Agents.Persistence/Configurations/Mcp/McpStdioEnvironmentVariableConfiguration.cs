using DonkeyWork.Agents.Persistence.Entities.Mcp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Mcp;

/// <summary>
/// EF Core configuration for the MCP stdio environment variable entity.
/// </summary>
public class McpStdioEnvironmentVariableConfiguration : IEntityTypeConfiguration<McpStdioEnvironmentVariableEntity>
{
    public void Configure(EntityTypeBuilder<McpStdioEnvironmentVariableEntity> builder)
    {
        builder.ToTable("stdio_environment_variables", "mcp", table =>
        {
            table.HasCheckConstraint(
                "ck_stdio_env_var_value_or_credential",
                "(value IS NOT NULL AND credential_id IS NULL AND credential_field_type IS NULL) OR (value IS NULL AND credential_id IS NOT NULL AND credential_field_type IS NOT NULL)");
        });

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.McpStdioConfigurationId)
            .HasColumnName("mcp_stdio_configuration_id")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Value)
            .HasColumnName("value");

        builder.Property(e => e.CredentialId)
            .HasColumnName("credential_id");

        builder.Property(e => e.CredentialFieldType)
            .HasColumnName("credential_field_type")
            .HasMaxLength(50);

        // Unique env var name per stdio configuration
        builder.HasIndex(e => new { e.McpStdioConfigurationId, e.Name })
            .IsUnique()
            .HasDatabaseName("ix_mcp_stdio_env_vars_config_id_name");

        builder.HasIndex(e => e.McpStdioConfigurationId)
            .HasDatabaseName("ix_mcp_stdio_env_vars_mcp_stdio_configuration_id");
    }
}
