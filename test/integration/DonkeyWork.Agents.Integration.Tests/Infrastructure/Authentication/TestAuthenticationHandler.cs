using System.Security.Claims;
using System.Text.Encodings.Web;
using DonkeyWork.Agents.Identity.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;

public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string UserIdHeader = "X-Test-User-Id";
    public const string EmailHeader = "X-Test-Email";
    public const string NameHeader = "X-Test-Name";
    public const string UsernameHeader = "X-Test-Username";

    private readonly IdentityContext _identityContext;

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IdentityContext identityContext)
        : base(options, logger, encoder)
    {
        _identityContext = identityContext;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Get user from headers or use default
        var userIdString = Request.Headers[UserIdHeader].FirstOrDefault();
        var userId = string.IsNullOrEmpty(userIdString)
            ? TestUser.Default.UserId
            : Guid.Parse(userIdString);

        var email = Request.Headers[EmailHeader].FirstOrDefault() ?? TestUser.Default.Email;
        var name = Request.Headers[NameHeader].FirstOrDefault() ?? TestUser.Default.Name;
        var username = Request.Headers[UsernameHeader].FirstOrDefault() ?? TestUser.Default.Username;

        // Set identity context - this is what the real auth handlers do
        _identityContext.SetIdentity(userId, email, name, username);

        // Create claims
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("sub", userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim("email", email),
            new Claim(ClaimTypes.Name, name),
            new Claim("name", name),
            new Claim("preferred_username", username)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
