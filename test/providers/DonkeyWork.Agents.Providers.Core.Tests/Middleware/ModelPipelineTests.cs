using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;
using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events;
using DonkeyWork.Agents.Providers.Core.Middleware;
using DonkeyWork.Agents.Providers.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DonkeyWork.Agents.Providers.Core.Tests.Middleware;

public class ModelPipelineTests
{
    [Fact]
    public async Task ExecuteAsync_WithPlaceholderProvider_EmitsTextAndStreamEnd()
    {
        // Arrange - build a real DI container with placeholder provider
        var services = new ServiceCollection();
        services.AddSingleton<IAiClientFactory, PlaceholderAiClientFactory>();
        services.AddTransient<BaseExceptionMiddleware>();
        services.AddTransient<ToolMiddleware>();
        services.AddTransient<GuardrailsMiddleware>();
        services.AddTransient<AccumulatorMiddleware>();
        services.AddTransient<ProviderMiddleware>();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var pipeline = new ModelPipeline(sp, NullLogger<ModelPipeline>.Instance);

        var request = new ModelPipelineRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromText(ChatMessageRole.User, "Hello")
            },
            Model = new PipelineModelConfig
            {
                Provider = LlmProvider.OpenAI,
                ModelId = "gpt-5",
                ApiKey = "test-api-key"
            }
        };

        // Act
        var events = new List<ModelPipelineEvent>();
        await foreach (var evt in pipeline.ExecuteAsync(request))
        {
            events.Add(evt);
        }

        // Assert
        var textEvents = events.OfType<TextDeltaEvent>().ToList();
        Assert.NotEmpty(textEvents);

        var fullText = string.Join("", textEvents.Select(e => e.Text));
        Assert.Contains("placeholder", fullText, StringComparison.OrdinalIgnoreCase);

        var streamEnd = events.OfType<StreamEndEvent>().SingleOrDefault();
        Assert.NotNull(streamEnd);
        Assert.Equal(PipelineStopReason.EndTurn, streamEnd.Reason);
        Assert.NotNull(streamEnd.Usage);
        Assert.Equal(10, streamEnd.Usage.InputTokens);
    }

    [Fact]
    public async Task ExecuteAsync_WithSystemMessage_MapsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IAiClientFactory, PlaceholderAiClientFactory>();
        services.AddTransient<BaseExceptionMiddleware>();
        services.AddTransient<ToolMiddleware>();
        services.AddTransient<GuardrailsMiddleware>();
        services.AddTransient<AccumulatorMiddleware>();
        services.AddTransient<ProviderMiddleware>();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var pipeline = new ModelPipeline(sp, NullLogger<ModelPipeline>.Instance);

        var request = new ModelPipelineRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromText(ChatMessageRole.System, "You are helpful."),
                ChatMessage.FromText(ChatMessageRole.User, "Hello")
            },
            Model = new PipelineModelConfig
            {
                Provider = LlmProvider.Anthropic,
                ModelId = "claude-sonnet-4-5",
                ApiKey = "test-api-key"
            }
        };

        // Act - should not throw
        var events = new List<ModelPipelineEvent>();
        await foreach (var evt in pipeline.ExecuteAsync(request))
        {
            events.Add(evt);
        }

        // Assert - placeholder still works regardless of provider
        Assert.NotEmpty(events);
        Assert.Contains(events, e => e is TextDeltaEvent);
    }

    [Fact]
    public async Task ExecuteAsync_WithOptions_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IAiClientFactory, PlaceholderAiClientFactory>();
        services.AddTransient<BaseExceptionMiddleware>();
        services.AddTransient<ToolMiddleware>();
        services.AddTransient<GuardrailsMiddleware>();
        services.AddTransient<AccumulatorMiddleware>();
        services.AddTransient<ProviderMiddleware>();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var pipeline = new ModelPipeline(sp, NullLogger<ModelPipeline>.Instance);

        var request = new ModelPipelineRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromText(ChatMessageRole.User, "Hello")
            },
            Model = new PipelineModelConfig
            {
                Provider = LlmProvider.Google,
                ModelId = "gemini-2.5-pro",
                ApiKey = "test-api-key"
            },
            Options = new Dictionary<string, object>
            {
                ["temperature"] = 0.7,
                ["max_tokens"] = 1024,
                ["top_p"] = 0.9
            }
        };

        // Act
        var events = new List<ModelPipelineEvent>();
        await foreach (var evt in pipeline.ExecuteAsync(request))
        {
            events.Add(evt);
        }

        // Assert
        Assert.NotEmpty(events);
    }
}
