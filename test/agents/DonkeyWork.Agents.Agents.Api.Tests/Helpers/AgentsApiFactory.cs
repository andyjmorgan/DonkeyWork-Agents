using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace DonkeyWork.Agents.Agents.Api.Tests.Helpers;

/// <summary>
/// Test authentication handler that always authenticates.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestScheme";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, AgentsApiFactory.TestUserId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim("username", "testuser")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Custom WebApplicationFactory for integration testing the Agents API.
/// </summary>
public class AgentsApiFactory : WebApplicationFactory<Program>
{
    public static readonly Guid TestUserId = Guid.NewGuid();

    public AgentsApiFactory()
    {
        // Reset Serilog logger before creating the host to avoid "logger is already frozen" error
        Log.CloseAndFlush();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Suppress verbose logging in tests
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        builder.ConfigureTestServices(services =>
        {
            // Add test authentication scheme
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, options => { });

            // Mock IIdentityContext to provide a consistent test user ID
            var identityContextMock = new Mock<IIdentityContext>();
            identityContextMock.Setup(x => x.UserId).Returns(TestUserId);
            identityContextMock.Setup(x => x.Email).Returns("test@example.com");
            identityContextMock.Setup(x => x.Username).Returns("testuser");
            identityContextMock.Setup(x => x.Name).Returns("Test User");

            // Replace the real IIdentityContext with the mock
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IIdentityContext));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            services.AddScoped<IIdentityContext>(_ => identityContextMock.Object);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Log.CloseAndFlush();
        }
        base.Dispose(disposing);
    }
}
