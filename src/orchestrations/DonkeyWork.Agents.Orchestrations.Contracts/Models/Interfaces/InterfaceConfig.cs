namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;

/// <summary>
/// Base configuration for an orchestration interface.
/// </summary>
public class InterfaceConfig
{
    /// <summary>
    /// Whether this interface is enabled for the orchestration.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Display name for this interface instance.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description of what this interface does.
    /// </summary>
    public string? Description { get; set; }
}
