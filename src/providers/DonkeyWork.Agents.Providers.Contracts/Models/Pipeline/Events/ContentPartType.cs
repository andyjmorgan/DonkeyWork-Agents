using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events;

/// <summary>
/// Type of content part in a model response.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContentPartType
{
    /// <summary>
    /// Text content.
    /// </summary>
    Text,

    /// <summary>
    /// Thinking/reasoning content.
    /// </summary>
    Thinking,

    /// <summary>
    /// Image content.
    /// </summary>
    Image,

    /// <summary>
    /// Tool use request.
    /// </summary>
    ToolUse,

    /// <summary>
    /// Tool result.
    /// </summary>
    ToolResult
}
