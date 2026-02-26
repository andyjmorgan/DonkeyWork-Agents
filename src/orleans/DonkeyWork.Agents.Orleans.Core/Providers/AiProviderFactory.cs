using DonkeyWork.Agents.Orleans.Core.Providers.Anthropic;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orleans.Core.Providers;

internal sealed class AiProviderFactory : IAiProviderFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiProviderFactory(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
    {
        _loggerFactory = loggerFactory;
        _httpClientFactory = httpClientFactory;
    }

    public IAiProvider Create(ProviderType providerType) => providerType switch
    {
        ProviderType.Anthropic => new AnthropicProvider(
            _loggerFactory.CreateLogger<AnthropicProvider>(), _httpClientFactory),
        _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, "Unsupported provider type")
    };
}
