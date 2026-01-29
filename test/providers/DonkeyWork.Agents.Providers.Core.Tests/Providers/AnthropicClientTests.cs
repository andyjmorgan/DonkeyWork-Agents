using DonkeyWork.Agents.Providers.Core.Middleware;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Providers;
using DonkeyWork.Agents.Providers.Core.Providers.Anthropic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DonkeyWork.Agents.Providers.Core.Tests.Providers;

/// <summary>
/// Unit tests for the Anthropic AI client.
/// </summary>
public class AnthropicClientTests
{
    private readonly ILogger<AnthropicAiClient> _logger = NullLogger<AnthropicAiClient>.Instance;

    [Fact]
    public void Constructor_WithValidParameters_CreatesClient()
    {
        // Arrange
        var apiKey = "test-api-key";
        var modelId = "claude-3-sonnet";

        // Act
        var client = new AnthropicAiClient(apiKey, modelId, _logger);

        // Assert
        Assert.NotNull(client);
    }

    [Theory]
    [InlineData("claude-3-opus")]
    [InlineData("claude-3-sonnet")]
    [InlineData("claude-3-haiku")]
    [InlineData("claude-3-5-sonnet")]
    [InlineData("claude-opus-4")]
    public void Constructor_WithVariousModelIds_CreatesClient(string modelId)
    {
        // Arrange
        var apiKey = "test-api-key";

        // Act
        var client = new AnthropicAiClient(apiKey, modelId, _logger);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Client_ImplementsIAiClient()
    {
        // Arrange
        var apiKey = "test-api-key";
        var modelId = "claude-3-sonnet";

        // Act
        var client = new AnthropicAiClient(apiKey, modelId, _logger);

        // Assert
        Assert.IsAssignableFrom<IAiClient>(client);
    }

    [Fact]
    public void StreamCompletionAsync_ReturnsAsyncEnumerable()
    {
        // Arrange
        var client = new AnthropicAiClient("test-key", "claude-3-sonnet", _logger);
        var messages = new List<InternalMessage>
        {
            new InternalUserMessage
            {
                Role = InternalMessageRole.User,
                Content = "Hello"
            }
        };

        // Act
        var result = client.StreamCompletionAsync(messages, null, null);

        // Assert - The method returns an IAsyncEnumerable (we can't enumerate without valid API key)
        Assert.NotNull(result);
    }
}
