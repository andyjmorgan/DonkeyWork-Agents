using DonkeyWork.Agents.Persistence.Entities.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Agents;

public class AgentVersionConfiguration : IEntityTypeConfiguration<AgentVersionEntity>
{
    public void Configure(EntityTypeBuilder<AgentVersionEntity> builder)
    {
        builder.ToTable("agent_versions", "agents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.AgentId)
            .HasColumnName("agent_id")
            .IsRequired();

        builder.Property(e => e.VersionNumber)
            .HasColumnName("version_number")
            .IsRequired();

        builder.Property(e => e.IsDraft)
            .HasColumnName("is_draft")
            .IsRequired();

        builder.Property(e => e.InputSchema)
            .HasColumnName("input_schema")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.OutputSchema)
            .HasColumnName("output_schema")
            .HasColumnType("jsonb");

        builder.Property(e => e.ReactFlowData)
            .HasColumnName("react_flow_data")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.NodeConfigurations)
            .HasColumnName("node_configurations")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.PublishedAt)
            .HasColumnName("published_at");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_agent_versions_user_id");

        builder.HasIndex(e => e.AgentId)
            .HasDatabaseName("ix_agent_versions_agent_id");

        builder.HasIndex(e => new { e.AgentId, e.IsDraft })
            .HasDatabaseName("ix_agent_versions_agent_id_is_draft");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_agent_versions_created_at");

        // Relationships
        builder.HasMany(e => e.CredentialMappings)
            .WithOne(m => m.AgentVersion)
            .HasForeignKey(m => m.AgentVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Executions)
            .WithOne(ex => ex.AgentVersion)
            .HasForeignKey(ex => ex.AgentVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
