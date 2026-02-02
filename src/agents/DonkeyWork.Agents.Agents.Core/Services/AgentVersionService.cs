using System.Text.Json;
using System.Text.RegularExpressions;
using DonkeyWork.Agents.Agents.Contracts.Models;
using DonkeyWork.Agents.Agents.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Registry;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Agents.Core.Services;

public partial class AgentVersionService : IAgentVersionService
{
    private readonly AgentsDbContext _dbContext;
    private readonly ILogger<AgentVersionService> _logger;

    public AgentVersionService(AgentsDbContext dbContext, ILogger<AgentVersionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<GetAgentVersionResponseV1> SaveDraftAsync(Guid agentId, SaveAgentVersionRequestV1 request, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving draft version for agent {AgentId}", agentId);

        // Check if agent exists
        var agentExists = await _dbContext.Agents.AnyAsync(a => a.Id == agentId, cancellationToken);
        if (!agentExists)
        {
            throw new InvalidOperationException($"Agent {agentId} not found");
        }

        // Validate graph structure and deserialize node configurations
        var nodeConfigurations = ValidateAndDeserializeNodeConfigurations(request);

        // Check if a draft already exists
        var existingDraft = await _dbContext.AgentVersions
            .FirstOrDefaultAsync(v => v.AgentId == agentId && v.IsDraft, cancellationToken);

        AgentVersionEntity version;

        if (existingDraft != null)
        {
            // Update existing draft
            existingDraft.InputSchema = request.InputSchema;
            existingDraft.OutputSchema = request.OutputSchema;
            existingDraft.ReactFlowData = request.ReactFlowData;
            existingDraft.NodeConfigurations = nodeConfigurations;
            existingDraft.UpdatedAt = DateTimeOffset.UtcNow;

            version = existingDraft;

            _logger.LogInformation("Updated existing draft version {VersionId}", version.Id);
        }
        else
        {
            // Create new draft
            var latestVersion = await _dbContext.AgentVersions
                .Where(v => v.AgentId == agentId)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            var nextVersionNumber = (latestVersion?.VersionNumber ?? 0) + 1;

            version = new AgentVersionEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AgentId = agentId,
                VersionNumber = nextVersionNumber,
                IsDraft = true,
                InputSchema = request.InputSchema,
                OutputSchema = request.OutputSchema,
                ReactFlowData = request.ReactFlowData,
                NodeConfigurations = nodeConfigurations,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.AgentVersions.Add(version);

            _logger.LogInformation("Created new draft version {VersionId} with version number {VersionNumber}", version.Id, nextVersionNumber);
        }

        // Update credential mappings
        if (request.CredentialMappings != null)
        {
            // Remove existing mappings
            var existingMappings = await _dbContext.AgentVersionCredentialMappings
                .Where(m => m.AgentVersionId == version.Id)
                .ToListAsync(cancellationToken);

            _dbContext.AgentVersionCredentialMappings.RemoveRange(existingMappings);

            // Add new mappings
            foreach (var mapping in request.CredentialMappings)
            {
                _dbContext.AgentVersionCredentialMappings.Add(new AgentVersionCredentialMappingEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    AgentVersionId = version.Id,
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

    public async Task<GetAgentVersionResponseV1> PublishAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing draft version for agent {AgentId}", agentId);

        var draft = await _dbContext.AgentVersions
            .FirstOrDefaultAsync(v => v.AgentId == agentId && v.IsDraft, cancellationToken);

        if (draft == null)
        {
            throw new InvalidOperationException($"No draft version found for agent {agentId}");
        }

        // Mark as published
        draft.IsDraft = false;
        draft.PublishedAt = DateTimeOffset.UtcNow;
        draft.UpdatedAt = DateTimeOffset.UtcNow;

        // Update agent's current version reference
        var agent = await _dbContext.Agents
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

    public async Task<GetAgentVersionResponseV1?> GetVersionAsync(Guid agentId, Guid versionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var version = await _dbContext.AgentVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == versionId && v.AgentId == agentId, cancellationToken);

        return version == null ? null : MapToResponse(version);
    }

    public async Task<IReadOnlyList<GetAgentVersionResponseV1>> GetVersionsAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var versions = await _dbContext.AgentVersions
            .AsNoTracking()
            .Where(v => v.AgentId == agentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

        return versions.Select(MapToResponse).ToList();
    }

    /// <summary>
    /// Validates the graph structure and deserializes node configurations.
    /// </summary>
    private Dictionary<Guid, NodeConfiguration> ValidateAndDeserializeNodeConfigurations(SaveAgentVersionRequestV1 request)
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

        // Build node type map from typed ReactFlow data
        var nodeTypeMap = nodes.ToDictionary(n => n.Id, n => n.Data.NodeType);

        // Deserialize node configurations with type discriminators
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

    private static GetAgentVersionResponseV1 MapToResponse(AgentVersionEntity version)
    {
        var registry = NodeConfigurationRegistry.Instance;

        return new GetAgentVersionResponseV1
        {
            Id = version.Id,
            AgentId = version.AgentId,
            VersionNumber = version.VersionNumber,
            IsDraft = version.IsDraft,
            InputSchema = version.InputSchema.RootElement.Clone(),
            OutputSchema = version.OutputSchema?.RootElement.Clone(),
            ReactFlowData = version.ReactFlowData,
            NodeConfigurations = JsonSerializer.SerializeToElement(version.NodeConfigurations, registry.JsonOptions),
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

            // Get type discriminator from enum
            var discriminator = nodeType.ToTypeDiscriminator();

            // Create enriched config with type discriminator
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
