using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Registry;
using DonkeyWork.Agents.Persistence.Entities.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Agents;

public class AgentVersionConfiguration : IEntityTypeConfiguration<AgentVersionEntity>
{
    public void Configure(EntityTypeBuilder<AgentVersionEntity> builder)
    {
        builder.ToTable("agent_versions", "agents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.AgentId)
            .HasColumnName("agent_id")
            .IsRequired();

        builder.Property(e => e.VersionNumber)
            .HasColumnName("version_number")
            .IsRequired();

        builder.Property(e => e.IsDraft)
            .HasColumnName("is_draft")
            .IsRequired();

        // InputSchema - JsonDocument stored as JSONB
        builder.Property(e => e.InputSchema)
            .HasColumnName("input_schema")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonDocumentToString(v),
                v => JsonDocument.Parse(v, default))
            .IsRequired();

        // OutputSchema - nullable JsonDocument stored as JSONB
        builder.Property(e => e.OutputSchema)
            .HasColumnName("output_schema")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonDocumentToString(v),
                v => v == null ? null : JsonDocument.Parse(v, default));

        // ReactFlowData - typed object stored as JSONB
        var reactFlowComparer = new ValueComparer<ReactFlowData>(
            (l, r) => JsonSerializer.Serialize(l, JsonOptions) == JsonSerializer.Serialize(r, JsonOptions),
            v => JsonSerializer.Serialize(v, JsonOptions).GetHashCode(),
            v => JsonSerializer.Deserialize<ReactFlowData>(JsonSerializer.Serialize(v, JsonOptions), JsonOptions)!);

        builder.Property(e => e.ReactFlowData)
            .HasColumnName("react_flow_data")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<ReactFlowData>(v, JsonOptions)!)
            .Metadata.SetValueComparer(reactFlowComparer);

        // NodeConfigurations - polymorphic dictionary stored as JSONB
        var nodeConfigComparer = new ValueComparer<Dictionary<Guid, NodeConfiguration>>(
            (l, r) => JsonSerializer.Serialize(l, RegistryJsonOptions) == JsonSerializer.Serialize(r, RegistryJsonOptions),
            v => JsonSerializer.Serialize(v, RegistryJsonOptions).GetHashCode(),
            v => JsonSerializer.Deserialize<Dictionary<Guid, NodeConfiguration>>(
                JsonSerializer.Serialize(v, RegistryJsonOptions), RegistryJsonOptions)!);

        builder.Property(e => e.NodeConfigurations)
            .HasColumnName("node_configurations")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, RegistryJsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<Guid, NodeConfiguration>>(v, RegistryJsonOptions)!)
            .Metadata.SetValueComparer(nodeConfigComparer);

        builder.Property(e => e.PublishedAt)
            .HasColumnName("published_at");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_agent_versions_user_id");

        builder.HasIndex(e => e.AgentId)
            .HasDatabaseName("ix_agent_versions_agent_id");

        builder.HasIndex(e => new { e.AgentId, e.IsDraft })
            .HasDatabaseName("ix_agent_versions_agent_id_is_draft");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_agent_versions_created_at");

        // Relationships
        builder.HasMany(e => e.CredentialMappings)
            .WithOne(m => m.AgentVersion)
            .HasForeignKey(m => m.AgentVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Executions)
            .WithOne(ex => ex.AgentVersion)
            .HasForeignKey(ex => ex.AgentVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // Use registry options for polymorphic NodeConfiguration serialization
    private static JsonSerializerOptions RegistryJsonOptions => NodeConfigurationRegistry.Instance.JsonOptions;

    private static string JsonDocumentToString(JsonDocument doc)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        doc.WriteTo(writer);
        writer.Flush();
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}
