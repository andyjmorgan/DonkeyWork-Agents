using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;

namespace DonkeyWork.Agents.Providers.Core.Providers;

/// <summary>
/// Internal interface for AI provider clients.
/// </summary>
internal interface IAiClient
{
    /// <summary>
    /// Streams completion responses from the AI provider.
    /// Each chunk is yielded as it arrives from the provider.
    /// </summary>
    IAsyncEnumerable<ModelResponseBase> StreamCompletionAsync(
        IReadOnlyList<InternalMessage> messages,
        IReadOnlyList<InternalToolDefinition>? tools,
        IReadOnlyDictionary<string, object>? providerParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a request without streaming.
    /// The entire response is returned at once after the provider finishes generating.
    /// </summary>
    IAsyncEnumerable<ModelResponseBase> CompleteAsync(
        IReadOnlyList<InternalMessage> messages,
        IReadOnlyList<InternalToolDefinition>? tools,
        IReadOnlyDictionary<string, object>? providerParameters,
        CancellationToken cancellationToken = default);
}
