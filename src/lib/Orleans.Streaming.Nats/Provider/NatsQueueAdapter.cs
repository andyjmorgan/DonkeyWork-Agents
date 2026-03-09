using System.Text.Json;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using Orleans.Streams;

namespace Orleans.Streaming.Nats.Provider;

public sealed class NatsQueueAdapter : IQueueAdapter
{
    private readonly INatsJSContext _jsContext;
    private readonly string _streamName;
    private readonly string _subjectPrefix;
    private readonly string _consumerName;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    public string Name { get; }
    public bool IsRewindable => true;
    public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;

    public NatsQueueAdapter(
        string name,
        INatsJSContext jsContext,
        string streamName,
        string subjectPrefix,
        string consumerName,
        ILoggerFactory loggerFactory)
    {
        Name = name;
        _jsContext = jsContext;
        _streamName = streamName;
        _subjectPrefix = subjectPrefix;
        _consumerName = consumerName;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<NatsQueueAdapter>();
    }

    public async Task QueueMessageBatchAsync<T>(
        StreamId streamId,
        IEnumerable<T> events,
        StreamSequenceToken? token,
        Dictionary<string, object> requestContext)
    {
        foreach (var evt in events)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(evt);
            var routingKey = $"{streamId.GetNamespace()}-{streamId.GetKeyAsString()}";
            var partitionIndex = (uint)Math.Abs(routingKey.GetHashCode()) % (uint)PartitionCount;
            var subject = $"{_subjectPrefix}.{partitionIndex}";

            var headers = new NatsHeaders
            {
                ["orleans-stream-ns"] = streamId.GetNamespace() ?? string.Empty,
                ["orleans-stream-key"] = streamId.GetKeyAsString()
            };

            var ack = await _jsContext.PublishAsync(subject, payload, headers: headers);
            ack.EnsureSuccess();
        }
    }

    public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
    {
        var partitionIndex = (int)(queueId.GetNumericId() % (uint)PartitionCount);

        _logger.LogDebug("Creating receiver for queue {QueueId} -> subject {Subject}.{Partition}",
            queueId, _subjectPrefix, partitionIndex);

        return new NatsQueueAdapterReceiver(
            _jsContext,
            _streamName,
            _subjectPrefix,
            partitionIndex,
            _consumerName,
            _loggerFactory.CreateLogger<NatsQueueAdapterReceiver>());
    }

    internal int PartitionCount { get; set; } = 8;
}
