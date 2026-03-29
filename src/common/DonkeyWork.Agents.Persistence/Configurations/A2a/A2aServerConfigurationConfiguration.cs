using DonkeyWork.Agents.Persistence.Entities.A2a;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.A2a;

public class A2aServerConfigurationConfiguration : IEntityTypeConfiguration<A2aServerConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<A2aServerConfigurationEntity> builder)
    {
        builder.ToTable("server_configurations", "a2a");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(e => e.Address)
            .HasColumnName("address")
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(e => e.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.ConnectToNavi)
            .HasColumnName("connect_to_navi")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_a2a_server_configurations_user_id");

        builder.HasIndex(e => new { e.UserId, e.Name })
            .IsUnique()
            .HasDatabaseName("ix_a2a_server_configurations_user_id_name");

        builder.HasIndex(e => new { e.UserId, e.IsEnabled })
            .HasDatabaseName("ix_a2a_server_configurations_user_id_is_enabled");

        builder.HasMany(e => e.HeaderConfigurations)
            .WithOne(h => h.A2aServerConfiguration)
            .HasForeignKey(h => h.A2aServerConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
