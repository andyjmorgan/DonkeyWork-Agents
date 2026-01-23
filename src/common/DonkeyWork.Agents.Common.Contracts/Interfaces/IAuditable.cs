namespace DonkeyWork.Agents.Common.Contracts.Interfaces;

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; }

    DateTimeOffset? UpdatedAt { get; }
}
