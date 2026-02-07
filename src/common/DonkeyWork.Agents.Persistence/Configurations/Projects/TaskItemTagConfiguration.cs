using DonkeyWork.Agents.Persistence.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Projects;

public class TaskItemTagConfiguration : IEntityTypeConfiguration<TaskItemTagEntity>
{
    public void Configure(EntityTypeBuilder<TaskItemTagEntity> builder)
    {
        builder.ToTable("task_tags", "projects");

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
            .HasMaxLength(100);

        builder.Property(e => e.Color)
            .HasColumnName("color")
            .HasMaxLength(7);

        builder.Property(e => e.TaskItemId)
            .HasColumnName("task_item_id")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_task_tags_user_id");

        builder.HasIndex(e => e.TaskItemId)
            .HasDatabaseName("ix_task_tags_task_item_id");

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("ix_task_tags_name");
    }
}
