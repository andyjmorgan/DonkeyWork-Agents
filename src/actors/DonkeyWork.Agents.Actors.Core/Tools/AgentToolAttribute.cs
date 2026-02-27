namespace DonkeyWork.Agents.Actors.Core.Tools;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AgentToolAttribute : Attribute
{
    public string? Name { get; }

    public string? DisplayName { get; set; }

    public AgentToolAttribute() { }

    public AgentToolAttribute(string name) => Name = name;
}
