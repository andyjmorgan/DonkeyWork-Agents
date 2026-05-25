using DonkeyWork.Agents.Persistence.Entities.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Actors;

public class GrainCompactionMarkerConfiguration : IEntityTypeConfiguration<GrainCompactionMarkerEntity>
{
    public void Configure(EntityTypeBuilder<GrainCompactionMarkerEntity> builder)
    {
        builder.ToTable("grain_compaction_markers", "actors");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.GrainKey)
            .HasColumnName("grain_key")
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.AtSequenceNumber)
            .HasColumnName("at_sequence_number")
            .IsRequired();

        builder.Property(e => e.AtTurnId)
            .HasColumnName("at_turn_id")
            .IsRequired();

        builder.Property(e => e.Summary)
            .HasColumnName("summary")
            .IsRequired()
            .HasColumnType("text");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(e => new { e.GrainKey, e.AtSequenceNumber })
            .HasDatabaseName("ix_grain_compaction_markers_grain_key_at_sequence_number");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_grain_compaction_markers_user_id");
    }
}
