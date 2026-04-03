using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Contracts.Services;
using DonkeyWork.Agents.AgentDefinitions.Contracts.Services;
using DonkeyWork.Agents.A2a.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Services;

public sealed class ConversationContractHydrator : IConversationContractHydrator
{
    private readonly IMcpServerConfigurationService _mcpServerConfigService;
    private readonly IA2aServerConfigurationService _a2aServerConfigService;
    private readonly IAgentDefinitionService _agentDefinitionService;
    private readonly IOrchestrationService _orchestrationService;
    private readonly ILogger<ConversationContractHydrator> _logger;

    public ConversationContractHydrator(
        IMcpServerConfigurationService mcpServerConfigService,
        IA2aServerConfigurationService a2aServerConfigService,
        IAgentDefinitionService agentDefinitionService,
        IOrchestrationService orchestrationService,
        ILogger<ConversationContractHydrator> logger)
    {
        _mcpServerConfigService = mcpServerConfigService;
        _a2aServerConfigService = a2aServerConfigService;
        _agentDefinitionService = agentDefinitionService;
        _orchestrationService = orchestrationService;
        _logger = logger;
    }

    public async Task<AgentContract> HydrateAsync(AgentContract baseContract, CancellationToken ct = default)
    {
        var mcpTask = DiscoverMcpServersAsync(ct);
        var a2aTask = DiscoverA2aServersAsync(ct);
        var agentsTask = DiscoverSubAgentsAsync(ct);
        var orchestrationsTask = DiscoverOrchestrationsAsync(ct);

        await Task.WhenAll(mcpTask, a2aTask, agentsTask, orchestrationsTask);

        var mcpServers = await mcpTask;
        var a2aServers = await a2aTask;
        var subAgents = await agentsTask;
        var orchestrations = await orchestrationsTask;

        var enableSandbox = mcpServers.Length > 0
            || baseContract.ToolGroups.Contains(ToolGroupNames.Sandbox, StringComparer.OrdinalIgnoreCase);

        var allowDelegation = baseContract.ToolGroups.Contains(
            ToolGroupNames.SwarmDelegate, StringComparer.OrdinalIgnoreCase);

        return new AgentContract
        {
            SystemPrompt = baseContract.SystemPrompt,
            ToolGroups = baseContract.ToolGroups,
            MaxTokens = baseContract.MaxTokens,
            ThinkingBudgetTokens = baseContract.ThinkingBudgetTokens,
            Stream = baseContract.Stream,
            WebSearch = baseContract.WebSearch,
            WebFetch = baseContract.WebFetch,
            PersistMessages = baseContract.PersistMessages,
            Lifecycle = baseContract.Lifecycle,
            LingerSeconds = baseContract.LingerSeconds,
            AgentType = baseContract.AgentType,
            KeyPrefix = baseContract.KeyPrefix,
            TimeoutSeconds = baseContract.TimeoutSeconds,
            EnableSandbox = enableSandbox,
            SandboxPodName = baseContract.SandboxPodName,
            ModelId = baseContract.ModelId,
            Prompts = baseContract.Prompts,
            ReasoningEffort = baseContract.ReasoningEffort,
            ToolConfiguration = baseContract.ToolConfiguration,
            DisplayName = baseContract.DisplayName,
            Icon = baseContract.Icon,
            ContextManagement = baseContract.ContextManagement,
            McpServers = mcpServers,
            A2aServers = a2aServers,
            SubAgents = subAgents,
            Orchestrations = orchestrations,
            AllowDelegation = allowDelegation,
        };
    }

    private async Task<McpServerReference[]> DiscoverMcpServersAsync(CancellationToken ct)
    {
        try
        {
            var httpConfigs = await _mcpServerConfigService.GetNaviConnectionConfigsAsync(ct);
            var stdioConfigs = await _mcpServerConfigService.GetNaviStdioConfigsAsync(ct);

            var references = httpConfigs
                .Select(c => new McpServerReference { Id = c.Id.ToString(), Name = c.Name, Description = c.Description })
                .Concat(stdioConfigs.Select(c => new McpServerReference { Id = c.Id.ToString(), Name = c.Name, Description = c.Description }))
                .ToArray();

            if (references.Length > 0)
                _logger.LogInformation("Hydrated {Count} MCP servers for conversation contract", references.Length);

            return references;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover MCP servers during contract hydration");
            return [];
        }
    }

    private async Task<A2aServerReference[]> DiscoverA2aServersAsync(CancellationToken ct)
    {
        try
        {
            var configs = await _a2aServerConfigService.GetNaviConnectionConfigsAsync(ct);

            return configs
                .Select(c => new A2aServerReference { Id = c.Id.ToString(), Name = c.Name, Description = c.Description })
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover A2A servers during contract hydration");
            return [];
        }
    }

    private async Task<SubAgentReference[]> DiscoverSubAgentsAsync(CancellationToken ct)
    {
        try
        {
            var agents = await _agentDefinitionService.GetNaviConnectedAsync(ct);

            return agents
                .Select(a => new SubAgentReference { Id = a.Id.ToString(), Name = a.Name, Description = a.Description })
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover sub-agents during contract hydration");
            return [];
        }
    }

    private async Task<OrchestrationReference[]> DiscoverOrchestrationsAsync(CancellationToken ct)
    {
        try
        {
            var toolEnabled = await _orchestrationService.ListToolEnabledAsync(ct);

            return toolEnabled
                .Select(t => new OrchestrationReference
                {
                    Id = t.Orchestration.Id.ToString(),
                    Name = t.Orchestration.Name,
                    Description = t.Orchestration.Description,
                    ToolName = t.Orchestration.FriendlyName,
                    VersionId = t.Version.Id.ToString(),
                })
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover orchestrations during contract hydration");
            return [];
        }
    }
}
