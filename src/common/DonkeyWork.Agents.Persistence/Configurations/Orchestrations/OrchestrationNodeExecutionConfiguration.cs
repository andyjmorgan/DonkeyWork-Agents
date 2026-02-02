using DonkeyWork.Agents.Persistence.Entities.Orchestrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Orchestrations;

public class OrchestrationNodeExecutionConfiguration : IEntityTypeConfiguration<OrchestrationNodeExecutionEntity>
{
    public void Configure(EntityTypeBuilder<OrchestrationNodeExecutionEntity> builder)
    {
        builder.ToTable("orchestration_node_executions", "orchestrations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.OrchestrationExecutionId)
            .HasColumnName("orchestration_execution_id")
            .IsRequired();

        builder.Property(e => e.NodeId)
            .HasColumnName("node_id")
            .IsRequired();

        builder.Property(e => e.NodeType)
            .HasColumnName("node_type")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.NodeName)
            .HasColumnName("node_name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.ActionType)
            .HasColumnName("action_type")
            .HasMaxLength(100);

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Input)
            .HasColumnName("input")
            .HasColumnType("jsonb");

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

        builder.Property(e => e.TokensUsed)
            .HasColumnName("tokens_used");

        builder.Property(e => e.FullResponse)
            .HasColumnName("full_response")
            .HasColumnType("text");

        builder.Property(e => e.DurationMs)
            .HasColumnName("duration_ms");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_orchestration_node_executions_user_id");

        builder.HasIndex(e => e.OrchestrationExecutionId)
            .HasDatabaseName("ix_orchestration_node_executions_orchestration_execution_id");

        builder.HasIndex(e => e.NodeType)
            .HasDatabaseName("ix_orchestration_node_executions_node_type");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_orchestration_node_executions_status");

        builder.HasIndex(e => e.StartedAt)
            .HasDatabaseName("ix_orchestration_node_executions_started_at");
    }
}
