using DonkeyWork.Agents.Persistence.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Projects;

public class MilestoneConfiguration : IEntityTypeConfiguration<MilestoneEntity>
{
    public void Configure(EntityTypeBuilder<MilestoneEntity> builder)
    {
        builder.ToTable("milestones", "projects");

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

        builder.Property(e => e.Content)
            .HasColumnName("content");

        builder.Property(e => e.SuccessCriteria)
            .HasColumnName("success_criteria");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(e => e.CompletionNotes)
            .HasColumnName("completion_notes");

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.DueDate)
            .HasColumnName("due_date");

        builder.Property(e => e.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(e => e.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_milestones_user_id");

        builder.HasIndex(e => e.ProjectId)
            .HasDatabaseName("ix_milestones_project_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_milestones_status");

        builder.HasIndex(e => e.DueDate)
            .HasDatabaseName("ix_milestones_due_date");

        // Relationships
        builder.HasMany(e => e.TaskItems)
            .WithOne(t => t.Milestone)
            .HasForeignKey(t => t.MilestoneId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Notes)
            .WithOne(n => n.Milestone)
            .HasForeignKey(n => n.MilestoneId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Tags)
            .WithOne(t => t.Milestone)
            .HasForeignKey(t => t.MilestoneId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.FileReferences)
            .WithOne(f => f.Milestone)
            .HasForeignKey(f => f.MilestoneId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
