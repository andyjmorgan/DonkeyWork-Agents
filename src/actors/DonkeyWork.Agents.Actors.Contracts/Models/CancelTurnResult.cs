namespace DonkeyWork.Agents.Actors.Contracts.Models;

[GenerateSerializer]
public enum CancelTurnResult
{
    Active,
    Pending,
    NotFound,
}
