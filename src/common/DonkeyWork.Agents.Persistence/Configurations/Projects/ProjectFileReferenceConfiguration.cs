using DonkeyWork.Agents.Persistence.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Projects;

public class ProjectFileReferenceConfiguration : IEntityTypeConfiguration<ProjectFileReferenceEntity>
{
    public void Configure(EntityTypeBuilder<ProjectFileReferenceEntity> builder)
    {
        builder.ToTable("project_file_references", "projects");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.FilePath)
            .HasColumnName("file_path")
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(255);

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(e => e.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_project_file_references_user_id");

        builder.HasIndex(e => e.ProjectId)
            .HasDatabaseName("ix_project_file_references_project_id");
    }
}
