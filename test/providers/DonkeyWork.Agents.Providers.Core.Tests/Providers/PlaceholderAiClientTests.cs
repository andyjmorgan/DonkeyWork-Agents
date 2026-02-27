using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;
using DonkeyWork.Agents.Providers.Core.Providers;
using Xunit;

namespace DonkeyWork.Agents.Providers.Core.Tests.Providers;

public class PlaceholderAiClientTests
{
    [Fact]
    public async Task StreamCompletionAsync_ReturnsExpectedSequence()
    {
        var client = new PlaceholderAiClient();
        var messages = new List<InternalMessage>
        {
            new InternalContentMessage
            {
                Role = InternalMessageRole.User,
                Content = [new TextChatContentPart { Text = "Hello" }]
            }
        };

        var results = new List<ModelResponseBase>();
        await foreach (var msg in client.StreamCompletionAsync(messages, null, null))
        {
            results.Add(msg);
        }

        // Should start with BlockStart
        Assert.IsType<ModelResponseBlockStart>(results[0]);
        var blockStart = (ModelResponseBlockStart)results[0];
        Assert.Equal(0, blockStart.BlockIndex);
        Assert.Equal(InternalContentBlockType.Text, blockStart.Type);

        // Should have text content chunks
        var textChunks = results.OfType<ModelResponseTextContent>().ToList();
        Assert.NotEmpty(textChunks);

        // Should have BlockEnd
        var blockEnds = results.OfType<ModelResponseBlockEnd>().ToList();
        Assert.Single(blockEnds);

        // Should have Usage
        var usage = results.OfType<ModelResponseUsage>().Single();
        Assert.Equal(10, usage.InputTokens);
        Assert.True(usage.OutputTokens > 0);

        // Should end with StreamEnd
        var streamEnd = results.OfType<ModelResponseStreamEnd>().Single();
        Assert.Equal(InternalStopReason.EndTurn, streamEnd.Reason);
    }
}
