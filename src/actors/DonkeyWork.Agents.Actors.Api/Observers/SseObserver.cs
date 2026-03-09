using System.Threading.Channels;
using DonkeyWork.Agents.Actors.Contracts.Events;
using DonkeyWork.Agents.Actors.Contracts.Grains;

namespace DonkeyWork.Agents.Actors.Api.Observers;

/// <summary>
/// Bridges <see cref="IAgentResponseObserver"/> events to a <see cref="Channel{T}"/>
/// for consumption by SSE streaming endpoints.
/// </summary>
public sealed class SseObserver : IAgentResponseObserver, IDisposable
{
    private readonly Channel<StreamEventBase> _channel = Channel.CreateUnbounded<StreamEventBase>(
        new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });

    public void OnEvent(StreamEventBase streamEvent)
    {
        _channel.Writer.TryWrite(streamEvent);
    }

    public void Complete()
    {
        _channel.Writer.TryComplete();
    }

    public IAsyncEnumerable<StreamEventBase> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public void Dispose()
    {
        _channel.Writer.TryComplete();
    }
}
