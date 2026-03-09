using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Orleans.Streams;

namespace Orleans.Streaming.Nats.Provider;

public sealed class NatsQueueAdapterReceiver : IQueueAdapterReceiver
{
    private readonly INatsJSContext _jsContext;
    private readonly string _streamName;
    private readonly string _subjectPrefix;
    private readonly int _partitionIndex;
    private readonly string _consumerName;
    private readonly ILogger _logger;

    private INatsJSConsumer? _consumer;
    private CancellationTokenSource? _cts;
    private Task? _fetchTask;
    private readonly ConcurrentQueue<NatsBatchContainer> _buffer = new();

    public NatsQueueAdapterReceiver(
        INatsJSContext jsContext,
        string streamName,
        string subjectPrefix,
        int partitionIndex,
        string consumerName,
        ILogger logger)
    {
        _jsContext = jsContext;
        _streamName = streamName;
        _subjectPrefix = subjectPrefix;
        _partitionIndex = partitionIndex;
        _consumerName = consumerName;
        _logger = logger;
    }

    public async Task Initialize(TimeSpan timeout)
    {
        var subject = $"{_subjectPrefix}.{_partitionIndex}";
        var durableName = $"{_consumerName}-{_partitionIndex}";

        _logger.LogInformation(
            "Initializing NATS receiver for partition {Partition} (subject={Subject}, consumer={Consumer})",
            _partitionIndex, subject, durableName);

        var consumerConfig = new ConsumerConfig(durableName)
        {
            FilterSubject = subject,
            AckPolicy = ConsumerConfigAckPolicy.Explicit,
            DeliverPolicy = ConsumerConfigDeliverPolicy.All
        };

        _consumer = await _jsContext.CreateOrUpdateConsumerAsync(_streamName, consumerConfig);

        _cts = new CancellationTokenSource();
        _fetchTask = Task.Run(() => FetchLoop(_cts.Token));
    }

    public Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
    {
        var batch = new List<IBatchContainer>();
        var count = 0;

        while (count < maxCount && _buffer.TryDequeue(out var container))
        {
            batch.Add(container);
            count++;
        }

        return Task.FromResult<IList<IBatchContainer>>(batch);
    }

    public Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
    {
        // NATS durable consumers track acknowledged position automatically.
        // Messages are acked individually in the fetch loop after being enqueued.
        return Task.CompletedTask;
    }

    public async Task Shutdown(TimeSpan timeout)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();

            if (_fetchTask is not null)
            {
                try
                {
                    await _fetchTask.WaitAsync(timeout);
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Fetch loop did not stop within timeout for partition {Partition}",
                        _partitionIndex);
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }

            _cts.Dispose();
            _cts = null;
        }

        _consumer = null;
    }

    private async Task FetchLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await foreach (var msg in _consumer!.FetchAsync<byte[]>(
                    new NatsJSFetchOpts { MaxMsgs = 100 },
                    cancellationToken: ct))
                {
                    var streamId = ExtractStreamId(msg);
                    var sequence = msg.Metadata?.Sequence.Stream ?? 0;
                    var token = new NatsStreamSequenceToken(sequence);
                    var payload = msg.Data ?? [];
                    var container = new NatsBatchContainer(streamId, payload, token);

                    _buffer.Enqueue(container);
                    await msg.AckAsync(cancellationToken: ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in fetch loop for partition {Partition}, retrying...",
                    _partitionIndex);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private static StreamId ExtractStreamId(INatsJSMsg<byte[]> msg)
    {
        if (msg.Headers is not null
            && msg.Headers.TryGetValue("orleans-stream-ns", out var nsValues)
            && msg.Headers.TryGetValue("orleans-stream-key", out var keyValues))
        {
            var ns = nsValues.ToString();
            var key = keyValues.ToString();
            if (!string.IsNullOrEmpty(ns) && !string.IsNullOrEmpty(key))
                return StreamId.Create(ns, key);
        }

        return StreamId.Create("default", Guid.Empty.ToString());
    }
}
