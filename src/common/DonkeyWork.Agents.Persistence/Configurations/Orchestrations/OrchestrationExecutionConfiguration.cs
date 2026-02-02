using DonkeyWork.Agents.Persistence.Entities.Orchestrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Orchestrations;

public class OrchestrationExecutionConfiguration : IEntityTypeConfiguration<OrchestrationExecutionEntity>
{
    public void Configure(EntityTypeBuilder<OrchestrationExecutionEntity> builder)
    {
        builder.ToTable("orchestration_executions", "orchestrations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.OrchestrationId)
            .HasColumnName("orchestration_id")
            .IsRequired();

        builder.Property(e => e.OrchestrationVersionId)
            .HasColumnName("orchestration_version_id")
            .IsRequired();

        builder.Property(e => e.Interface)
            .HasColumnName("interface")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

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
            .HasDatabaseName("ix_orchestration_executions_user_id");

        builder.HasIndex(e => e.OrchestrationId)
            .HasDatabaseName("ix_orchestration_executions_orchestration_id");

        builder.HasIndex(e => e.OrchestrationVersionId)
            .HasDatabaseName("ix_orchestration_executions_orchestration_version_id");

        builder.HasIndex(e => e.Interface)
            .HasDatabaseName("ix_orchestration_executions_interface");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_orchestration_executions_status");

        builder.HasIndex(e => e.StartedAt)
            .HasDatabaseName("ix_orchestration_executions_started_at");

        builder.HasIndex(e => new { e.OrchestrationId, e.StartedAt })
            .HasDatabaseName("ix_orchestration_executions_orchestration_id_started_at");

        // Relationships
        builder.HasMany(e => e.NodeExecutions)
            .WithOne(ne => ne.OrchestrationExecution)
            .HasForeignKey(ne => ne.OrchestrationExecutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
