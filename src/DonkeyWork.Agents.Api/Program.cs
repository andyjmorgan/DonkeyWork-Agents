using Asp.Versioning;
using DonkeyWork.Agents.Orchestrations.Api;
using DonkeyWork.Agents.Credentials.Api;
using DonkeyWork.Agents.Identity.Api;
using DonkeyWork.Agents.Mcp.Core;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Services;
using DonkeyWork.Agents.Projects.Api;
using DonkeyWork.Agents.Projects.Api.McpTools;
using DonkeyWork.Agents.Providers.Api;
using DonkeyWork.Agents.Storage.Api;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
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

builder.Services.AddDynamicMcpServer(typeof(NotesTools).Assembly);

// Add Providers module
builder.Services.AddProvidersApi();

// Add Storage module
builder.Services.AddStorageApi(builder.Configuration);

// Configure Data Protection with PostgreSQL storage
builder.Services.AddDataProtection()
    .SetApplicationName("DonkeyWork.Agents")
    .PersistKeysToDbContext<AgentsDbContext>();

builder.Services.AddHealthChecks();

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapMcp().RequireAuthorization();

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