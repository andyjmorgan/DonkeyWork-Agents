using DonkeyWork.Agents.Persistence.Entities.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Providers;

public sealed class CustomModelConfiguration : IEntityTypeConfiguration<CustomModelEntity>
{
    public void Configure(EntityTypeBuilder<CustomModelEntity> builder)
    {
        builder.ToTable("custom_models", "providers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Endpoint).IsRequired().HasMaxLength(2000);
        builder.Property(e => e.WireFormat).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ModelName).IsRequired().HasMaxLength(500);
        builder.Property(e => e.ApiKeyEncrypted).HasColumnType("text");
        builder.Property(e => e.MaxInputTokens).IsRequired();
        builder.Property(e => e.MaxOutputTokens).IsRequired();
        builder.Property(e => e.SupportsTools).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.HasIndex(e => new { e.UserId, e.Name });
    }
}
