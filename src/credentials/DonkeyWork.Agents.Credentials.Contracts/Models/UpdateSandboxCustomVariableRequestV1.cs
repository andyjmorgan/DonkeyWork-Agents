namespace DonkeyWork.Agents.Credentials.Contracts.Models;

public sealed class UpdateSandboxCustomVariableRequestV1
{
    public string? Value { get; set; }

    public bool? IsSecret { get; set; }
}
