using DonkeyWork.Agents.Persistence.Entities.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Agents;

public class AgentExecutionConfiguration : IEntityTypeConfiguration<AgentExecutionEntity>
{
    public void Configure(EntityTypeBuilder<AgentExecutionEntity> builder)
    {
        builder.ToTable("agent_executions", "agents");

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

        builder.Property(e => e.AgentVersionId)
            .HasColumnName("agent_version_id")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Input)
            .HasColumnName("input")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.Output)
            .HasColumnName("output")
            .HasColumnType("jsonb");

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(e => e.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.TotalTokensUsed)
            .HasColumnName("total_tokens_used");

        builder.Property(e => e.StreamName)
            .HasColumnName("stream_name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_agent_executions_user_id");

        builder.HasIndex(e => e.AgentId)
            .HasDatabaseName("ix_agent_executions_agent_id");

        builder.HasIndex(e => e.AgentVersionId)
            .HasDatabaseName("ix_agent_executions_agent_version_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_agent_executions_status");

        builder.HasIndex(e => e.StartedAt)
            .HasDatabaseName("ix_agent_executions_started_at");

        builder.HasIndex(e => new { e.AgentId, e.StartedAt })
            .HasDatabaseName("ix_agent_executions_agent_id_started_at");

        // Relationships
        builder.HasMany(e => e.NodeExecutions)
            .WithOne(ne => ne.AgentExecution)
            .HasForeignKey(ne => ne.AgentExecutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
