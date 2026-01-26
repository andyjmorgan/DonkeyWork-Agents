using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;

namespace DonkeyWork.Agents.Agents.Core.Execution.Executors;

/// <summary>
/// Executor for End nodes.
/// Collects output from upstream node(s) and signals completion.
/// </summary>
public class EndNodeExecutor : NodeExecutor<EndNodeConfiguration, EndNodeOutput>
{
    protected override Task<EndNodeOutput> ExecuteInternalAsync(
        EndNodeConfiguration config,
        ExecutionContext context,
        CancellationToken cancellationToken)
    {
        // For MVP, we get the most recent output from context
        // In the orchestrator, nodes are executed in topological order,
        // so the last node before End will be the upstream node

        if (context.NodeOutputs.Count == 0)
        {
            throw new InvalidOperationException("End node has no upstream outputs");
        }

        // Get the last output (most recent node execution)
        var lastOutput = context.NodeOutputs.Values.Last();

        // Convert to final output format
        object finalOutput = lastOutput switch
        {
            NodeOutput nodeOutput => nodeOutput.ToMessageOutput(),
            string str => str,
            _ => JsonSerializer.Serialize(lastOutput)
        };

        return Task.FromResult(new EndNodeOutput
        {
            FinalOutput = finalOutput
        });
    }
}
