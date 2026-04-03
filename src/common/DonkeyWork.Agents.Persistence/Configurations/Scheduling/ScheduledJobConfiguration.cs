using DonkeyWork.Agents.Persistence.Entities.Scheduling;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Scheduling;

public class ScheduledJobConfiguration : IEntityTypeConfiguration<ScheduledJobEntity>
{
    public void Configure(EntityTypeBuilder<ScheduledJobEntity> builder)
    {
        builder.ToTable("scheduled_jobs", "scheduling");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.JobType)
            .HasColumnName("job_type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.ScheduleMode)
            .HasColumnName("schedule_mode")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.CronExpression)
            .HasColumnName("cron_expression")
            .HasMaxLength(120);

        builder.Property(e => e.RunAtUtc)
            .HasColumnName("run_at_utc");

        builder.Property(e => e.TimeZoneId)
            .HasColumnName("time_zone_id")
            .HasMaxLength(100)
            .HasDefaultValue("Europe/Dublin")
            .IsRequired();

        builder.Property(e => e.IsEnabled)
            .HasColumnName("is_enabled")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(e => e.IsSystem)
            .HasColumnName("is_system")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.TargetType)
            .HasColumnName("target_type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.TargetAgentDefinitionId)
            .HasColumnName("target_agent_definition_id");

        builder.Property(e => e.TargetOrchestrationId)
            .HasColumnName("target_orchestration_id");

        builder.Property(e => e.QuartzJobKey)
            .HasColumnName("quartz_job_key")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.QuartzTriggerKey)
            .HasColumnName("quartz_trigger_key")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.CreatorEmail)
            .HasColumnName("creator_email")
            .HasMaxLength(255);

        builder.Property(e => e.CreatorName)
            .HasColumnName("creator_name")
            .HasMaxLength(255);

        builder.Property(e => e.CreatorUsername)
            .HasColumnName("creator_username")
            .HasMaxLength(255);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_scheduled_jobs_user_id");

        builder.HasIndex(e => e.JobType)
            .HasDatabaseName("ix_scheduled_jobs_job_type");

        builder.HasIndex(e => e.IsSystem)
            .HasDatabaseName("ix_scheduled_jobs_is_system");

        builder.HasIndex(e => e.IsEnabled)
            .HasDatabaseName("ix_scheduled_jobs_is_enabled");

        builder.HasIndex(e => e.TargetType)
            .HasDatabaseName("ix_scheduled_jobs_target_type");

        builder.HasOne(e => e.Payload)
            .WithOne(p => p.ScheduledJob)
            .HasForeignKey<ScheduledJobPayloadEntity>(p => p.ScheduledJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Executions)
            .WithOne(ex => ex.ScheduledJob)
            .HasForeignKey(ex => ex.ScheduledJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
