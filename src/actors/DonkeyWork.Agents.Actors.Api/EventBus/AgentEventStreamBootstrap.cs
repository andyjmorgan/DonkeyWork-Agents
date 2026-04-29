using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;

namespace DonkeyWork.Agents.Actors.Api.EventBus;

/// <summary>
/// Ensures the AGENT_EVENTS stream and AGENT_EVENT_STASH object store exist on startup.
/// Creates them if absent; leaves existing configuration untouched.
/// </summary>
public sealed class AgentEventStreamBootstrap : IHostedService
{
    private readonly INatsJSContext _js;
    private readonly INatsObjContext _obj;
    private readonly ILogger<AgentEventStreamBootstrap> _log;

    public AgentEventStreamBootstrap(
        INatsJSContext js,
        INatsObjContext obj,
        ILogger<AgentEventStreamBootstrap> log)
    {
        _js = js;
        _obj = obj;
        _log = log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await EnsureStreamAsync(cancellationToken);
        await EnsureBucketAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureStreamAsync(CancellationToken ct)
    {
        try
        {
            await _js.CreateStreamAsync(new StreamConfig(AgentEventSubjects.StreamName,
                [AgentEventSubjects.SubjectsFilter])
            {
                Retention = StreamConfigRetention.Limits,
                MaxAge = TimeSpan.FromHours(1),
                MaxMsgsPerSubject = 10000,
                Storage = StreamConfigStorage.File,
                NumReplicas = 1,
            }, ct);

            _log.LogInformation("Created JetStream stream {Stream}", AgentEventSubjects.StreamName);
        }
        catch (Exception ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                                   || ex.Message.Contains("stream name already in use", StringComparison.OrdinalIgnoreCase))
        {
            _log.LogDebug("JetStream stream {Stream} already exists", AgentEventSubjects.StreamName);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to create JetStream stream {Stream}, will retry on next start",
                AgentEventSubjects.StreamName);
        }
    }

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        try
        {
            await _obj.CreateObjectStoreAsync(new NatsObjConfig(AgentEventSubjects.BucketName)
            {
                MaxAge = TimeSpan.FromMinutes(75),
                NumberOfReplicas = 1,
            }, ct);

            _log.LogInformation("Created object store bucket {Bucket}", AgentEventSubjects.BucketName);
        }
        catch (Exception ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                                   || ex.Message.Contains("stream name already in use", StringComparison.OrdinalIgnoreCase))
        {
            _log.LogDebug("Object store bucket {Bucket} already exists", AgentEventSubjects.BucketName);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to create object store bucket {Bucket}, stash will be unavailable",
                AgentEventSubjects.BucketName);
        }
    }
}
