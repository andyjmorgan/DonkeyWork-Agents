using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Providers;
using DonkeyWork.Agents.Providers.Core.Providers.Anthropic;
using DonkeyWork.Agents.Providers.Core.Providers.Google;
using DonkeyWork.Agents.Providers.Core.Providers.OpenAi;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Providers.Core.Tests.Providers;

public class AiClientFactoryTests
{
    private readonly AiClientFactory _factory;

    public AiClientFactoryTests()
    {
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        _factory = new AiClientFactory(loggerFactory.Object);
    }

    [Fact]
    public async Task CreateClientAsync_OpenAI_ReturnsOpenAiClient()
    {
        var config = new InternalModelConfig
        {
            Provider = LlmProvider.OpenAI,
            ModelId = "gpt-5",
            ApiKey = "test-key"
        };

        var client = await _factory.CreateClientAsync(config);

        Assert.IsType<OpenAiClient>(client);
    }

    [Fact]
    public async Task CreateClientAsync_Anthropic_ReturnsAnthropicAiClient()
    {
        var config = new InternalModelConfig
        {
            Provider = LlmProvider.Anthropic,
            ModelId = "claude-sonnet-4-5",
            ApiKey = "test-key"
        };

        var client = await _factory.CreateClientAsync(config);

        Assert.IsType<AnthropicAiClient>(client);
    }

    [Fact]
    public async Task CreateClientAsync_Google_ReturnsGoogleAiClient()
    {
        var config = new InternalModelConfig
        {
            Provider = LlmProvider.Google,
            ModelId = "gemini-2.5-pro",
            ApiKey = "test-key"
        };

        var client = await _factory.CreateClientAsync(config);

        Assert.IsType<GoogleAiClient>(client);
    }

    [Fact]
    public async Task CreateClientAsync_UnsupportedProvider_ThrowsNotSupportedException()
    {
        var config = new InternalModelConfig
        {
            Provider = (LlmProvider)999,
            ModelId = "unknown",
            ApiKey = "test-key"
        };

        await Assert.ThrowsAsync<NotSupportedException>(
            () => _factory.CreateClientAsync(config));
    }
}
