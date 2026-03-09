using DonkeyWork.Agents.Orchestrations.Core.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Orchestrations.Core.Services;

public class StreamCleanupBackgroundService : BackgroundService
{
    private readonly ILogger<StreamCleanupBackgroundService> _logger;
    private readonly OrchestrationsOptions _options;

    public StreamCleanupBackgroundService(
        ILogger<StreamCleanupBackgroundService> logger,
        IOptions<OrchestrationsOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stream cleanup background service started (NATS MaxAge handles expiry)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

                _logger.LogDebug(
                    "Stream cleanup check - NATS MaxAge handles automatic message expiry. " +
                    "Retention: {StreamRetention}", _options.StreamRetention);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during stream cleanup");
            }
        }

        _logger.LogInformation("Stream cleanup background service stopped");
    }
}
