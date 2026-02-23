using Asp.Versioning;
using DonkeyWork.Agents.Conversations.Api;
using DonkeyWork.Agents.Orchestrations.Api;
using DonkeyWork.Agents.Credentials.Api;
using DonkeyWork.Agents.Identity.Api;
using DonkeyWork.Agents.Identity.Api.Options;
using DonkeyWork.Agents.Mcp.Api;
using DonkeyWork.Agents.Mcp.Core;
using DonkeyWork.Agents.Notifications.Core;
using DonkeyWork.Agents.Notifications.Core.Hubs;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Services;
using DonkeyWork.Agents.Projects.Api;
using DonkeyWork.Agents.Projects.Api.McpTools;
using DonkeyWork.Agents.Identity.Api.McpTools;
using DonkeyWork.Agents.Providers.Api;
using DonkeyWork.Agents.Storage.Api;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore.Authentication;
using System.Text.Json;
using System.Text.Json.Nodes;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(
        outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();


var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"));

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1.0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Add controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.AllowOutOfOrderMetadataProperties = true;
    });

// Add OpenAPI
builder.Services.AddOpenApi();

// Add Persistence module (provides DbContext)
builder.Services.AddPersistence(builder.Configuration);

// Add Orchestrations module
builder.Services.AddOrchestrationsApi();

// Add Credentials module
builder.Services.AddCredentialsApi();

// Add Identity module
builder.Services.AddIdentityApi(builder.Configuration);

// Add Projects module
builder.Services.AddProjectsApi();

// Add MCP module
builder.Services.AddMcpApi();

// Add Conversations module
builder.Services.AddConversationsApi(builder.Configuration);

builder.Services.AddDynamicMcpServer(typeof(NotesTools).Assembly, typeof(IdentityTools).Assembly);

// Add Providers module
builder.Services.AddProvidersApi();

// Add Storage module
builder.Services.AddStorageApi(builder.Configuration);

// Add Notifications module (includes SignalR)
builder.Services.AddNotificationsCore();

// Configure Data Protection with PostgreSQL storage
builder.Services.AddDataProtection()
    .SetApplicationName("DonkeyWork.Agents")
    .PersistKeysToDbContext<AgentsDbContext>();

builder.Services.AddHealthChecks();
builder.Services.AddHttpClient("KeycloakProxy");

// CORS policy for MCP clients (browser-based clients like MCP Inspector need cross-origin access).
// Set as the default policy so it applies to all requests, including those handled by middleware
// (e.g., /.well-known/oauth-protected-resource served by the MCP auth handler).
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("WWW-Authenticate"));
});

var app = builder.Build();

// Configure forwarded headers for reverse proxy (must be first)
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
};
// Trust all proxies in k3s/docker environment
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Add Serilog request logging
app.UseSerilogRequestLogging();

// Enable developer exception page in all environments for debugging
app.UseDeveloperExceptionPage();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Servers =
        [
            new ScalarServer("https://localhost:5001", "HTTPS"),
            new ScalarServer("http://localhost:5000", "HTTP")
        ];
    });
}

// No HTTPS redirection - API is always behind a traffic shaper/load balancer that handles TLS
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map SignalR hub for real-time notifications
app.MapHub<NotificationHub>("/hubs/notifications");

app.MapMcp().RequireAuthorization(new AuthorizeAttribute
{
    AuthenticationSchemes = McpAuthenticationDefaults.AuthenticationScheme
});

// Proxy OAuth discovery metadata from Keycloak, rewriting the registration_endpoint to route
// through our server. Keycloak's client registration endpoint doesn't support CORS, so
// browser-based MCP clients (like MCP Inspector) can't call it directly.
var keycloakAuthority = app.Services.GetRequiredService<IOptions<KeycloakOptions>>().Value.Authority.TrimEnd('/');

app.MapGet("/.well-known/openid-configuration", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var resourceUri = $"{context.Request.Scheme}://{context.Request.Host}";
    return await ProxyOAuthMetadata(httpClientFactory, keycloakAuthority, "openid-configuration", resourceUri);
});

app.MapGet("/.well-known/oauth-authorization-server", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var resourceUri = $"{context.Request.Scheme}://{context.Request.Host}";
    return await ProxyOAuthMetadata(httpClientFactory, keycloakAuthority, "oauth-authorization-server", resourceUri);
});

// Proxy client registration requests to Keycloak (Keycloak's registration endpoint lacks CORS)
app.MapPost("/oauth/register", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("KeycloakProxy");
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var request = new HttpRequestMessage(HttpMethod.Post, $"{keycloakAuthority}/clients-registrations/openid-connect")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    var response = await client.SendAsync(request);
    var responseBody = await response.Content.ReadAsStringAsync();
    return Results.Content(responseBody, "application/json", statusCode: (int)response.StatusCode);
});

static async Task<IResult> ProxyOAuthMetadata(
    IHttpClientFactory httpClientFactory, string keycloakAuthority, string metadataType, string resourceUri)
{
    var client = httpClientFactory.CreateClient("KeycloakProxy");
    var response = await client.GetStringAsync($"{keycloakAuthority}/.well-known/{metadataType}");
    var metadata = JsonNode.Parse(response);
    if (metadata is not null)
    {
        // Rewrite registration_endpoint to our proxy
        metadata["registration_endpoint"] = $"{resourceUri}/oauth/register";
    }
    return Results.Content(metadata?.ToJsonString() ?? response, "application/json");
}

app.MapHealthChecks("/healthz");

using(var scope = app.Services.CreateScope())
{
    var migrationService = scope.ServiceProvider.GetRequiredService<IMigrationService>();
    await migrationService.MigrateAsync();
}

await app.RunAsync();

namespace DonkeyWork.Agents.Api
{
    /// <summary>
    /// Partial Program class for testing purposes.
    /// </summary>
    public partial class Program { }
}