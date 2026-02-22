using DonkeyWork.Agents.Persistence.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Projects;

public class NoteConfiguration : IEntityTypeConfiguration<NoteEntity>
{
    public void Configure(EntityTypeBuilder<NoteEntity> builder)
    {
        builder.ToTable("notes", "projects");

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

        builder.Property(e => e.Content)
            .HasColumnName("content");

        builder.Property(e => e.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(e => e.ProjectId)
            .HasColumnName("project_id");

        builder.Property(e => e.MilestoneId)
            .HasColumnName("milestone_id");

        builder.Property(e => e.ResearchId)
            .HasColumnName("research_id");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_notes_user_id");

        builder.HasIndex(e => e.ProjectId)
            .HasDatabaseName("ix_notes_project_id");

        builder.HasIndex(e => e.MilestoneId)
            .HasDatabaseName("ix_notes_milestone_id");

        builder.HasIndex(e => e.ResearchId)
            .HasDatabaseName("ix_notes_research_id");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_notes_created_at");

        // Relationships
        builder.HasMany(e => e.Tags)
            .WithOne(t => t.Note)
            .HasForeignKey(t => t.NoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
