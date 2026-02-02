using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Executors;

/// <summary>
/// Executor for Start nodes.
/// Entry point that makes the input available to downstream nodes.
/// </summary>
public class StartNodeExecutor : NodeExecutor<StartNodeConfiguration, StartNodeOutput>
{
    public StartNodeExecutor(
        IExecutionStreamWriter streamWriter,
        IExecutionContext context)
        : base(streamWriter, context)
    {
    }

    protected override Task<StartNodeOutput> ExecuteInternalAsync(
        StartNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new StartNodeOutput
        {
            Input = Context.Input
        });
    }
}
