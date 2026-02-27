using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Providers;
using DonkeyWork.Agents.Providers.Core.Providers.OpenAi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DonkeyWork.Agents.Providers.Core.Tests.Providers;

/// <summary>
/// Unit tests for the OpenAI client.
/// </summary>
public class OpenAiClientTests
{
    private readonly ILogger<OpenAiClient> _logger = NullLogger<OpenAiClient>.Instance;

    [Fact]
    public void Constructor_WithValidParameters_CreatesClient()
    {
        // Arrange
        var apiKey = "test-api-key";
        var modelId = "gpt-4";

        // Act
        var client = new OpenAiClient(apiKey, modelId, _logger);

        // Assert
        Assert.NotNull(client);
    }

    [Theory]
    [InlineData("gpt-4")]
    [InlineData("gpt-4-turbo")]
    [InlineData("gpt-4o")]
    [InlineData("gpt-3.5-turbo")]
    public void Constructor_WithVariousModelIds_CreatesClient(string modelId)
    {
        // Arrange
        var apiKey = "test-api-key";

        // Act
        var client = new OpenAiClient(apiKey, modelId, _logger);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Client_ImplementsIAiClient()
    {
        // Arrange
        var apiKey = "test-api-key";
        var modelId = "gpt-4";

        // Act
        var client = new OpenAiClient(apiKey, modelId, _logger);

        // Assert
        Assert.IsAssignableFrom<IAiClient>(client);
    }

    [Fact]
    public void StreamCompletionAsync_ReturnsAsyncEnumerable()
    {
        // Arrange
        var client = new OpenAiClient("test-key", "gpt-4", _logger);
        var messages = new List<InternalMessage>
        {
            new InternalContentMessage
            {
                Role = InternalMessageRole.User,
                Content = [new TextChatContentPart { Text = "Hello" }]
            }
        };

        // Act
        var result = client.StreamCompletionAsync(messages, null, null);

        // Assert - The method returns an IAsyncEnumerable (we can't enumerate without valid API key)
        Assert.NotNull(result);
    }
}
