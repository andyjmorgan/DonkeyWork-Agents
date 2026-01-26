using DonkeyWork.Agents.Persistence.Entities.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Agents;

public class AgentExecutionLogConfiguration : IEntityTypeConfiguration<AgentExecutionLogEntity>
{
    public void Configure(EntityTypeBuilder<AgentExecutionLogEntity> builder)
    {
        builder.ToTable("agent_execution_logs", "agents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.ExecutionId)
            .HasColumnName("execution_id")
            .IsRequired();

        builder.Property(e => e.LogLevel)
            .HasColumnName("log_level")
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Message)
            .HasColumnName("message")
            .IsRequired();

        builder.Property(e => e.Details)
            .HasColumnName("details")
            .HasColumnType("jsonb");

        builder.Property(e => e.NodeId)
            .HasColumnName("node_id")
            .HasMaxLength(255);

        builder.Property(e => e.Source)
            .HasColumnName("source")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.ExecutionId)
            .HasDatabaseName("ix_agent_execution_logs_execution_id");

        builder.HasIndex(e => e.LogLevel)
            .HasDatabaseName("ix_agent_execution_logs_log_level");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_agent_execution_logs_created_at");

        builder.HasIndex(e => new { e.ExecutionId, e.CreatedAt })
            .HasDatabaseName("ix_agent_execution_logs_execution_id_created_at");

        // Relationship
        builder.HasOne(e => e.Execution)
            .WithMany()
            .HasForeignKey(e => e.ExecutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
