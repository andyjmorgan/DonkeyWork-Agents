using DonkeyWork.Agents.Persistence.Entities.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Credentials;

public class ExternalApiKeyConfiguration : IEntityTypeConfiguration<ExternalApiKeyEntity>
{
    public void Configure(EntityTypeBuilder<ExternalApiKeyEntity> builder)
    {
        builder.ToTable("external_api_keys", "credentials");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(255);

        // Encrypted using pgcrypto
        builder.Property(e => e.FieldsEncrypted)
            .IsRequired()
            .HasColumnType("bytea");

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.Provider });
    }
}
