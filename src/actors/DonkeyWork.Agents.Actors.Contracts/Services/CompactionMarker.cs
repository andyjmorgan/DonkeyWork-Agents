namespace DonkeyWork.Agents.Actors.Contracts.Services;

public sealed record CompactionMarker(int AtSequenceNumber, Guid AtTurnId, string Summary);
