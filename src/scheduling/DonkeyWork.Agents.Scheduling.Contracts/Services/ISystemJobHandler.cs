namespace DonkeyWork.Agents.Scheduling.Contracts.Services;

public interface ISystemJobHandler
{
    Task ExecuteAsync(CancellationToken ct = default);
}
