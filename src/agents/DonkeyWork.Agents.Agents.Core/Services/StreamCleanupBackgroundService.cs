using DonkeyWork.Agents.Agents.Core.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Stream.Client;

namespace DonkeyWork.Agents.Agents.Core.Services;

public class StreamCleanupBackgroundService : BackgroundService
{
    private readonly ILogger<StreamCleanupBackgroundService> _logger;
    private readonly StreamSystem _streamSystem;
    private readonly AgentsOptions _options;

    public StreamCleanupBackgroundService(
        ILogger<StreamCleanupBackgroundService> logger,
        StreamSystem streamSystem,
        IOptions<AgentsOptions> options)
    {
        _logger = logger;
        _streamSystem = streamSystem;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stream cleanup background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

                _logger.LogInformation("Starting stream cleanup");
                await CleanupOldStreamsAsync(stoppingToken);
                _logger.LogInformation("Stream cleanup completed");
            }
            catch (OperationCanceledException)
            {
                // Service is stopping, this is expected
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during stream cleanup");
            }
        }

        _logger.LogInformation("Stream cleanup background service stopped");
    }

    private async Task CleanupOldStreamsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Note: RabbitMQ Stream Client doesn't provide a direct QueryStreamStats method
            // The actual stream cleanup will need to be coordinated with the execution tracking system
            // which maintains execution metadata including creation timestamps in the database.
            //
            // The proper implementation would:
            // 1. Query the database for executions older than StreamRetention
            // 2. For each old execution, call DeleteStream on its stream
            // 3. Handle cases where the stream may have already been deleted
            //
            // This is a placeholder implementation that will be enhanced when the
            // execution tracking system is implemented in the orchestrator.

            _logger.LogDebug("Stream cleanup placeholder - will be enhanced with execution tracking");

            // TODO: Implement proper cleanup when execution tracking is available
            // Example implementation:
            // var oldExecutions = await _executionRepository.GetExecutionsOlderThan(cutoffTime);
            // foreach (var execution in oldExecutions)
            // {
            //     await _streamSystem.DeleteStream($"execution-{execution.Id}");
            // }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during stream cleanup");
            throw;
        }
    }
}
