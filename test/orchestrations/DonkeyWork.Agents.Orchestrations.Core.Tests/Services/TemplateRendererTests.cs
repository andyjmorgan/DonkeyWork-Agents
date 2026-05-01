using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using DonkeyWork.Agents.Orchestrations.Core.Services;
using OrchestrationExecutionContext = DonkeyWork.Agents.Orchestrations.Core.Execution.ExecutionContext;

namespace DonkeyWork.Agents.Orchestrations.Core.Tests.Services;

public class TemplateRendererTests
{
    private static OrchestrationExecutionContext BuildContext(Guid? userId = null)
    {
        var context = new OrchestrationExecutionContext();
        context.Hydrate(
            Guid.NewGuid(),
            userId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ExecutionInterface.Direct,
            JsonDocument.Parse("{}").RootElement,
            JsonDocument.Parse("{\"type\":\"object\"}"));
        return context;
    }

    private static TextToSpeechNodeOutput BuildTtsOutput(string contentType = "audio/mpeg", string fileExtension = "mp3")
    {
        return new TextToSpeechNodeOutput
        {
            Voice = "Kore",
            Model = "gemini-2.5-flash-preview-tts",
            Clips =
            [
                new AudioClip
                {
                    AudioBase64 = Convert.ToBase64String([1, 2, 3]),
                    ContentType = contentType,
                    FileExtension = fileExtension,
                    SizeBytes = 3,
                    Transcript = "Hello world",
                },
            ],
        };
    }

    #region Top-Level Node Access Tests

    [Fact]
    public async Task RenderAsync_NodeNameAtTopLevel_ResolvesContentType()
    {
        var context = BuildContext();
        context.SetNodeOutput("Gemini_25_Flash_TTS", BuildTtsOutput());

        var renderer = new TemplateRenderer(context);

        var result = await renderer.RenderAsync("{{ Gemini_25_Flash_TTS.ContentType }}");

        Assert.Equal("audio/mpeg", result);
    }

    [Fact]
    public async Task RenderAsync_NodeNameViaStepsPrefix_ResolvesContentType()
    {
        var context = BuildContext();
        context.SetNodeOutput("Gemini_25_Flash_TTS", BuildTtsOutput());

        var renderer = new TemplateRenderer(context);

        var result = await renderer.RenderAsync("{{ Steps.Gemini_25_Flash_TTS.ContentType }}");

        Assert.Equal("audio/mpeg", result);
    }

    [Fact]
    public async Task RenderAsync_NodeNameAtTopLevel_ResolvesFileExtension()
    {
        var context = BuildContext();
        context.SetNodeOutput("Gemini_25_Flash_TTS", BuildTtsOutput(fileExtension: "mp3"));

        var renderer = new TemplateRenderer(context);

        var result = await renderer.RenderAsync("{{ Gemini_25_Flash_TTS.FileExtension }}");

        Assert.Equal("mp3", result);
    }

    [Fact]
    public async Task RenderAsync_NodeNameAtTopLevel_ResolvesVoice()
    {
        var context = BuildContext();
        context.SetNodeOutput("Gemini_25_Flash_TTS", BuildTtsOutput());

        var renderer = new TemplateRenderer(context);

        var result = await renderer.RenderAsync("{{ Gemini_25_Flash_TTS.Voice }}");

        Assert.Equal("Kore", result);
    }

    [Fact]
    public async Task RenderAsync_NodeNameAtTopLevel_ResolvesAudioBase64()
    {
        var context = BuildContext();
        var output = BuildTtsOutput();
        context.SetNodeOutput("Gemini_25_Flash_TTS", output);

        var renderer = new TemplateRenderer(context);

        var result = await renderer.RenderAsync("{{ Gemini_25_Flash_TTS.AudioBase64 }}");

        Assert.Equal(output.AudioBase64, result);
    }

    [Fact]
    public async Task RenderAsync_MultipleNodes_BothAccessibleAtTopLevel()
    {
        var context = BuildContext();
        context.SetNodeOutput("TtsNode", BuildTtsOutput(contentType: "audio/mpeg"));
        context.SetNodeOutput("OtherNode", BuildTtsOutput(contentType: "audio/wav"));

        var renderer = new TemplateRenderer(context);

        var mpeg = await renderer.RenderAsync("{{ TtsNode.ContentType }}");
        var wav = await renderer.RenderAsync("{{ OtherNode.ContentType }}");

        Assert.Equal("audio/mpeg", mpeg);
        Assert.Equal("audio/wav", wav);
    }

    #endregion

    #region Input Access Tests

    [Fact]
    public async Task RenderAsync_InputProperty_Resolves()
    {
        var context = new OrchestrationExecutionContext();
        context.Hydrate(
            Guid.NewGuid(),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ExecutionInterface.Direct,
            JsonDocument.Parse("{\"name\":\"Alice\"}").RootElement,
            JsonDocument.Parse("{\"type\":\"object\"}"));

        var renderer = new TemplateRenderer(context);

        var result = await renderer.RenderAsync("{{ Input.name }}");

        Assert.Equal("Alice", result);
    }

    #endregion

    #region Literal Template Tests

    [Fact]
    public async Task RenderAsync_PlainString_ReturnsUnchanged()
    {
        var context = BuildContext();
        var renderer = new TemplateRenderer(context);

        var result = await renderer.RenderAsync("Hello, world!");

        Assert.Equal("Hello, world!", result);
    }

    [Fact]
    public async Task RenderAsync_EmptyTemplate_ReturnsEmpty()
    {
        var context = BuildContext();
        var renderer = new TemplateRenderer(context);

        var result = await renderer.RenderAsync(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    #endregion
}
