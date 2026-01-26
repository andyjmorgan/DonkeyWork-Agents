namespace DonkeyWork.Agents.Actions.Contracts.Models.Schema;

/// <summary>
/// Schema definition for an action node (used by frontend for UI generation)
/// </summary>
public class ActionNodeSchema
{
    /// <summary>
    /// Unique action type identifier
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the action
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Category for organizing actions
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Optional subcategory/group
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Icon name for UI
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Description of what this action does
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Maximum input connections (-1 = unlimited)
    /// </summary>
    public int MaxInputs { get; set; }

    /// <summary>
    /// Maximum output connections (-1 = unlimited)
    /// </summary>
    public int MaxOutputs { get; set; }

    /// <summary>
    /// Whether this action is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Parameter definitions
    /// </summary>
    public List<ParameterSchema> Parameters { get; set; } = new();
}

/// <summary>
/// Schema definition for an action parameter
/// </summary>
public class ParameterSchema
{
    /// <summary>
    /// Parameter name (property name)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description/help text
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Parameter type (string, number, boolean, enum, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether this parameter is required
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Default value (as string)
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Whether this parameter supports variable expressions
    /// </summary>
    public bool SupportsVariables { get; set; }

    /// <summary>
    /// Editor type for UI control
    /// </summary>
    public string? EditorType { get; set; }

    /// <summary>
    /// Control type (text, slider, dropdown, etc.)
    /// </summary>
    public string? ControlType { get; set; }

    /// <summary>
    /// Options for enum/dropdown types
    /// </summary>
    public List<OptionSchema>? Options { get; set; }

    /// <summary>
    /// Validation rules
    /// </summary>
    public ValidationSchema? Validation { get; set; }

    /// <summary>
    /// Whether this is a Resolvable&lt;T&gt; type
    /// </summary>
    public bool Resolvable { get; set; }

    /// <summary>
    /// Credential types if this is a credential parameter
    /// </summary>
    public string[]? CredentialTypes { get; set; }

    /// <summary>
    /// Dependency information
    /// </summary>
    public DependencySchema? Dependency { get; set; }
}

/// <summary>
/// Option for enum/dropdown parameters
/// </summary>
public class OptionSchema
{
    /// <summary>
    /// Display label
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Actual value
    /// </summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Validation rules for a parameter
/// </summary>
public class ValidationSchema
{
    /// <summary>
    /// Minimum value (for numbers)
    /// </summary>
    public double? Min { get; set; }

    /// <summary>
    /// Maximum value (for numbers)
    /// </summary>
    public double? Max { get; set; }

    /// <summary>
    /// Minimum length (for strings)
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Maximum length (for strings)
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Regular expression pattern
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Step size (for sliders)
    /// </summary>
    public double? Step { get; set; }
}

/// <summary>
/// Dependency information for conditional parameters
/// </summary>
public class DependencySchema
{
    /// <summary>
    /// Name of the parameter this depends on
    /// </summary>
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    /// Condition for showing this parameter
    /// </summary>
    public string? ShowIf { get; set; }
}
