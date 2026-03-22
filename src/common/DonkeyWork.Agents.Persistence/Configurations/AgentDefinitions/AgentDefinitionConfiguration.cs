using System.Text.Json;
using DonkeyWork.Agents.Persistence.Entities.AgentDefinitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.AgentDefinitions;

public class AgentDefinitionConfiguration : IEntityTypeConfiguration<AgentDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<AgentDefinitionEntity> builder)
    {
        builder.ToTable("agent_definitions", "agent_definitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.IsSystem)
            .HasColumnName("is_system")
            .IsRequired();

        // Contract - JsonDocument stored as JSONB
        builder.Property(e => e.Contract)
            .HasColumnName("contract")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonDocumentToString(v),
                v => JsonDocument.Parse(v, default))
            .IsRequired();

        // ReactFlowData - nullable JsonDocument stored as JSONB
        builder.Property(e => e.ReactFlowData)
            .HasColumnName("react_flow_data")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonDocumentToString(v),
                v => v == null ? null : JsonDocument.Parse(v, default));

        // NodeConfigurations - nullable JsonDocument stored as JSONB
        builder.Property(e => e.NodeConfigurations)
            .HasColumnName("node_configurations")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonDocumentToString(v),
                v => v == null ? null : JsonDocument.Parse(v, default));

        builder.Property(e => e.ConnectToNavi)
            .HasColumnName("connect_to_navi")
            .HasDefaultValue(false);

        builder.Property(e => e.Icon)
            .HasColumnName("icon");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_agent_definitions_user_id");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_agent_definitions_created_at");
    }

    private static string JsonDocumentToString(JsonDocument doc)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        doc.WriteTo(writer);
        writer.Flush();
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}
