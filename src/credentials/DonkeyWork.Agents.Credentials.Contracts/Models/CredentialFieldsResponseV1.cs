using DonkeyWork.Agents.Credentials.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

public class CredentialFieldsResponseV1
{
    public IReadOnlyList<CredentialFieldType> Fields { get; set; } = [];
}
