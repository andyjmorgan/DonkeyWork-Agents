using System.ComponentModel;
using DonkeyWork.Agents.Identity.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Tools.Swarm;

public sealed class SwarmDelegateSpawnTools
{
    [AgentTool("spawn_delegate")]
    [Description("Spawn a delegate agent to handle a specific task. The delegate will execute the task and return results. Use this to offload discrete pieces of work that can run independently.")]
    public async Task<ToolResult> SpawnDelegate(
        [Description("Detailed instructions for the task the delegate should perform")]
        string task,
        [Description("A short label describing this delegated task")]
        string label,
        GrainContext context,
        IIdentityContext identityContext,
        CancellationToken ct)
    {
        var contract = AgentContracts.Delegate()
            .WithParentContext(context);
        return await SwarmAgentSpawner.SpawnAsync(contract, task, label, context, identityContext, ct);
    }
}
