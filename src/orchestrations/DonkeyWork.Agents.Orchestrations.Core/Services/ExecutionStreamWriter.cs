using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream;

namespace DonkeyWork.Agents.Orchestrations.Core.Services;

/// <summary>
/// Scoped service for writing events to the execution stream.
/// Publishes to a per-execution NATS subject for the duration of the execution.
/// </summary>
public class ExecutionStreamWriter : IExecutionStreamWriter
{
    private readonly ILogger<ExecutionStreamWriter> _logger;
    private readonly INatsJSContext _jsContext;
    private readonly JsonSerializerOptions _jsonOptions;

    private Guid _executionId;
    private string? _subject;
    private bool _initialized;

    public ExecutionStreamWriter(
        ILogger<ExecutionStreamWriter> logger,
        INatsJSContext jsContext)
    {
        _logger = logger;
        _jsContext = jsContext;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };
    }

    public Task InitializeAsync(Guid executionId)
    {
        if (_initialized)
        {
            throw new InvalidOperationException("ExecutionStreamWriter has already been initialized");
        }

        _executionId = executionId;
        _subject = $"execution.{executionId}";
        _initialized = true;

        _logger.LogDebug("Initialized stream writer for execution {ExecutionId}", executionId);
        return Task.CompletedTask;
    }

    public async Task WriteEventAsync(ExecutionEvent evt)
    {
        if (!_initialized || _subject == null)
        {
            throw new InvalidOperationException("ExecutionStreamWriter must be initialized before writing events");
        }

        evt.ExecutionId = _executionId;
        evt.Timestamp = DateTime.UtcNow;

        var json = JsonSerializer.SerializeToUtf8Bytes<ExecutionEvent>(evt, _jsonOptions);

        var ack = await _jsContext.PublishAsync(_subject, json);
        ack.EnsureSuccess();

        _logger.LogDebug("Wrote event {EventType} to subject {Subject}",
            evt.GetType().Name, _subject);
    }

    public ValueTask DisposeAsync()
    {
        // No-op: NATS connection is shared at the singleton level
        return ValueTask.CompletedTask;
    }
}
