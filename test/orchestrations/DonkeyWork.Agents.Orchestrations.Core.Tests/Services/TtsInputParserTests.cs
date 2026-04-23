using DonkeyWork.Agents.Orchestrations.Core.Execution.Helpers;

namespace DonkeyWork.Agents.Orchestrations.Core.Tests.Services;

public class TtsInputParserTests
{
    [Fact]
    public void Parse_SingleElementArray_ReturnsOneChunk()
    {
        var result = TtsInputParser.Parse("[\"hello world\"]");

        Assert.Single(result);
        Assert.Equal("hello world", result[0]);
    }

    [Fact]
    public void Parse_MultipleElementArray_PreservesOrder()
    {
        var result = TtsInputParser.Parse("[\"first\",\"second\",\"third\"]");

        Assert.Equal(3, result.Count);
        Assert.Equal("first", result[0]);
        Assert.Equal("second", result[1]);
        Assert.Equal("third", result[2]);
    }

    [Fact]
    public void Parse_ArrayWithEmptyStrings_SkipsWhitespace()
    {
        var result = TtsInputParser.Parse("[\"one\",\"\",\"two\",\"   \"]");

        Assert.Equal(2, result.Count);
        Assert.Equal("one", result[0]);
        Assert.Equal("two", result[1]);
    }

    [Fact]
    public void Parse_EmptyString_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => TtsInputParser.Parse(string.Empty));
    }

    [Fact]
    public void Parse_NotJson_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TtsInputParser.Parse("just plain text"));
        Assert.Contains("JSON array", ex.Message);
    }

    [Fact]
    public void Parse_JsonObject_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TtsInputParser.Parse("{\"text\":\"hi\"}"));
        Assert.Contains("Object", ex.Message);
    }

    [Fact]
    public void Parse_ArrayOfNumbers_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TtsInputParser.Parse("[1,2,3]"));
        Assert.Contains("only strings", ex.Message);
    }

    [Fact]
    public void Parse_EmptyArray_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TtsInputParser.Parse("[]"));
        Assert.Contains("empty after rendering", ex.Message);
    }

    [Fact]
    public void Parse_ArrayWithOnlyWhitespaceStrings_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TtsInputParser.Parse("[\"\",\"  \"]"));
        Assert.Contains("empty after rendering", ex.Message);
    }

    [Fact]
    public void Parse_MultilineChunks_PreservesNewlines()
    {
        var result = TtsInputParser.Parse("[\"line one\\nline two\",\"second chunk\"]");

        Assert.Equal(2, result.Count);
        Assert.Equal("line one\nline two", result[0]);
    }
}
