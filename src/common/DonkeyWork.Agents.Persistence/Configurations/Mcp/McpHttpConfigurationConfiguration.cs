using DonkeyWork.Agents.Persistence.Entities.Mcp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Mcp;

/// <summary>
/// EF Core configuration for the MCP HTTP configuration entity.
/// </summary>
public class McpHttpConfigurationConfiguration : IEntityTypeConfiguration<McpHttpConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<McpHttpConfigurationEntity> builder)
    {
        builder.ToTable("http_configurations", "mcp");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.McpServerConfigurationId)
            .HasColumnName("mcp_server_configuration_id")
            .IsRequired();

        builder.Property(e => e.Endpoint)
            .HasColumnName("endpoint")
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.TransportMode)
            .HasColumnName("transport_mode")
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.AuthType)
            .HasColumnName("auth_type")
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.OAuthTokenId)
            .HasColumnName("oauth_token_id");

        // Index on FK
        builder.HasIndex(e => e.McpServerConfigurationId)
            .IsUnique()
            .HasDatabaseName("ix_mcp_http_configurations_mcp_server_configuration_id");

        // Relationships
        builder.HasOne(e => e.OAuthConfiguration)
            .WithOne(o => o.McpHttpConfiguration)
            .HasForeignKey<McpHttpOAuthConfigurationEntity>(o => o.McpHttpConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.HeaderConfigurations)
            .WithOne(h => h.McpHttpConfiguration)
            .HasForeignKey(h => h.McpHttpConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
