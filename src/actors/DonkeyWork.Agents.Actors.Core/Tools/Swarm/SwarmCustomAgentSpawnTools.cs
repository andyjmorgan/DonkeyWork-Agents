using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.AgentDefinitions.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Tools.Swarm;

public sealed class SwarmCustomAgentSpawnTools
{
    private readonly IAgentDefinitionService _agentDefinitionService;

    private static readonly JsonSerializerOptions ContractJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        AllowOutOfOrderMetadataProperties = true,
    };

    public SwarmCustomAgentSpawnTools(IAgentDefinitionService agentDefinitionService)
    {
        _agentDefinitionService = agentDefinitionService;
    }

    [AgentTool("spawn_custom_agent")]
    [Description("Spawn a custom agent by its definition ID. The agent will execute the task using its configured model, tools, and system prompt.")]
    public async Task<ToolResult> SpawnCustomAgent(
        [Description("The unique ID of the agent definition to spawn")]
        Guid agent_id,
        [Description("Detailed instructions for the task the agent should perform")]
        string task,
        [Description("A short label describing this agent's task")]
        string label,
        GrainContext context,
        IIdentityContext identityContext,
        CancellationToken ct)
    {
        var definition = await _agentDefinitionService.GetByIdAsync(agent_id, ct);

        if (definition is null)
            return ToolResult.Error($"Agent definition '{agent_id}' not found.");

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

        // Override key fields for custom agent spawning
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
            AgentType = "custom",
            KeyPrefix = AgentKeys.CustomAgentPrefix,
            TimeoutSeconds = contract.TimeoutSeconds,
            McpServers = contract.McpServers,
            EnableSandbox = contract.EnableSandbox,
            SandboxPodName = contract.SandboxPodName,
            ModelId = contract.ModelId,
            Prompts = contract.Prompts,
            SubAgents = contract.SubAgents,
            ReasoningEffort = contract.ReasoningEffort,
        };

        contract = contract.WithParentContext(context);

        return await SwarmAgentSpawner.SpawnAsync(contract, task, label, context, identityContext, ct);
    }
}
