namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;

/// <summary>
/// Output from a ChunkText node — an ordered list of chunk strings
/// produced by block-level, boundary-respecting splitting of the input.
/// </summary>
public class ChunkTextNodeOutput : NodeOutput
{
    /// <summary>
    /// The ordered chunks produced from the input text.
    /// </summary>
    public required IReadOnlyList<string> Chunks { get; init; }

    /// <summary>
    /// Number of chunks produced (convenience for template/DAG consumers).
    /// </summary>
    public int ChunkCount => Chunks.Count;

    public override string ToMessageOutput()
    {
        return $"Chunked into {Chunks.Count} piece(s)";
    }
}
