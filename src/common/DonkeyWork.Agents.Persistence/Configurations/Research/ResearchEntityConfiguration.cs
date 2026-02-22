using DonkeyWork.Agents.Persistence.Entities.Research;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Research;

public class ResearchEntityConfiguration : IEntityTypeConfiguration<ResearchEntity>
{
    public void Configure(EntityTypeBuilder<ResearchEntity> builder)
    {
        builder.ToTable("research", "research");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.Subject)
            .HasColumnName("subject")
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Content)
            .HasColumnName("content");

        builder.Property(e => e.Summary)
            .HasColumnName("summary");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(e => e.CompletionNotes)
            .HasColumnName("completion_notes");

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_research_user_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_research_status");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_research_created_at");

        // Relationships
        builder.HasMany(e => e.Notes)
            .WithOne(n => n.Research)
            .HasForeignKey(n => n.ResearchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Tags)
            .WithOne(t => t.Research)
            .HasForeignKey(t => t.ResearchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
