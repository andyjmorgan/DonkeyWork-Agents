namespace DonkeyWork.Agents.Orchestrations.Contracts.Services;

/// <summary>
/// Strips markdown-ish formatting and splits text into ordered chunks suitable
/// for feeding to a TTS engine. Chunk boundaries respect paragraphs, list items,
/// and sentences so the audio output sounds natural.
/// </summary>
public interface ITtsChunker
{
    /// <summary>
    /// Strip formatting from the input and split into chunks under
    /// <see cref="ChunkerOptions.TargetCharCount"/>, capped at
    /// <see cref="ChunkerOptions.MaxCharCount"/>.
    /// </summary>
    IReadOnlyList<string> Chunk(string text, ChunkerOptions options);
}
