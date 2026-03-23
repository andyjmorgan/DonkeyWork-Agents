using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.AgentDefinitions.Contracts.Models;
using DonkeyWork.Agents.AgentDefinitions.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Tools.Swarm;

public sealed class SwarmAgentSpawnTools
{
    private readonly IAgentDefinitionService _agentDefinitionService;
    private readonly SwarmAgentSpawner _spawner;

    private static readonly JsonSerializerOptions ContractJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        AllowOutOfOrderMetadataProperties = true,
    };

    public SwarmAgentSpawnTools(
        IAgentDefinitionService agentDefinitionService,
        SwarmAgentSpawner spawner)
    {
        _agentDefinitionService = agentDefinitionService;
        _spawner = spawner;
    }

    [AgentTool(ToolNames.SpawnAgent)]
    [Description("Spawn an agent by name. The agent will execute the task using its configured model, tools, and system prompt.")]
    public async Task<ToolResult> SpawnAgent(
        [Description("The name of the agent to spawn")]
        string agent_name,
        [Description("Detailed instructions for the task the agent should perform")]
        string task,
        [Description("A short label describing this agent's task")]
        string label,
        GrainContext context,
        IIdentityContext identityContext,
        CancellationToken ct)
    {
        // First check sub-agents on the current contract (looked up by name, fetched by ID)
        var subAgentRef = context.SubAgents.FirstOrDefault(s =>
            string.Equals(s.Name, agent_name, StringComparison.OrdinalIgnoreCase));

        NaviAgentDefinitionV1? definition = null;

        if (subAgentRef is not null && Guid.TryParse(subAgentRef.Id, out var subAgentId))
        {
            var details = await _agentDefinitionService.GetByIdAsync(subAgentId, ct);
            if (details is not null)
            {
                definition = new NaviAgentDefinitionV1
                {
                    Id = details.Id,
                    Name = details.Name,
                    Description = details.Description,
                    Icon = details.Icon,
                    Contract = details.Contract,
                };
            }
        }

        // Fall back to Navi-connected agents
        if (definition is null)
        {
            var naviAgents = await _agentDefinitionService.GetNaviConnectedAsync(ct);
            definition = naviAgents.FirstOrDefault(a =>
                string.Equals(a.Name, agent_name, StringComparison.OrdinalIgnoreCase));

            if (definition is null)
            {
                var available = context.SubAgents.Select(s => s.Name)
                    .Concat(naviAgents.Select(a => a.Name))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
                return ToolResult.Error($"Agent '{agent_name}' not found. Available agents: {string.Join(", ", available)}");
            }
        }

        AgentContract contract;
        try
        {
            contract = JsonSerializer.Deserialize<AgentContract>(definition.Contract.GetRawText(), ContractJsonOptions)
                       ?? throw new JsonException("Deserialized contract was null.");
        }
        catch (JsonException ex)
        {
            return ToolResult.Error($"Failed to parse agent contract: {ex.Message}");
        }

        // Override key fields for agent spawning
        contract = new AgentContract
        {
            SystemPrompt = contract.SystemPrompt,
            ToolGroups = contract.ToolGroups,
            MaxTokens = contract.MaxTokens,
            ThinkingBudgetTokens = contract.ThinkingBudgetTokens,
            Stream = contract.Stream,
            WebSearch = contract.WebSearch,
            WebFetch = contract.WebFetch,
            PersistMessages = contract.PersistMessages,
            Lifecycle = AgentLifecycle.Task,
            LingerSeconds = contract.LingerSeconds,
            AgentType = AgentTypes.Agent,
            KeyPrefix = AgentKeys.AgentPrefix,
            TimeoutSeconds = contract.TimeoutSeconds,
            McpServers = contract.McpServers,
            EnableSandbox = contract.EnableSandbox,
            SandboxPodName = contract.SandboxPodName,
            ModelId = contract.ModelId,
            Prompts = contract.Prompts,
            SubAgents = contract.SubAgents,
            ReasoningEffort = contract.ReasoningEffort,
            DisplayName = contract.DisplayName ?? definition.Name,
            Icon = definition.Icon,
            AllowDelegation = contract.AllowDelegation,
        };

        contract = contract.WithParentContext(context);

        return await _spawner.SpawnAsync(contract, task, label, context, identityContext, ct);
    }
}
