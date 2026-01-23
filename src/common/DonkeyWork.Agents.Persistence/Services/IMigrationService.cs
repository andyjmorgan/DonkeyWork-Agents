namespace DonkeyWork.Agents.Persistence.Services;

public interface IMigrationService
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}