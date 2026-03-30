using DonkeyWork.Agents.Persistence.Interceptors;
using DonkeyWork.Agents.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(PersistenceOptions.SectionName)
            .Get<PersistenceOptions>() ?? new PersistenceOptions();

        services.Configure<PersistenceOptions>(
            configuration.GetSection(PersistenceOptions.SectionName));

        services.AddSingleton<AuditableInterceptor>();

        services.AddDbContext<AgentsDbContext>((serviceProvider, dbContextOptions) =>
        {
            var auditableInterceptor = serviceProvider.GetRequiredService<AuditableInterceptor>();

            dbContextOptions
                .UseNpgsql(options.ConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                })
                .AddInterceptors(auditableInterceptor);
        });

        services.AddDbContextFactory<AgentsDbContext>((serviceProvider, dbContextOptions) =>
        {
            dbContextOptions
                .UseNpgsql(options.ConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                });
        });

        services.AddScoped<IMigrationService, MigrationService>();

        return services;
    }
}
