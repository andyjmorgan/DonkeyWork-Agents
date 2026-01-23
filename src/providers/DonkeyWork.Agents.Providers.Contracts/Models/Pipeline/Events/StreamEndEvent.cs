using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events;

/// <summary>
/// End of the model response stream.
/// </summary>
public class StreamEndEvent : ModelPipelineEvent
{
    /// <summary>
    /// The reason the stream ended.
    /// </summary>
    public required PipelineStopReason Reason { get; set; }

    /// <summary>
    /// Token usage information.
    /// </summary>
    public TokenUsage? Usage { get; set; }
}

/// <summary>
/// Reason why the pipeline stream ended.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PipelineStopReason
{
    /// <summary>
    /// Natural completion.
    /// </summary>
    EndTurn,

    /// <summary>
    /// Hit token limit.
    /// </summary>
    MaxTokens,

    /// <summary>
    /// Blocked by content filter.
    /// </summary>
    ContentFilter,

    /// <summary>
    /// Request was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// An error occurred.
    /// </summary>
    Error
}

/// <summary>
/// Token usage information.
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// Number of input tokens.
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Number of output tokens.
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Total tokens.
    /// </summary>
    public int TotalTokens => InputTokens + OutputTokens;
}
