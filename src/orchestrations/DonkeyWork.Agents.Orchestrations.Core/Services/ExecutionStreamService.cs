using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace DonkeyWork.Agents.Orchestrations.Core.Services;

/// <summary>
/// Singleton service for managing execution streams.
/// Uses a shared NATS JetStream stream with per-execution subject filtering.
/// </summary>
public class ExecutionStreamService : IExecutionStreamService
{
    private readonly ILogger<ExecutionStreamService> _logger;
    private readonly INatsJSContext _jsContext;
    private readonly string _streamName;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExecutionStreamService(
        ILogger<ExecutionStreamService> logger,
        INatsJSContext jsContext,
        string streamName)
    {
        _logger = logger;
        _jsContext = jsContext;
        _streamName = streamName;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };
    }

    public Task CreateStreamAsync(Guid executionId)
    {
        // No-op: the shared JetStream stream with wildcard subject "execution.>"
        // is created at startup. Individual execution subjects are created implicitly
        // when the first message is published.
        _logger.LogDebug("Stream ready for execution {ExecutionId} (shared stream: {StreamName})",
            executionId, _streamName);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<ExecutionEvent> ReadEventsAsync(Guid executionId, long offset = 0)
    {
        var subject = $"execution.{executionId}";
        var eventQueue = new ConcurrentQueue<ExecutionEvent>();
        var completionSource = new TaskCompletionSource<bool>();
        var newEventSignal = new SemaphoreSlim(0);

        _logger.LogInformation("Starting to read events from subject {Subject} with offset {Offset}",
            subject, offset);

        // Create an ephemeral ordered consumer filtered to this execution's subject
        var consumerConfig = new ConsumerConfig
        {
            FilterSubject = subject,
            AckPolicy = ConsumerConfigAckPolicy.None,
            DeliverPolicy = ConsumerConfigDeliverPolicy.All
        };

        INatsJSConsumer consumer;
        try
        {
            consumer = await _jsContext.CreateOrUpdateConsumerAsync(_streamName, consumerConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create consumer for subject {Subject}", subject);
            throw;
        }

        _logger.LogInformation("Consumer created for subject {Subject}", subject);

        // Start a background fetch loop
        var cts = new CancellationTokenSource();
        var fetchTask = Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await foreach (var msg in consumer.FetchAsync<byte[]>(
                        new NatsJSFetchOpts { MaxMsgs = 100 },
                        cancellationToken: cts.Token))
                    {
                        try
                        {
                            var json = Encoding.UTF8.GetString(msg.Data ?? []);
                            _logger.LogDebug("Received message from subject {Subject}: {Json}", subject, json);

                            var evt = JsonSerializer.Deserialize<ExecutionEvent>(json, _jsonOptions);
                            if (evt != null)
                            {
                                eventQueue.Enqueue(evt);
                                newEventSignal.Release();

                                if (evt is ExecutionCompletedEvent or ExecutionFailedEvent)
                                {
                                    _logger.LogInformation("Received terminal event {EventType}, signaling completion",
                                        evt.GetType().Name);
                                    completionSource.TrySetResult(true);
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to deserialize event from subject {Subject}", subject);
                        }
                    }

                    // Brief pause between fetches when no messages available
                    await Task.Delay(50, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
        }, cts.Token);

        try
        {
            // Yield events as they arrive
            var timeout = TimeSpan.FromMinutes(10);
            var startTime = DateTime.UtcNow;
            var yieldCount = 0;

            while (DateTime.UtcNow - startTime < timeout)
            {
                // Dequeue and yield all available events
                while (eventQueue.TryDequeue(out var evt))
                {
                    _logger.LogDebug("Yielding event {EventType} (total yielded: {Count})",
                        evt.GetType().Name, ++yieldCount);
                    yield return evt;
                }

                // Check if execution completed
                if (completionSource.Task.IsCompleted)
                {
                    _logger.LogInformation(
                        "Execution completed, breaking from read loop. Total events yielded: {Count}", yieldCount);
                    break;
                }

                // Wait for new event signal or short timeout
                await newEventSignal.WaitAsync(TimeSpan.FromMilliseconds(100));
            }

            // Yield any remaining events
            while (eventQueue.TryDequeue(out var evt))
            {
                _logger.LogDebug("Yielding remaining event {EventType}", evt.GetType().Name);
                yield return evt;
            }

            _logger.LogInformation("Finished reading events from subject {Subject}. Total events: {Count}",
                subject, yieldCount);
        }
        finally
        {
            await cts.CancelAsync();
            try
            {
                await fetchTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            cts.Dispose();
        }
    }

    public async Task DeleteStreamAsync(Guid executionId)
    {
        var subject = $"execution.{executionId}";

        try
        {
            await _jsContext.PurgeStreamAsync(_streamName, new StreamPurgeRequest { Filter = subject });
            _logger.LogInformation("Purged messages for subject {Subject} in stream {StreamName}",
                subject, _streamName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to purge messages for subject {Subject}", subject);
            throw;
        }
    }
}
