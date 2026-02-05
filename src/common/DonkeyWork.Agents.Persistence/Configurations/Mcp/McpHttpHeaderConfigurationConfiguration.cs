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
        builder.ToTable("http_header_configurations", "mcp");

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

        // Encrypted using pgcrypto
        builder.Property(e => e.HeaderValueEncrypted)
            .HasColumnName("header_value_encrypted")
            .IsRequired()
            .HasColumnType("bytea");

        // Indexes
        builder.HasIndex(e => e.McpHttpConfigurationId)
            .HasDatabaseName("ix_mcp_http_header_configurations_mcp_http_configuration_id");

        builder.HasIndex(e => new { e.McpHttpConfigurationId, e.HeaderName })
            .IsUnique()
            .HasDatabaseName("ix_mcp_http_header_configurations_config_id_header_name");
    }
}
