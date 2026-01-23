using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Persistence.Services;

public class MigrationService(AgentsDbContext context, ILogger<MigrationService> logger) : IMigrationService
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Checking for pending database migrations");

        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation("No pending migrations found");
            return;
        }

        logger.LogInformation("Found {Count} pending migrations: {Migrations}", pending.Count, string.Join(", ", pending));

        await context.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Database migrations applied successfully");
    }
}
