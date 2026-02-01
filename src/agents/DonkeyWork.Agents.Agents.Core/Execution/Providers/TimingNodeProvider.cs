using DonkeyWork.Agents.Agents.Core.Execution.Outputs;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Providers;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Agents.Core.Execution.Providers;

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
        _logger.LogDebug("Sleeping for {DurationMs}ms", config.DurationMs);

        await Task.Delay(config.DurationMs, cancellationToken);

        return new SleepNodeOutput
        {
            DurationMs = config.DurationMs
        };
    }
}
