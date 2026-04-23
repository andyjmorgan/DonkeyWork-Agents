namespace DonkeyWork.Agents.Orchestrations.Contracts.Services;

/// <summary>
/// Splits markdown text into ordered chunks respecting block boundaries
/// (headings, paragraphs, lists) so each chunk fits within a provider's input limit.
/// </summary>
public interface IMarkdownChunker
{
    /// <summary>
    /// Split the supplied markdown into chunks under <see cref="ChunkerOptions.TargetCharCount"/>.
    /// </summary>
    IReadOnlyList<string> Chunk(string markdown, ChunkerOptions options);
}
