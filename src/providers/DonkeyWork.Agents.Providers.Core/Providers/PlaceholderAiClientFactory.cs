using DonkeyWork.Agents.Providers.Core.Middleware.Internal;

namespace DonkeyWork.Agents.Providers.Core.Providers;

/// <summary>
/// Factory that creates placeholder AI clients.
/// </summary>
internal class PlaceholderAiClientFactory : IAiClientFactory
{
    public Task<IAiClient> CreateClientAsync(InternalModelConfig modelConfig, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IAiClient>(new PlaceholderAiClient());
    }
}
