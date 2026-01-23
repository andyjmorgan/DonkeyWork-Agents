using DonkeyWork.Agents.Providers.Core.Middleware.Internal;

namespace DonkeyWork.Agents.Providers.Core.Providers;

/// <summary>
/// Internal factory for creating AI provider clients.
/// </summary>
internal interface IAiClientFactory
{
    Task<IAiClient> CreateClientAsync(InternalModelConfig modelConfig, CancellationToken cancellationToken = default);
}
