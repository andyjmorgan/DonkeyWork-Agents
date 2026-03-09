using DonkeyWork.Agents.Persistence.Entities.Prompts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Prompts;

public class PromptConfiguration : IEntityTypeConfiguration<PromptEntity>
{
    public void Configure(EntityTypeBuilder<PromptEntity> builder)
    {
        builder.ToTable("prompts", "prompts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(e => e.PromptType)
            .HasColumnName("prompt_type")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_prompts_user_id");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_prompts_created_at");

        builder.HasIndex(e => e.PromptType)
            .HasDatabaseName("ix_prompts_prompt_type");
    }
}
