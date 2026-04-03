using DonkeyWork.Agents.Persistence.Entities.Scheduling;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Scheduling;

public class ScheduledJobExecutionConfiguration : IEntityTypeConfiguration<ScheduledJobExecutionEntity>
{
    public void Configure(EntityTypeBuilder<ScheduledJobExecutionEntity> builder)
    {
        builder.ToTable("scheduled_job_executions", "scheduling");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.ScheduledJobId)
            .HasColumnName("scheduled_job_id")
            .IsRequired();

        builder.Property(e => e.QuartzFireInstanceId)
            .HasColumnName("quartz_fire_instance_id")
            .HasMaxLength(200);

        builder.Property(e => e.TriggerSource)
            .HasColumnName("trigger_source")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.StartedAtUtc)
            .HasColumnName("started_at_utc")
            .IsRequired();

        builder.Property(e => e.CompletedAtUtc)
            .HasColumnName("completed_at_utc");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.ErrorDetails)
            .HasColumnName("error_details");

        builder.Property(e => e.OutputSummary)
            .HasColumnName("output_summary");

        builder.Property(e => e.ExecutingNodeId)
            .HasColumnName("executing_node_id")
            .HasMaxLength(200);

        builder.Property(e => e.CorrelationId)
            .HasColumnName("correlation_id");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(e => new { e.ScheduledJobId, e.StartedAtUtc })
            .IsDescending(false, true)
            .HasDatabaseName("ix_scheduled_job_executions_job_started");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_scheduled_job_executions_status");

        builder.HasIndex(e => e.CorrelationId)
            .HasDatabaseName("ix_scheduled_job_executions_correlation_id");
    }
}
