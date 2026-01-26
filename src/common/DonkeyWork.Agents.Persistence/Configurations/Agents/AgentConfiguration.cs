using DonkeyWork.Agents.Persistence.Entities.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Agents;

public class AgentConfiguration : IEntityTypeConfiguration<AgentEntity>
{
    public void Configure(EntityTypeBuilder<AgentEntity> builder)
    {
        builder.ToTable("agents", "agents");

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

        builder.Property(e => e.CurrentVersionId)
            .HasColumnName("current_version_id");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_agents_user_id");

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("ix_agents_name");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_agents_created_at");

        // Relationships
        builder.HasMany(e => e.Versions)
            .WithOne(v => v.Agent)
            .HasForeignKey(v => v.AgentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CurrentVersion)
            .WithMany()
            .HasForeignKey(e => e.CurrentVersionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
