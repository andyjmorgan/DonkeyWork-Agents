using DonkeyWork.Agents.Orleans.Contracts.Messages;
using DonkeyWork.Agents.Orleans.Core.Providers.Responses;

namespace DonkeyWork.Agents.Orleans.Core.Providers;

internal interface IAiProvider
{
    IAsyncEnumerable<ModelResponseBase> StreamCompletionAsync(
        string systemPrompt,
        IReadOnlyList<InternalMessage> messages,
        IReadOnlyList<InternalToolDefinition>? tools,
        ProviderOptions options,
        CancellationToken ct = default);
}
