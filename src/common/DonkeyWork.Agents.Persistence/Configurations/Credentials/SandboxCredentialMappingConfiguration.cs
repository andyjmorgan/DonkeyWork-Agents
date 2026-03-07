using DonkeyWork.Agents.Persistence.Entities.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Credentials;

public class SandboxCredentialMappingConfiguration : IEntityTypeConfiguration<SandboxCredentialMappingEntity>
{
    public void Configure(EntityTypeBuilder<SandboxCredentialMappingEntity> builder)
    {
        builder.ToTable("sandbox_credential_mappings", "credentials");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.BaseDomain)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.HeaderName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.HeaderValuePrefix)
            .HasMaxLength(50);

        builder.Property(e => e.CredentialId)
            .IsRequired();

        builder.Property(e => e.CredentialType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.HeaderValueFormat)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Raw");

        builder.Property(e => e.BasicAuthUsername)
            .HasMaxLength(255);

        builder.Property(e => e.CredentialFieldType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.BaseDomain, e.HeaderName }).IsUnique();
    }
}
