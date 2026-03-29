using DonkeyWork.Agents.A2a.Contracts.Models;

namespace DonkeyWork.Agents.A2a.Contracts.Services;

public interface IA2aServerTestService
{
    Task<TestA2aServerResponseV1> TestConnectionAsync(Guid serverId, CancellationToken cancellationToken = default);

    Task<TestA2aServerResponseV1> DiscoverAsync(string address, CancellationToken cancellationToken = default);
}
