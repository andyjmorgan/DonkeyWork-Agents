using DonkeyWork.Agents.Persistence.Entities.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Actors;

public class AgentExecutionConfiguration : IEntityTypeConfiguration<AgentExecutionEntity>
{
    public void Configure(EntityTypeBuilder<AgentExecutionEntity> builder)
    {
        builder.ToTable("agent_executions", "actors");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(e => e.AgentType)
            .HasColumnName("agent_type")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Label)
            .HasColumnName("label")
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.GrainKey)
            .HasColumnName("grain_key")
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.ParentGrainKey)
            .HasColumnName("parent_grain_key")
            .HasMaxLength(512);

        builder.Property(e => e.ContractSnapshot)
            .HasColumnName("contract_snapshot")
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(e => e.Input)
            .HasColumnName("input");

        builder.Property(e => e.Output)
            .HasColumnName("output")
            .HasColumnType("jsonb");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(e => e.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.DurationMs)
            .HasColumnName("duration_ms");

        builder.Property(e => e.InputTokensUsed)
            .HasColumnName("input_tokens_used");

        builder.Property(e => e.OutputTokensUsed)
            .HasColumnName("output_tokens_used");

        builder.Property(e => e.ModelId)
            .HasColumnName("model_id")
            .HasMaxLength(128);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_agent_executions_user_id");

        builder.HasIndex(e => e.ConversationId)
            .HasDatabaseName("ix_agent_executions_conversation_id");

        builder.HasIndex(e => e.GrainKey)
            .HasDatabaseName("ix_agent_executions_grain_key");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_agent_executions_status");

        builder.HasIndex(e => e.StartedAt)
            .HasDatabaseName("ix_agent_executions_started_at");

        builder.HasIndex(e => new { e.ConversationId, e.StartedAt })
            .HasDatabaseName("ix_agent_executions_conversation_id_started_at");
    }
}
