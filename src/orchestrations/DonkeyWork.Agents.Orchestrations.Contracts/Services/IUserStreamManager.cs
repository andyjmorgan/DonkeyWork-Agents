namespace DonkeyWork.Agents.Orchestrations.Contracts.Services;

public interface IUserStreamManager
{
    Task EnsureStreamAsync(Guid userId, CancellationToken cancellationToken = default);
}
