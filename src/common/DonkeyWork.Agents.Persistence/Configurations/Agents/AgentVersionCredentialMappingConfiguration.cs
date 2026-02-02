using DonkeyWork.Agents.Persistence.Entities.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Agents;

public class AgentVersionCredentialMappingConfiguration : IEntityTypeConfiguration<AgentVersionCredentialMappingEntity>
{
    public void Configure(EntityTypeBuilder<AgentVersionCredentialMappingEntity> builder)
    {
        builder.ToTable("agent_version_credential_mappings", "agents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.AgentVersionId)
            .HasColumnName("agent_version_id")
            .IsRequired();

        builder.Property(e => e.NodeId)
            .HasColumnName("node_id")
            .IsRequired();

        builder.Property(e => e.CredentialId)
            .HasColumnName("credential_id")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_agent_version_credential_mappings_user_id");

        builder.HasIndex(e => e.AgentVersionId)
            .HasDatabaseName("ix_agent_version_credential_mappings_agent_version_id");

        builder.HasIndex(e => e.CredentialId)
            .HasDatabaseName("ix_agent_version_credential_mappings_credential_id");

        builder.HasIndex(e => new { e.AgentVersionId, e.NodeId })
            .IsUnique()
            .HasDatabaseName("ix_agent_version_credential_mappings_version_node");
    }
}
