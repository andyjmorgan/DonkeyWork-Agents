namespace DonkeyWork.Agents.Agents.Contracts.Enums;

/// <summary>
/// Represents the type of a workflow node.
/// </summary>
public enum NodeType
{
    /// <summary>
    /// Start node - entry point that validates input.
    /// </summary>
    Start,

    /// <summary>
    /// Model node - calls an LLM.
    /// </summary>
    Model,

    /// <summary>
    /// End node - signals completion and returns output.
    /// </summary>
    End
}
