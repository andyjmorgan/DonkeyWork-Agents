namespace DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;

public class InfrastructureFixture : IAsyncLifetime
{
    public PostgresContainerFixture Postgres { get; } = new();
    public NatsContainerFixture Nats { get; } = new();

    public async Task InitializeAsync()
    {
        // Start containers in parallel for faster startup
        await Task.WhenAll(
            Postgres.InitializeAsync(),
            Nats.InitializeAsync());
    }

    public async Task DisposeAsync()
    {
        await Postgres.DisposeAsync();
        await Nats.DisposeAsync();
    }
}
