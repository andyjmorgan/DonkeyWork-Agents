using DonkeyWork.Agents.Persistence.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Projects;

public class TodoTagConfiguration : IEntityTypeConfiguration<TodoTagEntity>
{
    public void Configure(EntityTypeBuilder<TodoTagEntity> builder)
    {
        builder.ToTable("todo_tags", "projects");

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

        builder.Property(e => e.TodoId)
            .HasColumnName("todo_id")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_todo_tags_user_id");

        builder.HasIndex(e => e.TodoId)
            .HasDatabaseName("ix_todo_tags_todo_id");

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("ix_todo_tags_name");
    }
}
