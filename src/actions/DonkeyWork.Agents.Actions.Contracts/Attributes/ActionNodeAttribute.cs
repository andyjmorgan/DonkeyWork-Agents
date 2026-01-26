namespace DonkeyWork.Agents.Actions.Contracts.Attributes;

/// <summary>
/// Defines an action node type with metadata for UI generation
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ActionNodeAttribute : Attribute
{
    /// <summary>
    /// Unique identifier for this action type (e.g., "http_request", "llm_call")
    /// </summary>
    public string ActionType { get; }

    /// <summary>
    /// Category for organizing in UI palette (e.g., "Communication", "AI/ML")
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Optional subcategory/group (e.g., "HTTP", "Language Models")
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Maximum number of input connections (-1 = unlimited)
    /// </summary>
    public int MaxInputs { get; set; } = -1;

    /// <summary>
    /// Maximum number of output connections (-1 = unlimited)
    /// </summary>
    public int MaxOutputs { get; set; } = -1;

    /// <summary>
    /// Whether this action is enabled (for feature flags)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Icon name for UI display (e.g., "globe", "brain", "database")
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Human-readable description of what this action does
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Display name for the action (defaults to action type if not set)
    /// </summary>
    public string? DisplayName { get; set; }

    public ActionNodeAttribute(string actionType, string category)
    {
        ActionType = actionType;
        Category = category;
    }
}
