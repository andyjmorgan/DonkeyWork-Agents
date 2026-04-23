using DonkeyWork.Agents.Persistence.Entities.Tts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Tts;

public class TtsRecordingConfiguration : IEntityTypeConfiguration<TtsRecordingEntity>
{
    public void Configure(EntityTypeBuilder<TtsRecordingEntity> builder)
    {
        builder.ToTable("recordings", "tts");

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
            .HasMaxLength(500);

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .IsRequired();

        builder.Property(e => e.FilePath)
            .HasColumnName("file_path")
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.Transcript)
            .HasColumnName("transcript")
            .IsRequired();

        builder.Property(e => e.ContentType)
            .HasColumnName("content_type")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.SizeBytes)
            .HasColumnName("size_bytes")
            .IsRequired();

        builder.Property(e => e.Voice)
            .HasColumnName("voice")
            .HasMaxLength(50);

        builder.Property(e => e.Model)
            .HasColumnName("model")
            .HasMaxLength(50);

        builder.Property(e => e.OrchestrationExecutionId)
            .HasColumnName("orchestration_execution_id");

        builder.Property(e => e.CollectionId)
            .HasColumnName("collection_id");

        builder.Property(e => e.SequenceNumber)
            .HasColumnName("sequence_number");

        builder.Property(e => e.ChapterTitle)
            .HasColumnName("chapter_title")
            .HasMaxLength(500);

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Progress)
            .HasColumnName("progress")
            .IsRequired();

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.OrchestrationExecutionId);
        builder.HasIndex(e => new { e.CollectionId, e.SequenceNumber });

        builder.HasOne(e => e.Playback)
            .WithOne(p => p.Recording)
            .HasForeignKey<TtsPlaybackEntity>(p => p.RecordingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
