using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Core.Providers.Responses;

namespace DonkeyWork.Agents.Actors.Core.Providers;

internal interface IAiProvider
{
    IAsyncEnumerable<ModelResponseBase> StreamCompletionAsync(
        string systemPrompt,
        IReadOnlyList<InternalMessage> messages,
        IReadOnlyList<InternalToolDefinition>? tools,
        ProviderOptions options,
        CancellationToken ct = default);
}
