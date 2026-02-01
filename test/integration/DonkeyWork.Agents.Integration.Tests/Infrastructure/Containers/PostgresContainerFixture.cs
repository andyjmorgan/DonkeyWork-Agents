using Testcontainers.PostgreSql;

namespace DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;

public class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgresContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg17")
            .WithDatabase("agents_test")
            .WithUsername("test")
            .WithPassword("test")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Enable pgcrypto and pgvector extensions
        await _container.ExecScriptAsync("CREATE EXTENSION IF NOT EXISTS pgcrypto;");
        await _container.ExecScriptAsync("CREATE EXTENSION IF NOT EXISTS vector;");
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
