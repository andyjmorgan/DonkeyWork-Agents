using DonkeyWork.Agents.Persistence.Entities.Mcp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Mcp;

/// <summary>
/// EF Core configuration for the MCP stdio configuration entity.
/// </summary>
public class McpStdioConfigurationConfiguration : IEntityTypeConfiguration<McpStdioConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<McpStdioConfigurationEntity> builder)
    {
        builder.ToTable("stdio_configurations", "mcp");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.McpServerConfigurationId)
            .HasColumnName("mcp_server_configuration_id")
            .IsRequired();

        builder.Property(e => e.Command)
            .HasColumnName("command")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Arguments)
            .HasColumnName("arguments")
            .IsRequired()
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(e => e.EnvironmentVariables)
            .HasColumnName("environment_variables")
            .IsRequired()
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(e => e.PreExecScripts)
            .HasColumnName("pre_exec_scripts")
            .IsRequired()
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(e => e.WorkingDirectory)
            .HasColumnName("working_directory")
            .HasMaxLength(1000);

        // Index on FK
        builder.HasIndex(e => e.McpServerConfigurationId)
            .IsUnique()
            .HasDatabaseName("ix_mcp_stdio_configurations_mcp_server_configuration_id");
    }
}
