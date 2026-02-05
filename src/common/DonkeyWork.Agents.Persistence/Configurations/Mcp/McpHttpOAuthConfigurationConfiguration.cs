using DonkeyWork.Agents.Persistence.Entities.Mcp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Mcp;

/// <summary>
/// EF Core configuration for the MCP HTTP OAuth configuration entity.
/// </summary>
public class McpHttpOAuthConfigurationConfiguration : IEntityTypeConfiguration<McpHttpOAuthConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<McpHttpOAuthConfigurationEntity> builder)
    {
        builder.ToTable("http_oauth_configurations", "mcp");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.McpHttpConfigurationId)
            .HasColumnName("mcp_http_configuration_id")
            .IsRequired();

        builder.Property(e => e.ClientId)
            .HasColumnName("client_id")
            .IsRequired()
            .HasMaxLength(255);

        // Encrypted using pgcrypto
        builder.Property(e => e.ClientSecretEncrypted)
            .HasColumnName("client_secret_encrypted")
            .IsRequired()
            .HasColumnType("bytea");

        builder.Property(e => e.RedirectUri)
            .HasColumnName("redirect_uri")
            .HasMaxLength(2000);

        builder.Property(e => e.Scopes)
            .HasColumnName("scopes")
            .IsRequired()
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(e => e.AuthorizationEndpoint)
            .HasColumnName("authorization_endpoint")
            .HasMaxLength(2000);

        builder.Property(e => e.TokenEndpoint)
            .HasColumnName("token_endpoint")
            .HasMaxLength(2000);

        // Index on FK
        builder.HasIndex(e => e.McpHttpConfigurationId)
            .IsUnique()
            .HasDatabaseName("ix_mcp_http_oauth_configurations_mcp_http_configuration_id");
    }
}
