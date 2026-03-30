using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Core.Providers;

namespace DonkeyWork.Agents.Actors.Core.Middleware;

internal class ModelMiddlewareContext
{
    public List<InternalMessage> Messages { get; set; } = [];
    public string SystemPrompt { get; set; } = "";
    public IReadOnlyList<InternalToolDefinition>? Tools { get; set; }
    public required ProviderOptions ProviderOptions { get; set; }
    public IToolExecutor? ToolExecutor { get; set; }
    public CancellationToken CancellationToken { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public ResponsePartsBuilder PartsBuilder { get; } = new();
    public Guid TurnId { get; set; }
    public Guid? ParentTurnId { get; set; }
    public Func<InternalMessage, Task>? PersistMessage { get; set; }
    public Func<Task<IReadOnlyList<InternalMessage>>>? DrainPendingMessages { get; set; }
}
