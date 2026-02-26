using System.Net;
using DonkeyWork.Agents.Orchestrations.Api.Options;
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
using RabbitMQ.Stream.Client;

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
            // Add test configuration that will satisfy ValidateOnStart validation
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

                // RabbitMQ Stream options - will be overridden in ConfigureTestServices
                ["RabbitMqStream:Host"] = _infrastructure.RabbitMq.HostName,
                ["RabbitMqStream:Port"] = _infrastructure.RabbitMq.StreamPort.ToString(),
                ["RabbitMqStream:Username"] = "guest",
                ["RabbitMqStream:Password"] = "guest",
                ["RabbitMqStream:VirtualHost"] = "/",

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

                // Orleans options
                ["Orleans:SeaweedFsBaseUrl"] = "http://localhost:8888"
            };

            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext registration
            services.RemoveAll<DbContextOptions<AgentsDbContext>>();
            services.RemoveAll<AgentsDbContext>();

            // Register test IdentityContext
            services.RemoveAll<IdentityContext>();
            services.RemoveAll<IIdentityContext>();
            services.AddScoped<IdentityContext>();
            services.AddScoped<IIdentityContext>(sp => sp.GetRequiredService<IdentityContext>());

            // Add DbContext with TestContainers PostgreSQL
            services.AddDbContext<AgentsDbContext>((serviceProvider, options) =>
            {
                options.UseNpgsql(_infrastructure.Postgres.ConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                });
            });

            // Remove all existing auth option configurations (JWT, API Key, McpAuth, MultiAuth)
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

            // Remove and re-register StreamSystem with our test container config
            services.RemoveAll<StreamSystem>();
            services.AddSingleton(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<RabbitMqStreamOptions>>().Value;
                var config = new StreamSystemConfig
                {
                    UserName = opts.Username,
                    Password = opts.Password,
                    VirtualHost = opts.VirtualHost,
                    Endpoints = new List<EndPoint> { new DnsEndPoint(opts.Host, opts.Port) }
                };
                return StreamSystem.Create(config).GetAwaiter().GetResult();
            });
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
