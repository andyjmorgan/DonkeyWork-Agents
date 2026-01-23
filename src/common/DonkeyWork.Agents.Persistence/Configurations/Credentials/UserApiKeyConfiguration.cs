using DonkeyWork.Agents.Persistence.Entities.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Credentials;

public class UserApiKeyConfiguration : IEntityTypeConfiguration<UserApiKeyEntity>
{
    public void Configure(EntityTypeBuilder<UserApiKeyEntity> builder)
    {
        builder.ToTable("user_api_keys", "credentials");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.EncryptedKey)
            .IsRequired()
            .HasColumnType("bytea");

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.UserId);
    }
}
