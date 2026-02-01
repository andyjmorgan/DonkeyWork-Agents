using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;

namespace DonkeyWork.Agents.Agents.Core.Execution.Executors;

/// <summary>
/// Executor for End nodes.
/// Collects output from upstream node(s) and signals completion.
/// </summary>
public class EndNodeExecutor : NodeExecutor<EndNodeConfiguration, EndNodeOutput>
{
    public EndNodeExecutor(
        IExecutionStreamWriter streamWriter,
        IExecutionContext context)
        : base(streamWriter, context)
    {
    }

    protected override Task<EndNodeOutput> ExecuteInternalAsync(
        EndNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        if (Context.NodeOutputs.Count == 0)
        {
            throw new InvalidOperationException("End node has no upstream outputs");
        }

        // Get the last output (most recent node execution)
        var lastOutput = Context.NodeOutputs.Values.Last();

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
