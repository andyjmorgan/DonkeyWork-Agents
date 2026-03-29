using DonkeyWork.Agents.Persistence.Entities.A2a;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.A2a;

public class A2aServerHeaderConfigurationConfiguration : IEntityTypeConfiguration<A2aServerHeaderConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<A2aServerHeaderConfigurationEntity> builder)
    {
        builder.ToTable("header_configurations", "a2a", table =>
        {
            table.HasCheckConstraint(
                "ck_a2a_header_value_or_credential",
                "(header_value_encrypted IS NOT NULL AND credential_id IS NULL AND credential_field_type IS NULL) OR (header_value_encrypted IS NULL AND credential_id IS NOT NULL AND credential_field_type IS NOT NULL)");
        });

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.A2aServerConfigurationId)
            .HasColumnName("a2a_server_configuration_id")
            .IsRequired();

        builder.Property(e => e.HeaderName)
            .HasColumnName("header_name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.HeaderValueEncrypted)
            .HasColumnName("header_value_encrypted")
            .HasColumnType("bytea");

        builder.Property(e => e.CredentialId)
            .HasColumnName("credential_id");

        builder.Property(e => e.CredentialFieldType)
            .HasColumnName("credential_field_type")
            .HasMaxLength(50);

        builder.HasIndex(e => e.A2aServerConfigurationId)
            .HasDatabaseName("ix_a2a_header_configurations_a2a_server_configuration_id");

        builder.HasIndex(e => new { e.A2aServerConfigurationId, e.HeaderName })
            .IsUnique()
            .HasDatabaseName("ix_a2a_header_configurations_config_id_header_name");
    }
}
