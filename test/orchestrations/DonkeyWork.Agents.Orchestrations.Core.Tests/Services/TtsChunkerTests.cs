using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Services;

namespace DonkeyWork.Agents.Orchestrations.Core.Tests.Services;

public class TtsChunkerTests
{
    private readonly TtsChunker _chunker = new();

    #region Empty input

    [Fact]
    public void Chunk_EmptyInput_ReturnsEmpty()
    {
        var result = _chunker.Chunk(string.Empty, new ChunkerOptions());

        Assert.Empty(result);
    }

    [Fact]
    public void Chunk_WhitespaceInput_ReturnsEmpty()
    {
        var result = _chunker.Chunk("   \n\n  \t", new ChunkerOptions());

        Assert.Empty(result);
    }

    #endregion

    #region Plain prose

    [Fact]
    public void Chunk_SingleSmallParagraph_ReturnsSingleChunk()
    {
        var result = _chunker.Chunk("Hello world.", new ChunkerOptions());

        Assert.Single(result);
        Assert.Equal("Hello world.", result[0]);
    }

    [Fact]
    public void Chunk_MultipleSmallBlocks_PacksIntoSingleChunk()
    {
        var text = "Paragraph one.\n\nParagraph two.\n\nParagraph three.";

        var result = _chunker.Chunk(text, new ChunkerOptions { TargetCharCount = 500, MaxCharCount = 1000 });

        Assert.Single(result);
        Assert.Contains("Paragraph one", result[0]);
        Assert.Contains("Paragraph three", result[0]);
    }

    [Fact]
    public void Chunk_PacksMultipleParagraphsUntilBudgetExceeded()
    {
        var paragraph = new string('a', 400) + ".";
        var text = string.Join("\n\n", Enumerable.Repeat(paragraph, 5));

        var result = _chunker.Chunk(text, new ChunkerOptions { TargetCharCount = 900, MaxCharCount = 1500 });

        Assert.True(result.Count >= 2, "expected multiple chunks when blocks exceed the target budget together");
        Assert.All(result, chunk => Assert.True(chunk.Length <= 1500, $"chunk exceeds max: {chunk.Length}"));
    }

    [Fact]
    public void Chunk_PrefersParagraphBoundaryOverMidParagraphSplit()
    {
        var text = "First paragraph short.\n\n" + new string('x', 500);

        var result = _chunker.Chunk(text, new ChunkerOptions { TargetCharCount = 300, MaxCharCount = 600 });

        Assert.Equal(2, result.Count);
        Assert.Equal("First paragraph short.", result[0]);
    }

    #endregion

    #region List handling

    [Fact]
    public void Chunk_ShortList_StaysWhole()
    {
        var text = "- item one\n- item two\n- item three";

        var result = _chunker.Chunk(text, new ChunkerOptions());

        Assert.Single(result);
        Assert.Contains("- item one", result[0]);
        Assert.Contains("- item three", result[0]);
    }

    [Fact]
    public void Chunk_LongList_SplitsBetweenItemsNeverMidItem()
    {
        var longItem = new string('y', 200);
        var text = string.Join("\n", Enumerable.Range(1, 8).Select(i => $"- item {i} {longItem}"));

        var result = _chunker.Chunk(text, new ChunkerOptions { TargetCharCount = 500, MaxCharCount = 800 });

        Assert.True(result.Count >= 2, "long list should be split");
        foreach (var chunk in result)
        {
            var firstLine = chunk.Split('\n')[0].TrimStart();
            Assert.StartsWith("-", firstLine);
        }
    }

    #endregion

    #region Sentence-level fallback

    [Fact]
    public void Chunk_ParagraphLargerThanMax_SplitsOnSentenceBoundary()
    {
        var sentence = new string('a', 100) + ".";
        var text = string.Concat(Enumerable.Repeat(sentence + " ", 10));

        var result = _chunker.Chunk(text, new ChunkerOptions { TargetCharCount = 250, MaxCharCount = 300 });

        Assert.True(result.Count >= 3, "oversized paragraph should split across multiple chunks");
        Assert.All(result, chunk =>
            Assert.True(
                chunk.EndsWith(".") || chunk.EndsWith("?") || chunk.EndsWith("!"),
                $"chunk did not end on sentence boundary: '{chunk}'"));
    }

    #endregion

    #region Last-resort hard cut

    [Fact]
    public void Chunk_SingleSentenceOverMax_FallsBackToHardSplit()
    {
        var oversized = new string('z', 1000);
        var options = new ChunkerOptions { TargetCharCount = 200, MaxCharCount = 200 };

        var result = _chunker.Chunk(oversized, options);

        Assert.True(result.Count >= 5);
        Assert.All(result, chunk => Assert.True(chunk.Length <= options.MaxCharCount));
    }

    #endregion

    #region Markdown stripping

    [Fact]
    public void Chunk_StripsBoldAndItalicMarkers()
    {
        var result = _chunker.Chunk("This is **bold** and *italic* text.", new ChunkerOptions());

        Assert.Single(result);
        Assert.Equal("This is bold and italic text.", result[0]);
    }

    [Fact]
    public void Chunk_StripsHeadingsKeepsText()
    {
        var result = _chunker.Chunk("# Title\n\nBody text.", new ChunkerOptions());

        Assert.Single(result);
        Assert.Contains("Title", result[0]);
        Assert.Contains("Body text", result[0]);
        Assert.DoesNotContain("#", result[0]);
    }

    [Fact]
    public void Chunk_KeepsLinkTextDropsUrl()
    {
        var result = _chunker.Chunk("See [the docs](https://example.com/page) for details.", new ChunkerOptions());

        Assert.Single(result);
        Assert.Contains("the docs", result[0]);
        Assert.DoesNotContain("https://", result[0]);
        Assert.DoesNotContain("example.com", result[0]);
    }

    [Fact]
    public void Chunk_KeepsInlineCodeContent()
    {
        var result = _chunker.Chunk("Run `dotnet build` to compile.", new ChunkerOptions());

        Assert.Single(result);
        Assert.Contains("dotnet build", result[0]);
        Assert.DoesNotContain("`", result[0]);
    }

    [Fact]
    public void Chunk_DropsFencedCodeBlocks()
    {
        var input = "Intro paragraph.\n\n```csharp\nvar x = 1;\n```\n\nOutro paragraph.";
        var result = _chunker.Chunk(input, new ChunkerOptions());

        Assert.Single(result);
        Assert.Contains("Intro", result[0]);
        Assert.Contains("Outro", result[0]);
        Assert.DoesNotContain("var x", result[0]);
        Assert.DoesNotContain("```", result[0]);
    }

    [Fact]
    public void Chunk_StripsHtmlTags()
    {
        var result = _chunker.Chunk("Hello <b>world</b>.", new ChunkerOptions());

        Assert.Single(result);
        Assert.Equal("Hello world.", result[0]);
    }

    [Fact]
    public void Chunk_StripsBlockquoteMarkers()
    {
        var result = _chunker.Chunk("> A quoted line.\n> Another quoted line.", new ChunkerOptions());

        Assert.Single(result);
        Assert.Contains("A quoted line", result[0]);
        Assert.DoesNotContain(">", result[0]);
    }

    [Fact]
    public void Chunk_PreservesListMarkersAfterStripping()
    {
        var result = _chunker.Chunk("- **bold item**\n- *italic item*", new ChunkerOptions());

        Assert.Single(result);
        Assert.Contains("- bold item", result[0]);
        Assert.Contains("- italic item", result[0]);
        Assert.DoesNotContain("**", result[0]);
        Assert.DoesNotContain("*italic", result[0]);
    }

    #endregion
}
