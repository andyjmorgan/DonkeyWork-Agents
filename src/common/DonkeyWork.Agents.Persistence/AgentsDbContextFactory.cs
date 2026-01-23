using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DonkeyWork.Agents.Persistence;

/// <summary>
/// Factory for creating DbContext instances at design time (migrations).
/// </summary>
public class AgentsDbContextFactory : IDesignTimeDbContextFactory<AgentsDbContext>
{
    public AgentsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetSection(PersistenceOptions.SectionName)
            .Get<PersistenceOptions>()?.ConnectionString
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=donkeywork_agents;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AgentsDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
        });

        return new AgentsDbContext(optionsBuilder.Options);
    }
}
