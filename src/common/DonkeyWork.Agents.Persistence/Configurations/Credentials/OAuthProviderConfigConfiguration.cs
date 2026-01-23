using DonkeyWork.Agents.Persistence.Entities.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Credentials;

public class OAuthProviderConfigConfiguration : IEntityTypeConfiguration<OAuthProviderConfigEntity>
{
    public void Configure(EntityTypeBuilder<OAuthProviderConfigEntity> builder)
    {
        builder.ToTable("oauth_provider_configs", "credentials");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // Encrypted using pgcrypto
        builder.Property(e => e.ClientIdEncrypted)
            .IsRequired()
            .HasColumnType("bytea");

        builder.Property(e => e.ClientSecretEncrypted)
            .IsRequired()
            .HasColumnType("bytea");

        builder.Property(e => e.RedirectUri)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.Provider }).IsUnique();
    }
}
