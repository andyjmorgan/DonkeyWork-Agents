using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;

namespace DonkeyWork.Agents.Providers.Core.Providers;

/// <summary>
/// Internal interface for AI provider clients.
/// </summary>
internal interface IAiClient
{
    IAsyncEnumerable<ModelResponseBase> StreamCompletionAsync(
        IReadOnlyList<InternalMessage> messages,
        IReadOnlyList<InternalToolDefinition>? tools,
        IReadOnlyDictionary<string, object>? providerParameters,
        CancellationToken cancellationToken = default);
}
