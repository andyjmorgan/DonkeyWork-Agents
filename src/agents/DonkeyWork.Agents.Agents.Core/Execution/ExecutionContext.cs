using DonkeyWork.Agents.Agents.Contracts.Services;

namespace DonkeyWork.Agents.Agents.Core.Execution;

/// <summary>
/// Scoped context that maintains state during agent execution.
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
    public object Input { get; private set; } = null!;

    /// <inheritdoc />
    public string InputSchema { get; private set; } = null!;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> NodeOutputs => _nodeOutputs;

    /// <inheritdoc />
    public void Hydrate(Guid executionId, Guid userId, object input, string inputSchema)
    {
        if (_isHydrated)
        {
            throw new InvalidOperationException("ExecutionContext has already been hydrated");
        }

        ExecutionId = executionId;
        UserId = userId;
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
