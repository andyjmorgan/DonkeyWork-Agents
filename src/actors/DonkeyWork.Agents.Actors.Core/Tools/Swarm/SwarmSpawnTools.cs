using System.ComponentModel;
using DonkeyWork.Agents.Identity.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Tools.Swarm;

public sealed class SwarmSpawnTools
{
    [AgentTool("spawn_researcher")]
    [Description("Spawn a research agent to investigate a specific question. The agent will search the web and return findings. Use this for questions that require up-to-date information or in-depth research.")]
    public async Task<ToolResult> SpawnResearcher(
        [Description("The research question or topic to investigate")]
        string query,
        [Description("A short label describing this research task")]
        string label,
        GrainContext context,
        IIdentityContext identityContext,
        CancellationToken ct)
    {
        var contract = AgentContracts.Research();
        return await SwarmAgentSpawner.SpawnAsync(contract, query, label, context, identityContext, ct);
    }

    [AgentTool("spawn_deep_researcher")]
    [Description("Spawn a deep research agent that can itself spawn sub-researchers for comprehensive multi-faceted investigation. Use this for complex topics that benefit from parallel research across multiple sub-questions.")]
    public async Task<ToolResult> SpawnDeepResearcher(
        [Description("The complex research question or topic requiring deep investigation")]
        string query,
        [Description("A short label describing this deep research task")]
        string label,
        GrainContext context,
        IIdentityContext identityContext,
        CancellationToken ct)
    {
        var contract = AgentContracts.DeepResearch();
        return await SwarmAgentSpawner.SpawnAsync(contract, query, label, context, identityContext, ct);
    }
}
