using DonkeyWork.Agents.Providers.Core.Middleware;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Providers;
using DonkeyWork.Agents.Providers.Core.Providers.Google;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DonkeyWork.Agents.Providers.Core.Tests.Providers;

/// <summary>
/// Unit tests for the Google AI (Gemini) client.
/// </summary>
public class GoogleAiClientTests
{
    private readonly ILogger<GoogleAiClient> _logger = NullLogger<GoogleAiClient>.Instance;

    [Fact]
    public void Constructor_WithValidParameters_CreatesClient()
    {
        // Arrange
        var apiKey = "test-api-key";
        var modelId = "gemini-pro";

        // Act
        var client = new GoogleAiClient(apiKey, modelId, _logger);

        // Assert
        Assert.NotNull(client);
    }

    [Theory]
    [InlineData("gemini-pro")]
    [InlineData("gemini-pro-vision")]
    [InlineData("gemini-1.5-pro")]
    [InlineData("gemini-1.5-flash")]
    [InlineData("gemini-2.0-flash")]
    public void Constructor_WithVariousModelIds_CreatesClient(string modelId)
    {
        // Arrange
        var apiKey = "test-api-key";

        // Act
        var client = new GoogleAiClient(apiKey, modelId, _logger);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Client_ImplementsIAiClient()
    {
        // Arrange
        var apiKey = "test-api-key";
        var modelId = "gemini-pro";

        // Act
        var client = new GoogleAiClient(apiKey, modelId, _logger);

        // Assert
        Assert.IsAssignableFrom<IAiClient>(client);
    }

    [Fact]
    public void StreamCompletionAsync_ReturnsAsyncEnumerable()
    {
        // Arrange
        var client = new GoogleAiClient("test-key", "gemini-pro", _logger);
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
