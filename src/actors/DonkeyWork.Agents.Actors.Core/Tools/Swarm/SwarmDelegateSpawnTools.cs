using System.ComponentModel;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Identity.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Tools.Swarm;

public sealed class SwarmDelegateSpawnTools
{
    [AgentTool(ToolNames.SpawnDelegate)]
    [Description("Spawn a delegate agent to handle a specific task. The delegate will execute the task and return results. Use this to offload discrete pieces of work that can run independently.")]
    public async Task<ToolResult> SpawnDelegate(
        [Description("Detailed instructions for the task the delegate should perform")]
        string task,
        [Description("A short label describing this delegated task")]
        string label,
        [Description("Optional list of tool group names to give the delegate (e.g. [\"sandbox\", \"project_management\"]). If omitted, the delegate inherits the parent's tool groups.")]
        string[]? tool_groups,
        GrainContext context,
        IIdentityContext identityContext,
        CancellationToken ct)
    {
        var contract = AgentContracts.Delegate()
            .WithParentContext(context, tool_groups);
        return await SwarmAgentSpawner.SpawnAsync(contract, task, label, context, identityContext, ct);
    }
}
