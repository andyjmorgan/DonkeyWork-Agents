using DonkeyWork.Agents.Persistence.Entities.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Actors;

public sealed class NaviSettingsConfiguration : IEntityTypeConfiguration<NaviSettingsEntity>
{
    public void Configure(EntityTypeBuilder<NaviSettingsEntity> builder)
    {
        builder.ToTable("navi_settings", "actors");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.ModelId).IsRequired().HasMaxLength(600);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.HasIndex(e => e.UserId).IsUnique();
    }
}
