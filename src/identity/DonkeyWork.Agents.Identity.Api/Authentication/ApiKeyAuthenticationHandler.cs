using System.Security.Claims;
using System.Text.Encodings.Web;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Identity.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Identity.Api.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IUserApiKeyService _apiKeyService;
    private readonly IKeycloakService _keycloakService;
    private readonly IMemoryCache _cache;

    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IUserApiKeyService apiKeyService,
        IKeycloakService keycloakService,
        IMemoryCache cache)
        : base(options, logger, encoder)
    {
        _apiKeyService = apiKeyService;
        _keycloakService = keycloakService;
        _cache = cache;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var apiKeyHeader))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = apiKeyHeader.ToString();
        if (string.IsNullOrEmpty(apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        // Validate API key and get user ID
        var userId = await _apiKeyService.ValidateAsync(apiKey);
        if (userId == null)
        {
            Logger.LogWarning("Invalid API key provided");
            return AuthenticateResult.Fail("Invalid API key");
        }

        // Try to get cached identity
        var cacheKey = $"identity:{userId}";
        if (!_cache.TryGetValue<CachedIdentity>(cacheKey, out var cachedIdentity))
        {
            // Fetch from Keycloak - we need an admin token or service account for this
            // For now, we'll create a minimal identity with just the user ID
            // In production, you'd use Keycloak Admin API to get user details
            cachedIdentity = new CachedIdentity
            {
                UserId = userId.Value,
                Email = null,
                Name = null,
                Username = null
            };

            // Cache for 30-60 seconds
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(45));
            _cache.Set(cacheKey, cachedIdentity, cacheOptions);
        }

        // Populate identity context - get the scoped instance from the request services
        // (auth handlers are singletons, but IdentityContext is scoped per request)
        var identityContext = Context.RequestServices.GetRequiredService<IdentityContext>();
        identityContext.SetIdentity(
            cachedIdentity!.UserId,
            cachedIdentity.Email,
            cachedIdentity.Name,
            cachedIdentity.Username);

        // Create claims principal
        var claims = new List<Claim>
        {
            new("sub", cachedIdentity.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, cachedIdentity.UserId.ToString())
        };

        if (!string.IsNullOrEmpty(cachedIdentity.Email))
            claims.Add(new Claim(ClaimTypes.Email, cachedIdentity.Email));
        if (!string.IsNullOrEmpty(cachedIdentity.Name))
            claims.Add(new Claim(ClaimTypes.Name, cachedIdentity.Name));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }

    private sealed class CachedIdentity
    {
        public Guid UserId { get; init; }
        public string? Email { get; init; }
        public string? Name { get; init; }
        public string? Username { get; init; }
    }
}

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}
