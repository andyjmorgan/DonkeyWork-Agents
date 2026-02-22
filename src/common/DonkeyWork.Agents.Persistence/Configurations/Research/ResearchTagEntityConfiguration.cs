using DonkeyWork.Agents.Persistence.Entities.Research;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Research;

public class ResearchTagEntityConfiguration : IEntityTypeConfiguration<ResearchTagEntity>
{
    public void Configure(EntityTypeBuilder<ResearchTagEntity> builder)
    {
        builder.ToTable("research_tags", "research");

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

        builder.Property(e => e.ResearchId)
            .HasColumnName("research_id")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_research_tags_user_id");

        builder.HasIndex(e => e.ResearchId)
            .HasDatabaseName("ix_research_tags_research_id");

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("ix_research_tags_name");
    }
}
