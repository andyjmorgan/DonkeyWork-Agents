using System.Text.Json;

namespace DonkeyWork.Agents.Orleans.Core.Providers;

internal abstract record ResponsePart;

internal sealed record TextPart(string Text) : ResponsePart;

internal sealed record ThinkingPart(string Text, string? Signature) : ResponsePart;

internal sealed record ToolUsePart(string ToolUseId, string ToolName, JsonElement Input) : ResponsePart;

internal sealed record ServerToolUsePart(string ToolUseId, string ToolName, JsonElement? Input) : ResponsePart;

internal sealed record CitationPart(string Title, string Url, string CitedText) : ResponsePart;

internal sealed record UsagePart(int InputTokens, int OutputTokens, int CachedInputTokens, int WebSearchRequests, int WebFetchRequests) : ResponsePart;

internal class ResponsePartsBuilder
{
    private readonly List<ResponsePart> _parts = [];
    private readonly System.Text.StringBuilder _textBuffer = new();
    private readonly System.Text.StringBuilder _thinkingBuffer = new();
    private string? _thinkingSignature;

    public IReadOnlyList<ResponsePart> Parts => _parts;
    public string AccumulatedText => _textBuffer.ToString();
    public string AccumulatedThinking => _thinkingBuffer.ToString();

    public void AppendText(string text)
    {
        _textBuffer.Append(text);
    }

    public void AppendThinking(string text)
    {
        _thinkingBuffer.Append(text);
    }

    public void SetThinkingSignature(string signature)
    {
        _thinkingSignature = signature;
    }

    public void Add(ResponsePart part)
    {
        _parts.Add(part);
    }

    public void FlushText()
    {
        if (_textBuffer.Length > 0)
        {
            _parts.Add(new TextPart(_textBuffer.ToString()));
            _textBuffer.Clear();
        }
    }

    public void FlushThinking()
    {
        if (_thinkingBuffer.Length > 0)
        {
            _parts.Add(new ThinkingPart(_thinkingBuffer.ToString(), _thinkingSignature));
            _thinkingBuffer.Clear();
            _thinkingSignature = null;
        }
    }

    public void FlushAll()
    {
        FlushThinking();
        FlushText();
    }
}
