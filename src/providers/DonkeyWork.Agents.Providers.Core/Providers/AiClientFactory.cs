using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Providers.Anthropic;
using DonkeyWork.Agents.Providers.Core.Providers.Google;
using DonkeyWork.Agents.Providers.Core.Providers.OpenAi;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Providers.Core.Providers;

/// <summary>
/// Factory that creates AI provider clients based on the configured provider.
/// </summary>
internal sealed class AiClientFactory : IAiClientFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public AiClientFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public Task<IAiClient> CreateClientAsync(InternalModelConfig modelConfig, CancellationToken cancellationToken = default)
    {
        IAiClient client = modelConfig.Provider switch
        {
            LlmProvider.OpenAI => new OpenAiClient(
                modelConfig.ApiKey,
                modelConfig.ModelId,
                _loggerFactory.CreateLogger<OpenAiClient>()),

            LlmProvider.Anthropic => new AnthropicAiClient(
                modelConfig.ApiKey,
                modelConfig.ModelId,
                _loggerFactory.CreateLogger<AnthropicAiClient>()),

            LlmProvider.Google => new GoogleAiClient(
                modelConfig.ApiKey,
                modelConfig.ModelId,
                _loggerFactory.CreateLogger<GoogleAiClient>()),

            _ => throw new NotSupportedException($"Provider '{modelConfig.Provider}' is not supported.")
        };

        return Task.FromResult(client);
    }
}
