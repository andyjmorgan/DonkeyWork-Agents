namespace DonkeyWork.Agents.Orchestrations.Contracts.Services;

/// <summary>
/// Configuration for the markdown chunker.
/// </summary>
public sealed class ChunkerOptions
{
    /// <summary>
    /// Target character budget per chunk. The chunker will pack blocks up to this size.
    /// </summary>
    public int TargetCharCount { get; init; } = 3000;

    /// <summary>
    /// Hard ceiling for chunk size. Used when a single block exceeds the target and
    /// further splitting is needed (between list items, sentences, or as a last resort
    /// a hard char cut).
    /// </summary>
    public int MaxCharCount { get; init; } = 3800;
}
