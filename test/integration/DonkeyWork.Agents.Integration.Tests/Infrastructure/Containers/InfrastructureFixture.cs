namespace DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;

public class InfrastructureFixture : IAsyncLifetime
{
    public PostgresContainerFixture Postgres { get; } = new();
    public NatsContainerFixture Nats { get; } = new();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            Postgres.InitializeAsync(),
            Nats.InitializeAsync());

        Environment.SetEnvironmentVariable("Nats__Url", Nats.Url);
    }

    public async Task DisposeAsync()
    {
        await Postgres.DisposeAsync();
        await Nats.DisposeAsync();
    }
}
