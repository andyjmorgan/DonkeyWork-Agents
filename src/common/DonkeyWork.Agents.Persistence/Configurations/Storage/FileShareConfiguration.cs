using DonkeyWork.Agents.Persistence.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Storage;

public class FileShareConfiguration : IEntityTypeConfiguration<FileShareEntity>
{
    public void Configure(EntityTypeBuilder<FileShareEntity> builder)
    {
        builder.ToTable("file_shares", "storage");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.FileId)
            .IsRequired();

        builder.Property(e => e.ShareToken)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.ExpiresAt)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.PasswordHash)
            .HasMaxLength(255);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.ShareToken).IsUnique();
        builder.HasIndex(e => e.FileId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.ExpiresAt);
    }
}
