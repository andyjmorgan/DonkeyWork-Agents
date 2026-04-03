using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Identity.Core.Services;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;
using DonkeyWork.Agents.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Quartz;

namespace DonkeyWork.Agents.Integration.Tests.Infrastructure.Factories;

public class IntegrationTestWebApplicationFactory : WebApplicationFactory<DonkeyWork.Agents.Api.Program>
{
    private readonly InfrastructureFixture _infrastructure;

    public IntegrationTestWebApplicationFactory(InfrastructureFixture infrastructure)
    {
        _infrastructure = infrastructure;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide configuration values BEFORE options validation runs
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                // Keycloak options (auth is replaced, but validation still runs)
                ["Keycloak:Authority"] = "http://localhost:8080/realms/test",
                ["Keycloak:Audience"] = "test-client",
                ["Keycloak:RequireHttpsMetadata"] = "false",
                ["Keycloak:FrontendUrl"] = "http://localhost:3000",

                // Storage options
                ["Storage:ServiceUrl"] = "http://localhost:9000",
                ["Storage:AccessKey"] = "minioadmin",
                ["Storage:SecretKey"] = "minioadmin",
                ["Storage:DefaultBucket"] = "test-bucket",

                // NATS options
                ["Nats:Url"] = _infrastructure.Nats.Url,

                // Persistence - will be overridden in ConfigureTestServices
                ["Persistence:ConnectionString"] = _infrastructure.Postgres.ConnectionString,
                ["Persistence:EncryptionKey"] = "test-encryption-key-for-integration-tests",

                // Agents options
                ["Agents:ExecutionTimeout"] = "00:05:00",
                ["Agents:StreamRetention"] = "00:30:00",

                // OAuth options
                ["OAuth:TokenRefreshCheckInterval"] = "00:05:00",
                ["OAuth:TokenRefreshWindow"] = "00:10:00",
                ["OAuth:MaxRefreshRetries"] = "3",
                ["OAuth:RefreshRetryDelay"] = "00:01:00",

                // Anthropic options (actors module - not used in tests but validation runs)
                ["Anthropic:ApiKey"] = "test-api-key",

                // Scheduling - use same Postgres connection for Quartz persistent store
                ["Persistence:ConnectionString"] = _infrastructure.Postgres.ConnectionString,
            };

            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AgentsDbContext>>();
            services.RemoveAll<AgentsDbContext>();

            services.RemoveAll<IdentityContext>();
            services.RemoveAll<IIdentityContext>();
            services.AddScoped<IdentityContext>();
            services.AddScoped<IIdentityContext>(sp => sp.GetRequiredService<IdentityContext>());

            services.AddDbContext<AgentsDbContext>((serviceProvider, options) =>
            {
                options.UseNpgsql(_infrastructure.Postgres.ConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                });
            });

            // to avoid "Scheme already exists" errors when re-registering
            services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();
            services.RemoveAll<IPostConfigureOptions<AuthenticationOptions>>();

            // Replace authentication with test handler
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName, _ => { })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    "McpAuth", _ => { });

            // Replace Quartz persistent store with RAMJobStore for tests
            services.AddQuartz(q =>
            {
                q.UseInMemoryStore();
            });

            // NATS connection uses test container URL from config override above
            // No need to re-register - the Nats:Url config points to the test container
        });

        builder.UseEnvironment("Test");
    }

    public async Task EnsureDatabaseCreatedAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AgentsDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
