namespace DonkeyWork.Agents.Orleans.Contracts.Models;

[GenerateSerializer]
public enum CancelScope
{
    Active,
    Pending,
    Both,
}
