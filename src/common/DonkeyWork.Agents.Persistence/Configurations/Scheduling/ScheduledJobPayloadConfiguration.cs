using DonkeyWork.Agents.Persistence.Entities.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Scheduling;

public class ScheduledJobPayloadConfiguration : IEntityTypeConfiguration<ScheduledJobPayloadEntity>
{
    public void Configure(EntityTypeBuilder<ScheduledJobPayloadEntity> builder)
    {
        builder.ToTable("scheduled_job_payloads", "scheduling");

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

        builder.Property(e => e.UserPrompt)
            .HasColumnName("user_prompt")
            .IsRequired();

        builder.Property(e => e.InputContext)
            .HasColumnName("input_context")
            .HasColumnType("jsonb");

        builder.Property(e => e.Version)
            .HasColumnName("version")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(e => e.ScheduledJobId)
            .IsUnique()
            .HasDatabaseName("ix_scheduled_job_payloads_scheduled_job_id");
    }
}
