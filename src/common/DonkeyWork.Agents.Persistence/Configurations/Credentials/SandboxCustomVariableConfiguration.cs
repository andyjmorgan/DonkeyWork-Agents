using DonkeyWork.Agents.Persistence.Entities.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Credentials;

public class SandboxCustomVariableConfiguration : IEntityTypeConfiguration<SandboxCustomVariableEntity>
{
    public void Configure(EntityTypeBuilder<SandboxCustomVariableEntity> builder)
    {
        builder.ToTable("sandbox_custom_variables", "credentials");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.Key)
            .IsRequired()
            .HasMaxLength(255);

        // Encrypted using pgcrypto
        builder.Property(e => e.Value)
            .IsRequired()
            .HasColumnType("bytea");

        builder.Property(e => e.IsSecret)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.Key }).IsUnique();
    }
}
