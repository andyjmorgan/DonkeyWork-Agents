using System.Buffers;
using System.Text;
using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Models.Events;
using DonkeyWork.Agents.Agents.Contracts.Services;
using Microsoft.Extensions.Logging;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;

namespace DonkeyWork.Agents.Agents.Core.Services;

/// <summary>
/// Singleton service for managing execution streams.
/// Handles stream lifecycle and reading events.
/// </summary>
public class ExecutionStreamService : IExecutionStreamService, IAsyncDisposable
{
    private readonly ILogger<ExecutionStreamService> _logger;
    private readonly StreamSystem _streamSystem;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExecutionStreamService(
        ILogger<ExecutionStreamService> logger,
        StreamSystem streamSystem)
    {
        _logger = logger;
        _streamSystem = streamSystem;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };
    }

    public async Task CreateStreamAsync(Guid executionId)
    {
        var streamName = GetStreamName(executionId);

        try
        {
            await _streamSystem.CreateStream(new StreamSpec(streamName));
            _logger.LogInformation("Created stream {StreamName} for execution {ExecutionId}", streamName, executionId);
        }
        catch (CreateStreamException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogDebug("Stream {StreamName} already exists", streamName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create stream {StreamName} for execution {ExecutionId}", streamName, executionId);
            throw;
        }
    }

    public async IAsyncEnumerable<ExecutionEvent> ReadEventsAsync(Guid executionId, long offset = 0)
    {
        var streamName = GetStreamName(executionId);
        var eventQueue = new System.Collections.Concurrent.ConcurrentQueue<ExecutionEvent>();
        var completionSource = new TaskCompletionSource<bool>();
        var newEventSignal = new SemaphoreSlim(0);

        _logger.LogInformation("Starting to read events from stream {StreamName} with offset {Offset}", streamName, offset);

        Consumer? consumer = null;
        try
        {
            consumer = await Consumer.Create(
                new ConsumerConfig(_streamSystem, streamName)
                {
                    ClientProvidedName = $"consumer-{executionId}-{Guid.NewGuid()}",
                    OffsetSpec = new OffsetTypeOffset((ulong)Math.Max(0, offset)),
                    MessageHandler = async (stream, consumerInstance, context, message) =>
                    {
                        try
                        {
                            var bytes = message.Data.Contents.ToArray();
                            var json = Encoding.UTF8.GetString(bytes);
                            _logger.LogDebug("Received message from stream {StreamName}: {Json}", streamName, json);

                            var evt = JsonSerializer.Deserialize<ExecutionEvent>(json, _jsonOptions);

                            if (evt != null)
                            {
                                _logger.LogDebug("Enqueuing event {EventType} to queue", evt.GetType().Name);
                                eventQueue.Enqueue(evt);
                                newEventSignal.Release(); // Signal that a new event is available

                                // Signal completion if we received a terminal event
                                if (evt is ExecutionCompletedEvent or ExecutionFailedEvent)
                                {
                                    _logger.LogInformation("Received terminal event {EventType}, signaling completion", evt.GetType().Name);
                                    completionSource.TrySetResult(true);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to deserialize event from stream {StreamName}", streamName);
                        }

                        await Task.CompletedTask;
                    }
                });

            _logger.LogInformation("Consumer created for stream {StreamName}", streamName);

            // Yield events as they arrive
            var timeout = TimeSpan.FromMinutes(10);
            var startTime = DateTime.UtcNow;
            var yieldCount = 0;

            while (DateTime.UtcNow - startTime < timeout)
            {
                // Dequeue and yield all available events
                while (eventQueue.TryDequeue(out var evt))
                {
                    _logger.LogDebug("Yielding event {EventType} (total yielded: {Count})", evt.GetType().Name, ++yieldCount);
                    yield return evt;
                }

                // Check if execution completed
                if (completionSource.Task.IsCompleted)
                {
                    _logger.LogInformation("Execution completed, breaking from read loop. Total events yielded: {Count}", yieldCount);
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

            _logger.LogInformation("Finished reading events from stream {StreamName}. Total events: {Count}", streamName, yieldCount);
        }
        finally
        {
            if (consumer != null)
            {
                _logger.LogDebug("Closing consumer for stream {StreamName}", streamName);
                await consumer.Close();
            }
        }
    }

    public async Task DeleteStreamAsync(Guid executionId)
    {
        var streamName = GetStreamName(executionId);

        try
        {
            await _streamSystem.DeleteStream(streamName);
            _logger.LogInformation("Deleted stream {StreamName} for execution {ExecutionId}", streamName, executionId);
        }
        catch (DeleteStreamException ex) when (ex.Message.Contains("does not exist"))
        {
            _logger.LogDebug("Stream {StreamName} does not exist", streamName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete stream {StreamName}", streamName);
            throw;
        }
    }

    private static string GetStreamName(Guid executionId)
    {
        return $"execution-{executionId}";
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _streamSystem.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing StreamSystem");
        }
    }
}
