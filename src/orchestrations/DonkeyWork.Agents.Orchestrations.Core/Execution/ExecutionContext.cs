using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution;

/// <summary>
/// Scoped context that maintains state during orchestration execution.
/// </summary>
public class ExecutionContext : IExecutionContext
{
    private readonly Dictionary<string, object> _nodeOutputs = new();
    private bool _isHydrated;

    /// <inheritdoc />
    public Guid ExecutionId { get; private set; }

    /// <inheritdoc />
    public Guid UserId { get; private set; }

    /// <inheritdoc />
    public ExecutionInterface Interface { get; private set; }

    /// <inheritdoc />
    public object Input { get; private set; } = null!;

    /// <inheritdoc />
    public JsonDocument InputSchema { get; private set; } = null!;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> NodeOutputs => _nodeOutputs;

    /// <inheritdoc />
    public void Hydrate(Guid executionId, Guid userId, ExecutionInterface executionInterface, object input, JsonDocument inputSchema)
    {
        if (_isHydrated)
        {
            throw new InvalidOperationException("ExecutionContext has already been hydrated");
        }

        ExecutionId = executionId;
        UserId = userId;
        Interface = executionInterface;
        Input = input;
        InputSchema = inputSchema;
        _isHydrated = true;
    }

    /// <inheritdoc />
    public void SetNodeOutput(string nodeName, object output)
    {
        if (!_isHydrated)
        {
            throw new InvalidOperationException("ExecutionContext must be hydrated before setting node outputs");
        }

        _nodeOutputs[nodeName] = output;
    }
}
