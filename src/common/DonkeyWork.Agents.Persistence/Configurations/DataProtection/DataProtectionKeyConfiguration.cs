using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.DataProtection;

public class DataProtectionKeyConfiguration : IEntityTypeConfiguration<DataProtectionKey>
{
    public void Configure(EntityTypeBuilder<DataProtectionKey> builder)
    {
        // Use a dedicated schema for system-level tables
        builder.ToTable("data_protection_keys", "system");

        // DataProtectionKey has an int Id by default, keep it
        builder.HasKey(k => k.Id);

        // The Xml column stores the serialized key data
        builder.Property(k => k.Xml)
            .HasColumnType("text");

        builder.Property(k => k.FriendlyName)
            .HasMaxLength(500);
    }
}
