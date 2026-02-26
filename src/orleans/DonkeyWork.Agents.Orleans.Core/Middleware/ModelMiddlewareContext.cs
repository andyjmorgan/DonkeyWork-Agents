using DonkeyWork.Agents.Orleans.Contracts.Messages;
using DonkeyWork.Agents.Orleans.Core.Providers;

namespace DonkeyWork.Agents.Orleans.Core.Middleware;

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
}
