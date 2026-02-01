using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;

/// <summary>
/// Defines the types of nodes available in the workflow editor.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NodeType
{
    /// <summary>
    /// Entry point that validates input against the input schema.
    /// </summary>
    Start,

    /// <summary>
    /// Signals completion and returns output.
    /// </summary>
    End,

    /// <summary>
    /// Calls an LLM with configured prompts and parameters.
    /// </summary>
    Model,

    /// <summary>
    /// Formats messages using Scriban templates.
    /// </summary>
    MessageFormatter,

    /// <summary>
    /// Makes HTTP requests to external APIs.
    /// </summary>
    HttpRequest,

    /// <summary>
    /// Pauses execution for a specified duration.
    /// </summary>
    Sleep
}
