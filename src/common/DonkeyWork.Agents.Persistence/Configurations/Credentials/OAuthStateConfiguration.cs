using DonkeyWork.Agents.Persistence.Entities.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Credentials;

public class OAuthStateConfiguration : IEntityTypeConfiguration<OAuthStateEntity>
{
    public void Configure(EntityTypeBuilder<OAuthStateEntity> builder)
    {
        builder.ToTable("oauth_states", "credentials");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.State)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.CodeVerifier)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.ExpiresAt)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Unique index on state for fast lookup
        builder.HasIndex(e => e.State).IsUnique();

        // Index for cleanup of expired states
        builder.HasIndex(e => e.ExpiresAt);
    }
}
