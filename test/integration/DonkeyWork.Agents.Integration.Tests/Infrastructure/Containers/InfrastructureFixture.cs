namespace DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;

public class InfrastructureFixture : IAsyncLifetime
{
    public PostgresContainerFixture Postgres { get; } = new();
    public RabbitMqContainerFixture RabbitMq { get; } = new();

    public async Task InitializeAsync()
    {
        // Start containers in parallel for faster startup
        await Task.WhenAll(
            Postgres.InitializeAsync(),
            RabbitMq.InitializeAsync());
    }

    public async Task DisposeAsync()
    {
        await Postgres.DisposeAsync();
        await RabbitMq.DisposeAsync();
    }
}
