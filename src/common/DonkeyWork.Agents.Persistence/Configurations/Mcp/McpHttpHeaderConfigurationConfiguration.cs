using DonkeyWork.Agents.Persistence.Entities.Mcp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Mcp;

/// <summary>
/// EF Core configuration for the MCP HTTP header configuration entity.
/// </summary>
public class McpHttpHeaderConfigurationConfiguration : IEntityTypeConfiguration<McpHttpHeaderConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<McpHttpHeaderConfigurationEntity> builder)
    {
        builder.ToTable("http_header_configurations", "mcp", table =>
        {
            table.HasCheckConstraint(
                "ck_http_header_value_or_credential",
                "(header_value_encrypted IS NOT NULL AND credential_id IS NULL AND credential_field_type IS NULL) OR (header_value_encrypted IS NULL AND credential_id IS NOT NULL AND credential_field_type IS NOT NULL)");
        });

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.McpHttpConfigurationId)
            .HasColumnName("mcp_http_configuration_id")
            .IsRequired();

        builder.Property(e => e.HeaderName)
            .HasColumnName("header_name")
            .IsRequired()
            .HasMaxLength(255);

        // Encrypted header value (nullable when using credential reference)
        builder.Property(e => e.HeaderValueEncrypted)
            .HasColumnName("header_value_encrypted")
            .HasColumnType("bytea");

        builder.Property(e => e.CredentialId)
            .HasColumnName("credential_id");

        builder.Property(e => e.CredentialFieldType)
            .HasColumnName("credential_field_type")
            .HasMaxLength(50);

        // Indexes
        builder.HasIndex(e => e.McpHttpConfigurationId)
            .HasDatabaseName("ix_mcp_http_header_configurations_mcp_http_configuration_id");

        builder.HasIndex(e => new { e.McpHttpConfigurationId, e.HeaderName })
            .IsUnique()
            .HasDatabaseName("ix_mcp_http_header_configurations_config_id_header_name");
    }
}
