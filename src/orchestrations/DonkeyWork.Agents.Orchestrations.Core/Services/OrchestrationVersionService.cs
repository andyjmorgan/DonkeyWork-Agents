using System.Text.Json;
using System.Text.RegularExpressions;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Registry;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Orchestrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orchestrations.Core.Services;

public partial class OrchestrationVersionService : IOrchestrationVersionService
{
    private readonly AgentsDbContext _dbContext;
    private readonly ILogger<OrchestrationVersionService> _logger;

    public OrchestrationVersionService(AgentsDbContext dbContext, ILogger<OrchestrationVersionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<GetOrchestrationVersionResponseV1> SaveDraftAsync(Guid agentId, SaveOrchestrationVersionRequestV1 request, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving draft version for agent {AgentId}", agentId);

        var agentExists = await _dbContext.Orchestrations.AnyAsync(a => a.Id == agentId, cancellationToken);
        if (!agentExists)
        {
            throw new InvalidOperationException($"Agent {agentId} not found");
        }

        var nodeConfigurations = ValidateAndDeserializeNodeConfigurations(request);

        var existingDraft = await _dbContext.OrchestrationVersions
            .FirstOrDefaultAsync(v => v.OrchestrationId == agentId && v.IsDraft, cancellationToken);

        OrchestrationVersionEntity version;

        if (existingDraft != null)
        {
            existingDraft.InputSchema = request.InputSchema;
            existingDraft.OutputSchema = request.OutputSchema;
            existingDraft.ReactFlowData = request.ReactFlowData;
            existingDraft.NodeConfigurations = nodeConfigurations;
            existingDraft.Interfaces = request.Interfaces;
            existingDraft.UpdatedAt = DateTimeOffset.UtcNow;

            version = existingDraft;

            _logger.LogInformation("Updated existing draft version {VersionId}", version.Id);
        }
        else
        {
            var latestVersion = await _dbContext.OrchestrationVersions
                .Where(v => v.OrchestrationId == agentId)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            var nextVersionNumber = (latestVersion?.VersionNumber ?? 0) + 1;

            version = new OrchestrationVersionEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrchestrationId = agentId,
                VersionNumber = nextVersionNumber,
                IsDraft = true,
                InputSchema = request.InputSchema,
                OutputSchema = request.OutputSchema,
                ReactFlowData = request.ReactFlowData,
                NodeConfigurations = nodeConfigurations,
                Interfaces = request.Interfaces,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.OrchestrationVersions.Add(version);

            _logger.LogInformation("Created new draft version {VersionId} with version number {VersionNumber}", version.Id, nextVersionNumber);
        }

        if (request.CredentialMappings != null)
        {
            var existingMappings = await _dbContext.OrchestrationVersionCredentialMappings
                .Where(m => m.OrchestrationVersionId == version.Id)
                .ToListAsync(cancellationToken);

            _dbContext.OrchestrationVersionCredentialMappings.RemoveRange(existingMappings);

            foreach (var mapping in request.CredentialMappings)
            {
                _dbContext.OrchestrationVersionCredentialMappings.Add(new OrchestrationVersionCredentialMappingEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    OrchestrationVersionId = version.Id,
                    NodeId = mapping.NodeId,
                    CredentialId = mapping.CredentialId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(version);
    }

    public async Task<GetOrchestrationVersionResponseV1> PublishAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing draft version for agent {AgentId}", agentId);

        var draft = await _dbContext.OrchestrationVersions
            .FirstOrDefaultAsync(v => v.OrchestrationId == agentId && v.IsDraft, cancellationToken);

        if (draft == null)
        {
            throw new InvalidOperationException($"No draft version found for agent {agentId}");
        }

        // Mark as published
        draft.IsDraft = false;
        draft.PublishedAt = DateTimeOffset.UtcNow;
        draft.UpdatedAt = DateTimeOffset.UtcNow;

        var agent = await _dbContext.Orchestrations
            .FirstOrDefaultAsync(a => a.Id == agentId, cancellationToken);

        if (agent != null)
        {
            agent.CurrentVersionId = draft.Id;
            agent.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Published version {VersionId} as version {VersionNumber}", draft.Id, draft.VersionNumber);

        return MapToResponse(draft);
    }

    public async Task<GetOrchestrationVersionResponseV1?> GetVersionAsync(Guid agentId, Guid versionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var version = await _dbContext.OrchestrationVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == versionId && v.OrchestrationId == agentId, cancellationToken);

        return version == null ? null : MapToResponse(version);
    }

    public async Task<IReadOnlyList<GetOrchestrationVersionResponseV1>> GetVersionsAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var versions = await _dbContext.OrchestrationVersions
            .AsNoTracking()
            .Where(v => v.OrchestrationId == agentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

        return versions.Select(MapToResponse).ToList();
    }

    /// <summary>
    /// Validates the graph structure and deserializes node configurations.
    /// </summary>
    private Dictionary<Guid, NodeConfiguration> ValidateAndDeserializeNodeConfigurations(SaveOrchestrationVersionRequestV1 request)
    {
        var reactFlowData = request.ReactFlowData;
        var nodes = reactFlowData.Nodes;
        var edges = reactFlowData.Edges;

        // 1. Validate exactly one start node
        var startNodes = nodes.Count(n => n.Data.NodeType == NodeType.Start);
        if (startNodes != 1)
        {
            throw new InvalidOperationException($"Graph must have exactly one Start node. Found: {startNodes}");
        }

        // 2. Validate exactly one end node
        var endNodes = nodes.Count(n => n.Data.NodeType == NodeType.End);
        if (endNodes != 1)
        {
            throw new InvalidOperationException($"Graph must have exactly one End node. Found: {endNodes}");
        }

        var nodeTypeMap = nodes.ToDictionary(n => n.Id, n => n.Data.NodeType);

        var nodeConfigurations = DeserializeNodeConfigurations(request.NodeConfigurations, nodeTypeMap);

        // 3. Validate node name uniqueness
        var nodeNames = nodeConfigurations.Values.Select(c => c.Name).ToList();
        var duplicates = nodeNames.GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException($"Duplicate node names found: {string.Join(", ", duplicates)}");
        }

        // 4. Validate node name format (a-z, A-Z, 0-9, -, _ only)
        var invalidNames = nodeNames
            .Where(name => !NodeNameRegex().IsMatch(name))
            .ToList();

        if (invalidNames.Count > 0)
        {
            throw new InvalidOperationException($"Invalid node names (must contain only letters, numbers, hyphens, or underscores): {string.Join(", ", invalidNames)}");
        }

        // 5. Validate configurations
        foreach (var (nodeId, config) in nodeConfigurations)
        {
            try
            {
                config.Validate();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Node '{nodeId}' validation failed: {ex.Message}", ex);
            }
        }

        // 6. Every ReactFlow node has configuration
        var nodeIds = nodes.Select(n => n.Id).ToHashSet();
        var configIds = nodeConfigurations.Keys.ToHashSet();
        var missingConfigs = nodeIds.Except(configIds).ToList();

        if (missingConfigs.Count > 0)
        {
            throw new InvalidOperationException($"Nodes missing configuration: {string.Join(", ", missingConfigs)}");
        }

        // 7. All edges reference existing nodes
        foreach (var edge in edges)
        {
            if (edge.Source == Guid.Empty || edge.Target == Guid.Empty)
            {
                throw new InvalidOperationException("Edge has empty source or target");
            }

            if (!nodeIds.Contains(edge.Source))
            {
                throw new InvalidOperationException($"Edge {edge.Id} references non-existent source node: {edge.Source}");
            }

            if (!nodeIds.Contains(edge.Target))
            {
                throw new InvalidOperationException($"Edge {edge.Id} references non-existent target node: {edge.Target}");
            }
        }

        return nodeConfigurations;
    }

    private static GetOrchestrationVersionResponseV1 MapToResponse(OrchestrationVersionEntity version)
    {
        var registry = NodeConfigurationRegistry.Instance;

        return new GetOrchestrationVersionResponseV1
        {
            Id = version.Id,
            OrchestrationId = version.OrchestrationId,
            VersionNumber = version.VersionNumber,
            IsDraft = version.IsDraft,
            InputSchema = version.InputSchema.RootElement.Clone(),
            OutputSchema = version.OutputSchema?.RootElement.Clone(),
            ReactFlowData = version.ReactFlowData,
            NodeConfigurations = JsonSerializer.SerializeToElement(version.NodeConfigurations, registry.JsonOptions),
            Interfaces = version.Interfaces,
            CreatedAt = version.CreatedAt,
            PublishedAt = version.PublishedAt
        };
    }

    /// <summary>
    /// Deserializes node configurations by injecting the type discriminator from ReactFlow node data.
    /// </summary>
    private static Dictionary<Guid, NodeConfiguration> DeserializeNodeConfigurations(
        JsonElement nodeConfigurationsElement,
        Dictionary<Guid, NodeType> nodeTypeMap)
    {
        var registry = NodeConfigurationRegistry.Instance;
        var result = new Dictionary<Guid, NodeConfiguration>();

        foreach (var property in nodeConfigurationsElement.EnumerateObject())
        {
            if (!Guid.TryParse(property.Name, out var nodeId))
            {
                throw new InvalidOperationException($"Invalid node ID format: '{property.Name}' - must be a GUID");
            }

            if (!nodeTypeMap.TryGetValue(nodeId, out var nodeType))
            {
                throw new InvalidOperationException($"Node configuration for '{nodeId}' has no corresponding ReactFlow node");
            }

            var discriminator = nodeType.ToTypeDiscriminator();

            var configWithType = new Dictionary<string, object> { ["type"] = discriminator };
            foreach (var configProperty in property.Value.EnumerateObject())
            {
                if (configProperty.Name == "type") continue;
                configWithType[configProperty.Name] = configProperty.Value;
            }

            var configJson = JsonSerializer.Serialize(configWithType, registry.JsonOptions);
            var config = JsonSerializer.Deserialize<NodeConfiguration>(configJson, registry.JsonOptions)
                ?? throw new InvalidOperationException($"Failed to deserialize configuration for node '{nodeId}'");

            result[nodeId] = config;
        }

        return result;
    }

    [GeneratedRegex(@"^[A-Za-z0-9_-]+$")]
    private static partial Regex NodeNameRegex();
}
