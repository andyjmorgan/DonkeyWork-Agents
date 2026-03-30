using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Registry;
using DonkeyWork.Agents.Persistence.Entities.Orchestrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Orchestrations;

public class OrchestrationVersionConfiguration : IEntityTypeConfiguration<OrchestrationVersionEntity>
{
    public void Configure(EntityTypeBuilder<OrchestrationVersionEntity> builder)
    {
        builder.ToTable("orchestration_versions", "orchestrations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.OrchestrationId)
            .HasColumnName("orchestration_id")
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

        // Interface - polymorphic type stored as JSONB
        var interfaceComparer = new ValueComparer<InterfaceConfig>(
            (l, r) => JsonSerializer.Serialize(l, InterfaceJsonOptions) == JsonSerializer.Serialize(r, InterfaceJsonOptions),
            v => JsonSerializer.Serialize(v, InterfaceJsonOptions).GetHashCode(),
            v => JsonSerializer.Deserialize<InterfaceConfig>(JsonSerializer.Serialize(v, InterfaceJsonOptions), InterfaceJsonOptions)!);

        builder.Property(e => e.Interface)
            .HasColumnName("interface")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, InterfaceJsonOptions),
                v => DeserializeInterface(v))
            .Metadata.SetValueComparer(interfaceComparer);

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
            .HasDatabaseName("ix_orchestration_versions_user_id");

        builder.HasIndex(e => e.OrchestrationId)
            .HasDatabaseName("ix_orchestration_versions_orchestration_id");

        builder.HasIndex(e => new { e.OrchestrationId, e.IsDraft })
            .HasDatabaseName("ix_orchestration_versions_orchestration_id_is_draft");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_orchestration_versions_created_at");

        // Relationships
        builder.HasMany(e => e.CredentialMappings)
            .WithOne(m => m.OrchestrationVersion)
            .HasForeignKey(m => m.OrchestrationVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Executions)
            .WithOne(ex => ex.OrchestrationVersion)
            .HasForeignKey(ex => ex.OrchestrationVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // Options for polymorphic InterfaceConfig serialization
    private static readonly JsonSerializerOptions InterfaceJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        AllowOutOfOrderMetadataProperties = true
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

    /// <summary>
    /// Deserializes InterfaceConfig, handling both new format (with type discriminator)
    /// and legacy format (OrchestrationInterfaces with mcp/chat/a2a/webhook properties).
    /// </summary>
    private static InterfaceConfig DeserializeInterface(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("type", out _))
        {
            return JsonSerializer.Deserialize<InterfaceConfig>(json, InterfaceJsonOptions)
                ?? new DirectInterfaceConfig();
        }

        // Legacy format: OrchestrationInterfaces with optional mcp/chat/a2a/webhook properties
        // Determine which interface was enabled and migrate to new format
        if (root.TryGetProperty("chat", out var chatProp) && chatProp.ValueKind != JsonValueKind.Null)
        {
            return JsonSerializer.Deserialize<ChatInterfaceConfig>(chatProp.GetRawText(), InterfaceJsonOptions)
                ?? new ChatInterfaceConfig();
        }

        if (root.TryGetProperty("mcp", out var mcpProp) && mcpProp.ValueKind != JsonValueKind.Null)
        {
            return JsonSerializer.Deserialize<McpInterfaceConfig>(mcpProp.GetRawText(), InterfaceJsonOptions)
                ?? new McpInterfaceConfig();
        }

        if (root.TryGetProperty("a2a", out var a2aProp) && a2aProp.ValueKind != JsonValueKind.Null)
        {
            return JsonSerializer.Deserialize<A2aInterfaceConfig>(a2aProp.GetRawText(), InterfaceJsonOptions)
                ?? new A2aInterfaceConfig();
        }

        if (root.TryGetProperty("webhook", out var webhookProp) && webhookProp.ValueKind != JsonValueKind.Null)
        {
            return JsonSerializer.Deserialize<WebhookInterfaceConfig>(webhookProp.GetRawText(), InterfaceJsonOptions)
                ?? new WebhookInterfaceConfig();
        }

        // Default to Direct interface
        return new DirectInterfaceConfig();
    }
}
