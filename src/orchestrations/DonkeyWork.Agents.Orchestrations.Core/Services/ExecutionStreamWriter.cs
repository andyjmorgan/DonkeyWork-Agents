using System.Text;
using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using Microsoft.Extensions.Logging;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;

namespace DonkeyWork.Agents.Orchestrations.Core.Services;

/// <summary>
/// Scoped service for writing events to the execution stream.
/// Creates a single producer that lives for the duration of the execution.
/// </summary>
public class ExecutionStreamWriter : IExecutionStreamWriter
{
    private readonly ILogger<ExecutionStreamWriter> _logger;
    private readonly StreamSystem _streamSystem;
    private readonly JsonSerializerOptions _jsonOptions;

    private Producer? _producer;
    private Guid _executionId;
    private string? _streamName;
    private bool _initialized;

    public ExecutionStreamWriter(
        ILogger<ExecutionStreamWriter> logger,
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

    public async Task InitializeAsync(Guid executionId)
    {
        if (_initialized)
        {
            throw new InvalidOperationException("ExecutionStreamWriter has already been initialized");
        }

        _executionId = executionId;
        _streamName = $"execution-{executionId}";

        _producer = await Producer.Create(
            new ProducerConfig(_streamSystem, _streamName)
            {
                ClientProvidedName = $"producer-{executionId}",
                ConfirmationHandler = confirmation =>
                {
                    if (confirmation.Status != ConfirmationStatus.Confirmed)
                    {
                        _logger.LogWarning(
                            "Message not confirmed for stream {StreamName}: {Status}",
                            _streamName,
                            confirmation.Status);
                    }
                    return Task.CompletedTask;
                }
            });

        _initialized = true;
        _logger.LogDebug("Initialized stream writer for execution {ExecutionId}", executionId);
    }

    public async Task WriteEventAsync(ExecutionEvent evt)
    {
        if (!_initialized || _producer == null)
        {
            throw new InvalidOperationException("ExecutionStreamWriter must be initialized before writing events");
        }

        // Set execution ID and timestamp on the event
        evt.ExecutionId = _executionId;
        evt.Timestamp = DateTime.UtcNow;

        var json = JsonSerializer.Serialize<ExecutionEvent>(evt, _jsonOptions);
        var message = new Message(Encoding.UTF8.GetBytes(json));

        // Send the message
        await _producer.Send(message);

        _logger.LogDebug("Wrote event {EventType} to stream {StreamName}",
            evt.GetType().Name, _streamName);
    }

    public async ValueTask DisposeAsync()
    {
        if (_producer != null)
        {
            try
            {
                await _producer.Close();
                _logger.LogDebug("Closed producer for execution {ExecutionId}", _executionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing producer for execution {ExecutionId}", _executionId);
            }
            _producer = null;
        }
    }
}
