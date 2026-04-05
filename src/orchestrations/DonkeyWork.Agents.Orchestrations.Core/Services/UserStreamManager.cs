using System.Collections.Concurrent;
using DonkeyWork.Agents.Orchestrations.Contracts;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace DonkeyWork.Agents.Orchestrations.Core.Services;

public class UserStreamManager : IUserStreamManager
{
    private readonly INatsJSContext _jsContext;
    private readonly OrchestrationsOptions _options;
    private readonly ILogger<UserStreamManager> _logger;
    private readonly ConcurrentDictionary<Guid, bool> _ensuredStreams = new();

    public UserStreamManager(
        INatsJSContext jsContext,
        IOptions<OrchestrationsOptions> options,
        ILogger<UserStreamManager> logger)
    {
        _jsContext = jsContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureStreamAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (_ensuredStreams.ContainsKey(userId))
            return;

        var streamName = NatsSubjects.UserStream(userId);
        var subjectFilter = NatsSubjects.UserSubjectFilter(userId);

        var config = new StreamConfig(streamName, [subjectFilter])
        {
            MaxAge = _options.StreamRetention,
            MaxBytes = _options.StreamMaxBytes,
            Retention = StreamConfigRetention.Limits,
            Discard = StreamConfigDiscard.Old
        };

        try
        {
            await _jsContext.CreateOrUpdateStreamAsync(config, cancellationToken);
            _ensuredStreams.TryAdd(userId, true);
            _logger.LogDebug("Ensured per-user stream {StreamName}", streamName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure per-user stream {StreamName}", streamName);
            throw;
        }
    }
}
