namespace DonkeyWork.Agents.Persistence.Entities.Credentials;

public class SandboxCustomVariableEntity : BaseEntity
{
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The variable value, encrypted at column level in database.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    public bool IsSecret { get; set; }
}
