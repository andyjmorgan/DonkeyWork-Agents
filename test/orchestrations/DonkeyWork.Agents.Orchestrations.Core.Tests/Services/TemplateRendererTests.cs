using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Services;
using Moq;

namespace DonkeyWork.Agents.Orchestrations.Core.Tests.Services;

public class TemplateRendererTests
{
    private static TemplateRenderer CreateRenderer(Action<Mock<IExecutionContext>>? setup = null)
    {
        var ctx = new Mock<IExecutionContext>();
        ctx.SetupGet(c => c.Input).Returns(JsonDocument.Parse("{}").RootElement);
        ctx.SetupGet(c => c.NodeOutputs).Returns(new Dictionary<string, object>());
        setup?.Invoke(ctx);
        return new TemplateRenderer(ctx.Object);
    }

    [Fact]
    public async Task ToJson_StringArray_ProducesJsonArray()
    {
        var renderer = CreateRenderer(ctx =>
        {
            ctx.SetupGet(c => c.NodeOutputs).Returns(new Dictionary<string, object>
            {
                ["chunker"] = new { Chunks = new[] { "first", "second", "third" } },
            });
        });

        var result = await renderer.RenderAsync("{{ Steps.chunker.Chunks | to_json }}");

        Assert.Equal("[\"first\",\"second\",\"third\"]", result);
    }

    [Fact]
    public async Task ToJson_EmptyList_ProducesEmptyArray()
    {
        var renderer = CreateRenderer(ctx =>
        {
            ctx.SetupGet(c => c.NodeOutputs).Returns(new Dictionary<string, object>
            {
                ["chunker"] = new { Chunks = Array.Empty<string>() },
            });
        });

        var result = await renderer.RenderAsync("{{ Steps.chunker.Chunks | to_json }}");

        Assert.Equal("[]", result);
    }

    [Fact]
    public async Task ToJson_SingleScalar_ProducesJsonString()
    {
        var renderer = CreateRenderer();

        var result = await renderer.RenderAsync("{{ \"hello\" | to_json }}");

        Assert.Equal("\"hello\"", result);
    }

    [Fact]
    public async Task ToJson_Null_ProducesNullLiteral()
    {
        var renderer = CreateRenderer();

        var result = await renderer.RenderAsync("{{ null | to_json }}");

        Assert.Equal("null", result);
    }

    [Fact]
    public async Task ToJson_ResultParsesAsJsonArrayOfStrings()
    {
        // Round-trip: the output should be something TtsInputParser can consume.
        var renderer = CreateRenderer(ctx =>
        {
            ctx.SetupGet(c => c.NodeOutputs).Returns(new Dictionary<string, object>
            {
                ["chunker"] = new { Chunks = new[] { "line one\nline two", "second chunk" } },
            });
        });

        var rendered = await renderer.RenderAsync("{{ Steps.chunker.Chunks | to_json }}");
        using var doc = JsonDocument.Parse(rendered);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(2, doc.RootElement.GetArrayLength());
        Assert.Equal("line one\nline two", doc.RootElement[0].GetString());
    }
}
