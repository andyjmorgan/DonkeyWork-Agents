using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Providers;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Providers;

/// <summary>
/// Provider for timing-related node executions.
/// </summary>
[NodeProvider]
public class TimingNodeProvider
{
    private readonly ILogger<TimingNodeProvider> _logger;

    public TimingNodeProvider(ILogger<TimingNodeProvider> logger)
    {
        _logger = logger;
    }

    [NodeMethod(NodeType.Sleep)]
    public async Task<SleepNodeOutput> ExecuteSleepAsync(
        SleepNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        var durationMs = (int)(config.DurationSeconds * 1000);
        _logger.LogDebug("Sleeping for {DurationSeconds}s ({DurationMs}ms)", config.DurationSeconds, durationMs);

        await Task.Delay(durationMs, cancellationToken);

        return new SleepNodeOutput
        {
            DurationSeconds = config.DurationSeconds
        };
    }
}
