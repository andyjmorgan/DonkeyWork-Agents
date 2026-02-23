using System.Security.Claims;
using DonkeyWork.Agents.Identity.Api.Authentication;
using DonkeyWork.Agents.Identity.Api.Options;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Identity.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using ModelContextProtocol.Authentication;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

namespace DonkeyWork.Agents.Identity.Api;

public static class DependencyInjection
{
    private const string DefaultScheme = "MultiAuth";

    public static IServiceCollection AddIdentityApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Enable PII logging in development for debugging
        IdentityModelEventSource.ShowPII = true;

        // Configure options with validation
        services.AddOptions<KeycloakOptions>()
            .BindConfiguration(KeycloakOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var keycloakOptions = configuration
            .GetSection(KeycloakOptions.SectionName)
            .Get<KeycloakOptions>() ?? new KeycloakOptions();

        // Register memory cache for identity caching
        services.AddMemoryCache();

        // Register identity context (scoped)
        services.AddScoped<IdentityContext>();
        services.AddScoped<IIdentityContext>(sp => sp.GetRequiredService<IdentityContext>());

        // Register HttpClientFactory for general use (e.g., AuthController)
        services.AddHttpClient();

        // Register Keycloak service with typed HttpClient (use internal URL to avoid hairpinning)
        var keycloakBaseUrl = keycloakOptions.InternalAuthority ?? keycloakOptions.Authority;
        services.AddHttpClient<IKeycloakService, KeycloakService>(client =>
        {
            client.BaseAddress = new Uri(keycloakBaseUrl.TrimEnd('/') + "/");
        });

        // Configure authentication with both JWT Bearer and API Key
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = DefaultScheme;
                options.DefaultChallengeScheme = DefaultScheme;
            })
            .AddPolicyScheme(DefaultScheme, "JWT or API Key", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    // If X-Api-Key header is present, use API key auth
                    if (context.Request.Headers.ContainsKey(ApiKeyAuthenticationHandler.HeaderName))
                        return ApiKeyAuthenticationHandler.SchemeName;

                    // Otherwise, use JWT Bearer
                    return JwtBearerDefaults.AuthenticationScheme;
                };
            })
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationHandler.SchemeName, _ => { })
            .AddJwtBearer(options =>
            {
                // Use internal URL for metadata if configured (avoids hairpinning in k8s)
                var metadataAuthority = keycloakOptions.InternalAuthority ?? keycloakOptions.Authority;
                options.Authority = metadataAuthority;
                options.Audience = keycloakOptions.Audience;
                options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Validate issuer against external URL (what's in the token)
                    ValidateIssuer = true,
                    ValidIssuer = keycloakOptions.Authority,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    // Keycloak uses 'sub' for user ID
                    NameClaimType = ClaimTypes.NameIdentifier,
                    // Custom audience validator: Keycloak puts client_id in 'azp' claim, not 'aud'
                    AudienceValidator = (audiences, securityToken, validationParameters) =>
                    {
                        var validAudience = keycloakOptions.Audience;

                        // Check standard 'aud' claim
                        if (audiences.Contains(validAudience))
                            return true;

                        // Check 'azp' (authorized party) claim by decoding the JWT directly
                        if (securityToken is JsonWebToken jwt)
                        {
                            if (jwt.TryGetPayloadValue<string>("azp", out var azp) && azp == validAudience)
                                return true;
                        }

                        return false;
                    }
                };

                options.Events = new JwtBearerEvents
                {
                    // Extract token from query string for SignalR WebSocket connections
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        // If token is in query string and path is a SignalR hub, use it
                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();
                        var identityContext = context.HttpContext.RequestServices
                            .GetRequiredService<IdentityContext>();

                        var principal = context.Principal;
                        if (principal?.Identity?.IsAuthenticated != true)
                        {
                            return Task.CompletedTask;
                        }

                        var subClaim = principal.FindFirst("sub")?.Value
                            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                        if (string.IsNullOrEmpty(subClaim))
                        {
                            logger.LogError("Token validated but 'sub' claim is missing");
                            context.Fail("Missing 'sub' claim");
                            return Task.CompletedTask;
                        }

                        if (!Guid.TryParse(subClaim, out var userId))
                        {
                            logger.LogError("Token 'sub' claim is not a valid GUID: {SubClaim}", subClaim);
                            context.Fail("Invalid 'sub' claim format - expected GUID");
                            return Task.CompletedTask;
                        }

                        var email = principal.FindFirst("email")?.Value
                            ?? principal.FindFirst(ClaimTypes.Email)?.Value;
                        var name = principal.FindFirst("name")?.Value;
                        var username = principal.FindFirst("preferred_username")?.Value;

                        identityContext.SetIdentity(userId, email, name, username);

                        return Task.CompletedTask;
                    }
                };
            })
            .AddMcp(options =>
            {
                options.ForwardAuthenticate = DefaultScheme;
                options.ResourceMetadataUri = new Uri("/.well-known/oauth-protected-resource", UriKind.Relative);
                options.ResourceMetadata = new ProtectedResourceMetadata
                {
                    AuthorizationServers = { new Uri(keycloakOptions.Authority) },
                };
                options.Events.OnResourceMetadataRequest = context =>
                {
                    context.ResourceMetadata ??= new ProtectedResourceMetadata
                    {
                        AuthorizationServers = { new Uri(keycloakOptions.Authority) },
                    };
                    context.ResourceMetadata.Resource ??=
                        new Uri($"{context.Request.Scheme}://{context.Request.Host}");
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorization();

        return services;
    }
}
