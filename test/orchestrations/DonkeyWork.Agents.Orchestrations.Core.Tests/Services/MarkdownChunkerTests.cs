using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Services;

namespace DonkeyWork.Agents.Orchestrations.Core.Tests.Services;

public class MarkdownChunkerTests
{
    private readonly MarkdownChunker _chunker = new();

    #region Happy path

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
        var markdown = "Paragraph one.\n\nParagraph two.\n\nParagraph three.";

        var result = _chunker.Chunk(markdown, new ChunkerOptions { TargetCharCount = 500, MaxCharCount = 1000 });

        Assert.Single(result);
        Assert.Contains("Paragraph one", result[0]);
        Assert.Contains("Paragraph three", result[0]);
    }

    #endregion

    #region Block boundary preference

    [Fact]
    public void Chunk_PacksMultipleParagraphsUntilBudgetExceeded()
    {
        var paragraph = new string('a', 400) + ".";
        var markdown = string.Join("\n\n", Enumerable.Repeat(paragraph, 5));

        var result = _chunker.Chunk(markdown, new ChunkerOptions { TargetCharCount = 900, MaxCharCount = 1500 });

        Assert.True(result.Count >= 2, "expected multiple chunks when blocks exceed the target budget together");
        Assert.All(result, chunk => Assert.True(chunk.Length <= 1500, $"chunk exceeds max: {chunk.Length}"));
    }

    [Fact]
    public void Chunk_PrefersParagraphBoundaryOverMidParagraphSplit()
    {
        var markdown = "First paragraph short.\n\n" + new string('x', 500);

        var result = _chunker.Chunk(markdown, new ChunkerOptions { TargetCharCount = 300, MaxCharCount = 600 });

        Assert.Equal(2, result.Count);
        Assert.Equal("First paragraph short.", result[0]);
    }

    #endregion

    #region List handling

    [Fact]
    public void Chunk_ShortList_StaysWhole()
    {
        var markdown = "- item one\n- item two\n- item three";

        var result = _chunker.Chunk(markdown, new ChunkerOptions());

        Assert.Single(result);
        Assert.Contains("- item one", result[0]);
        Assert.Contains("- item three", result[0]);
    }

    [Fact]
    public void Chunk_LongList_SplitsBetweenItemsNeverMidItem()
    {
        var longItem = new string('y', 200);
        var markdown = string.Join("\n", Enumerable.Range(1, 8).Select(i => $"- item {i} {longItem}"));

        var result = _chunker.Chunk(markdown, new ChunkerOptions { TargetCharCount = 500, MaxCharCount = 800 });

        Assert.True(result.Count >= 2, "long list should be split");
        foreach (var chunk in result)
        {
            // every chunk must start with a list-item marker once whitespace is trimmed
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
        var markdown = string.Concat(Enumerable.Repeat(sentence + " ", 10));

        var result = _chunker.Chunk(markdown, new ChunkerOptions { TargetCharCount = 250, MaxCharCount = 300 });

        Assert.True(result.Count >= 3, "oversized paragraph should split across multiple chunks");
        // every chunk should end with a sentence terminator (the hard cut fallback is only for sentences > maxCharCount)
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

    #region Heading handling

    [Fact]
    public void Chunk_SectionsWithHeadings_PacksUntilBudgetThenBreaks()
    {
        var section = "## Heading\n\n" + new string('p', 400);
        var markdown = string.Join("\n\n", Enumerable.Repeat(section, 4));

        var result = _chunker.Chunk(markdown, new ChunkerOptions { TargetCharCount = 800, MaxCharCount = 1500 });

        Assert.True(result.Count >= 2);
        Assert.All(result, chunk => Assert.True(chunk.Length <= 1500));
    }

    #endregion
}
