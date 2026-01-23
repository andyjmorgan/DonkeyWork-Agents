using DonkeyWork.Agents.Persistence.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Storage;

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFileEntity>
{
    public void Configure(EntityTypeBuilder<StoredFileEntity> builder)
    {
        builder.ToTable("stored_files", "storage");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.ContentType)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.SizeBytes)
            .IsRequired();

        builder.Property(e => e.BucketName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.ObjectKey)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(e => e.ChecksumSha256)
            .HasMaxLength(64);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Metadata)
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.ObjectKey).IsUnique();
        builder.HasIndex(e => e.Status);

        builder.HasMany(e => e.Shares)
            .WithOne(s => s.File)
            .HasForeignKey(s => s.FileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
