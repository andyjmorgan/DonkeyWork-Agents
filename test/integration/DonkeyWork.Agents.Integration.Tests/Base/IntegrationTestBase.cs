using DonkeyWork.Agents.Integration.Tests.Fixtures;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Factories;
using DonkeyWork.Agents.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;

namespace DonkeyWork.Agents.Integration.Tests.Base;

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly InfrastructureFixture Infrastructure;
    protected readonly IntegrationTestWebApplicationFactory Factory;
    private Respawner? _respawner;

    protected IntegrationTestBase(InfrastructureFixture infrastructure)
    {
        Infrastructure = infrastructure;
        Factory = new IntegrationTestWebApplicationFactory(infrastructure);
    }

    public virtual async Task InitializeAsync()
    {
        await Factory.EnsureDatabaseCreatedAsync();

        // Initialize Respawner for database cleanup
        await using var connection = new NpgsqlConnection(Infrastructure.Postgres.ConnectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public", "agents", "credentials", "mcp", "projects", "storage"],
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
    }

    public virtual async Task DisposeAsync()
    {
        await ResetDatabaseAsync();
        await Factory.DisposeAsync();
    }

    protected async Task ResetDatabaseAsync()
    {
        if (_respawner == null) return;

        await using var connection = new NpgsqlConnection(Infrastructure.Postgres.ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    protected AgentsDbContext CreateDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AgentsDbContext>();
    }
}
