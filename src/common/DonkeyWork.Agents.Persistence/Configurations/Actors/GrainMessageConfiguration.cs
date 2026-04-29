using DonkeyWork.Agents.Persistence.Entities.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Actors;

public class GrainMessageConfiguration : IEntityTypeConfiguration<GrainMessageEntity>
{
    public void Configure(EntityTypeBuilder<GrainMessageEntity> builder)
    {
        builder.ToTable("grain_messages", "actors");

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

        builder.Property(e => e.SequenceNumber)
            .HasColumnName("sequence_number")
            .IsRequired();

        builder.Property(e => e.MessageJson)
            .HasColumnName("message")
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(e => e.TurnId)
            .HasColumnName("turn_id")
            .IsRequired();

        // Unique composite index for ordering within a grain
        builder.HasIndex(e => new { e.GrainKey, e.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("ix_grain_messages_grain_key_sequence_number");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_grain_messages_user_id");

        builder.HasIndex(e => new { e.GrainKey, e.TurnId })
            .HasDatabaseName("ix_grain_messages_grain_key_turn_id");
    }
}
