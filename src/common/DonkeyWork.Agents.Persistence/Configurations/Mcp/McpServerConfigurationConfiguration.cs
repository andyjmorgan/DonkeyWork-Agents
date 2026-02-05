using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Persistence.Entities.Mcp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Mcp;

/// <summary>
/// EF Core configuration for the MCP server configuration entity.
/// </summary>
public class McpServerConfigurationConfiguration : IEntityTypeConfiguration<McpServerConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<McpServerConfigurationEntity> builder)
    {
        builder.ToTable("server_configurations", "mcp");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(e => e.TransportType)
            .HasColumnName("transport_type")
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_mcp_server_configurations_user_id");

        builder.HasIndex(e => new { e.UserId, e.Name })
            .IsUnique()
            .HasDatabaseName("ix_mcp_server_configurations_user_id_name");

        builder.HasIndex(e => new { e.UserId, e.IsEnabled })
            .HasDatabaseName("ix_mcp_server_configurations_user_id_is_enabled");

        // Relationships
        builder.HasOne(e => e.StdioConfiguration)
            .WithOne(s => s.McpServerConfiguration)
            .HasForeignKey<McpStdioConfigurationEntity>(s => s.McpServerConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.HttpConfiguration)
            .WithOne(h => h.McpServerConfiguration)
            .HasForeignKey<McpHttpConfigurationEntity>(h => h.McpServerConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
