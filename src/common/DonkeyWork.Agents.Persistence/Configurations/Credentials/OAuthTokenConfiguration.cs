using DonkeyWork.Agents.Persistence.Entities.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Credentials;

public class OAuthTokenConfiguration : IEntityTypeConfiguration<OAuthTokenEntity>
{
    public void Configure(EntityTypeBuilder<OAuthTokenEntity> builder)
    {
        builder.ToTable("oauth_tokens", "credentials");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.ExternalUserId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(255);

        // Encrypted using pgcrypto
        builder.Property(e => e.AccessTokenEncrypted)
            .IsRequired()
            .HasColumnType("bytea");

        builder.Property(e => e.RefreshTokenEncrypted)
            .IsRequired()
            .HasColumnType("bytea");

        builder.Property(e => e.ScopesJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(e => e.ExpiresAt)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.Provider }).IsUnique();
        builder.HasIndex(e => e.ExpiresAt);
    }
}
