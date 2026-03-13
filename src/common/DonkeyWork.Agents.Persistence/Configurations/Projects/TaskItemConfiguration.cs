using DonkeyWork.Agents.Persistence.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Projects;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItemEntity>
{
    public void Configure(EntityTypeBuilder<TaskItemEntity> builder)
    {
        builder.ToTable("tasks", "projects");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(e => e.Priority)
            .HasColumnName("priority")
            .IsRequired();

        builder.Property(e => e.CompletionNotes)
            .HasColumnName("completion_notes");

        builder.Property(e => e.DueDate)
            .HasColumnName("due_date");

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(e => e.ProjectId)
            .HasColumnName("project_id");

        builder.Property(e => e.MilestoneId)
            .HasColumnName("milestone_id");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_tasks_user_id");

        builder.HasIndex(e => e.ProjectId)
            .HasDatabaseName("ix_tasks_project_id");

        builder.HasIndex(e => e.MilestoneId)
            .HasDatabaseName("ix_tasks_milestone_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_tasks_status");

        builder.HasIndex(e => e.Priority)
            .HasDatabaseName("ix_tasks_priority");

        builder.HasIndex(e => e.DueDate)
            .HasDatabaseName("ix_tasks_due_date");

        // Relationships
        builder.HasMany(e => e.Tags)
            .WithOne(t => t.TaskItem)
            .HasForeignKey(t => t.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
