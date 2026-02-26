namespace DonkeyWork.Agents.Orleans.Contracts.Messages;

[GenerateSerializer]
public enum InternalMessageRole
{
    System,
    User,
    Assistant
}
