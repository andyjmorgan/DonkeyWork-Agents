using System.Text.Json;
using System.Text.RegularExpressions;
using DonkeyWork.Agents.Agents.Contracts.Models;
using DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Agents.Core.Services;

public class AgentVersionService : IAgentVersionService
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

        // Validate graph structure before saving
        ValidateGraphStructure(request);

        // Check if a draft already exists
        var existingDraft = await _dbContext.AgentVersions
            .FirstOrDefaultAsync(v => v.AgentId == agentId && v.IsDraft, cancellationToken);

        AgentVersionEntity version;

        if (existingDraft != null)
        {
            // Update existing draft
            existingDraft.InputSchema = request.InputSchema.GetRawText();
            existingDraft.OutputSchema = request.OutputSchema?.GetRawText();
            existingDraft.ReactFlowData = request.ReactFlowData.GetRawText();
            existingDraft.NodeConfigurations = request.NodeConfigurations.GetRawText();
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
                InputSchema = request.InputSchema.GetRawText(),
                OutputSchema = request.OutputSchema?.GetRawText(),
                ReactFlowData = request.ReactFlowData.GetRawText(),
                NodeConfigurations = request.NodeConfigurations.GetRawText(),
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

    private void ValidateGraphStructure(SaveAgentVersionRequestV1 request)
    {
        // Parse ReactFlow data
        var nodes = request.ReactFlowData.GetProperty("nodes").EnumerateArray().ToList();
        var edges = request.ReactFlowData.GetProperty("edges").EnumerateArray().ToList();

        // 1. Validate exactly one start node
        var startNodes = nodes.Count(n => n.GetProperty("type").GetString() == "start");
        if (startNodes != 1)
        {
            throw new InvalidOperationException($"Graph must have exactly one Start node. Found: {startNodes}");
        }

        // 2. Validate exactly one end node
        var endNodes = nodes.Count(n => n.GetProperty("type").GetString() == "end");
        if (endNodes != 1)
        {
            throw new InvalidOperationException($"Graph must have exactly one End node. Found: {endNodes}");
        }

        // Parse node configurations
        var nodeConfigurations = new Dictionary<string, NodeConfiguration>();
        foreach (var property in request.NodeConfigurations.EnumerateObject())
        {
            var nodeId = property.Name;
            var configJson = property.Value.GetRawText();

            // Deserialize based on type
            var type = property.Value.GetProperty("type").GetString();
            NodeConfiguration config = type switch
            {
                "start" => JsonSerializer.Deserialize<StartNodeConfiguration>(configJson)!,
                "model" => JsonSerializer.Deserialize<ModelNodeConfiguration>(configJson)!,
                "end" => JsonSerializer.Deserialize<EndNodeConfiguration>(configJson)!,
                _ => throw new InvalidOperationException($"Unknown node type: {type}")
            };

            nodeConfigurations[nodeId] = config;
        }

        // 3. Validate node name uniqueness
        var nodeNames = nodeConfigurations.Values.Select(c => c.Name).ToList();
        var duplicates = nodeNames.GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
        {
            throw new InvalidOperationException($"Duplicate node names found: {string.Join(", ", duplicates)}");
        }

        // 4. Validate node name format (lowercase a-z, 0-9, -, _ only)
        var invalidNames = nodeNames
            .Where(name => !Regex.IsMatch(name, @"^[a-z0-9_-]+$"))
            .ToList();

        if (invalidNames.Any())
        {
            throw new InvalidOperationException($"Invalid node names (must be lowercase a-z, 0-9, -, _): {string.Join(", ", invalidNames)}");
        }

        // 5. Validate required fields per node type
        foreach (var kvp in nodeConfigurations)
        {
            var nodeId = kvp.Key;
            var config = kvp.Value;

            if (string.IsNullOrWhiteSpace(config.Name))
            {
                throw new InvalidOperationException($"Node {nodeId} missing required field: name");
            }

            if (config is ModelNodeConfiguration modelConfig)
            {
                if (modelConfig.Provider == 0) // Default enum value
                {
                    throw new InvalidOperationException($"Model node {nodeId} missing required field: provider");
                }

                if (string.IsNullOrWhiteSpace(modelConfig.ModelId))
                {
                    throw new InvalidOperationException($"Model node {nodeId} missing required field: modelId");
                }

                if (modelConfig.CredentialId == Guid.Empty)
                {
                    throw new InvalidOperationException($"Model node {nodeId} missing required field: credentialId");
                }

                if (string.IsNullOrWhiteSpace(modelConfig.UserMessage))
                {
                    throw new InvalidOperationException($"Model node {nodeId} missing required field: userMessage");
                }
            }
        }

        // 6. Every ReactFlow node has configuration
        var nodeIds = nodes.Select(n => n.GetProperty("id").GetString()!).ToHashSet();
        var configIds = nodeConfigurations.Keys.ToHashSet();
        var missingConfigs = nodeIds.Except(configIds).ToList();

        if (missingConfigs.Any())
        {
            throw new InvalidOperationException($"Nodes missing configuration: {string.Join(", ", missingConfigs)}");
        }

        // 7. All edges reference existing nodes
        foreach (var edge in edges)
        {
            if (!edge.TryGetProperty("source", out var sourceProperty))
            {
                throw new InvalidOperationException("Edge missing 'source' property");
            }

            if (!edge.TryGetProperty("target", out var targetProperty))
            {
                throw new InvalidOperationException("Edge missing 'target' property");
            }

            var source = sourceProperty.GetString();
            var target = targetProperty.GetString();

            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
            {
                throw new InvalidOperationException("Edge has null or empty source or target");
            }

            if (!nodeIds.Contains(source))
            {
                var edgeId = edge.TryGetProperty("id", out var idProp) ? idProp.GetString() : "unknown";
                throw new InvalidOperationException($"Edge {edgeId} references non-existent source node: {source}");
            }

            if (!nodeIds.Contains(target))
            {
                var edgeId = edge.TryGetProperty("id", out var idProp) ? idProp.GetString() : "unknown";
                throw new InvalidOperationException($"Edge {edgeId} references non-existent target node: {target}");
            }
        }
    }

    private static GetAgentVersionResponseV1 MapToResponse(AgentVersionEntity version)
    {
        return new GetAgentVersionResponseV1
        {
            Id = version.Id,
            AgentId = version.AgentId,
            VersionNumber = version.VersionNumber,
            IsDraft = version.IsDraft,
            InputSchema = JsonDocument.Parse(version.InputSchema).RootElement,
            OutputSchema = version.OutputSchema != null ? JsonDocument.Parse(version.OutputSchema).RootElement : null,
            ReactFlowData = JsonDocument.Parse(version.ReactFlowData).RootElement,
            NodeConfigurations = JsonDocument.Parse(version.NodeConfigurations).RootElement,
            CreatedAt = version.CreatedAt,
            PublishedAt = version.PublishedAt
        };
    }
}
