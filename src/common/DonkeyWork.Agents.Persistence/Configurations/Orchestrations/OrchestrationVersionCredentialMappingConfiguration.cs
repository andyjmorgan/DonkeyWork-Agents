using DonkeyWork.Agents.Persistence.Entities.Orchestrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Orchestrations;

public class OrchestrationVersionCredentialMappingConfiguration : IEntityTypeConfiguration<OrchestrationVersionCredentialMappingEntity>
{
    public void Configure(EntityTypeBuilder<OrchestrationVersionCredentialMappingEntity> builder)
    {
        builder.ToTable("orchestration_version_credential_mappings", "orchestrations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.OrchestrationVersionId)
            .HasColumnName("orchestration_version_id")
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
            .HasDatabaseName("ix_orchestration_version_credential_mappings_user_id");

        builder.HasIndex(e => e.OrchestrationVersionId)
            .HasDatabaseName("ix_orchestration_version_credential_mappings_orchestration_version_id");

        builder.HasIndex(e => e.CredentialId)
            .HasDatabaseName("ix_orchestration_version_credential_mappings_credential_id");

        builder.HasIndex(e => new { e.OrchestrationVersionId, e.NodeId })
            .IsUnique()
            .HasDatabaseName("ix_orchestration_version_credential_mappings_version_node");
    }
}
