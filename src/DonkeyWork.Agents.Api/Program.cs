using System.Security.Cryptography.X509Certificates;
using Asp.Versioning;
using DonkeyWork.Agents.Conversations.Api;
using DonkeyWork.Agents.Orchestrations.Api;
using DonkeyWork.Agents.Credentials.Api;
using DonkeyWork.Agents.Credentials.Api.Interceptors;
using DonkeyWork.Agents.Credentials.Api.Options;
using DonkeyWork.Agents.Credentials.Api.Services;
using DonkeyWork.Agents.Identity.Api;
using DonkeyWork.Agents.Identity.Api.Options;
using DonkeyWork.Agents.A2a.Api;
using DonkeyWork.Agents.Mcp.Api;
using DonkeyWork.Agents.Notifications.Core;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Services;
using DonkeyWork.Agents.Projects.Api;
using DonkeyWork.Agents.Projects.Api.McpTools;
using DonkeyWork.Agents.Identity.Api.McpTools;
using DonkeyWork.Agents.Providers.Api;
using DonkeyWork.Agents.Storage.Api;
using DonkeyWork.Agents.Actors.Api;
using DonkeyWork.Agents.AgentDefinitions.Api;
using DonkeyWork.Agents.Prompts.Api;
using DonkeyWork.Agents.Scheduling.Api;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using DonkeyWork.Agents.Orchestrations.Contracts;
using DonkeyWork.Agents.Orchestrations.Contracts.Messages;
using DonkeyWork.Agents.Orchestrations.Core.Handlers;
using Scalar.AspNetCore;
using Serilog;
using Wolverine;
using Wolverine.Nats;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(
        outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"));

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

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.AllowOutOfOrderMetadataProperties = true;
    });

builder.Services.AddOpenApi();

builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddOrchestrationsApi();

builder.Services.AddCredentialsApi();

builder.Services.AddIdentityApi(builder.Configuration);

builder.Services.AddProjectsApi();

builder.Services.AddMcpApi(typeof(NotesTools).Assembly, typeof(IdentityTools).Assembly);

builder.Services.AddA2aApi();

builder.Services.AddConversationsApi(builder.Configuration);

builder.Services.AddAgentDefinitionsApi();

builder.Services.AddPromptsApi();

builder.Services.AddProvidersApi();

builder.Services.AddStorageApi(builder.Configuration);

builder.Host.AddActorsApi(builder.Configuration);
builder.Services.AddActorsServices(builder.Configuration);

builder.Host.UseWolverine(opts =>
{
    var natsUrl = builder.Configuration["Nats:Url"] ?? "nats://localhost:4222";
    opts.UseNats(natsUrl)
        .AutoProvision()
        .UseJetStream(_ => { })
        .DefineWorkQueueStream(NatsSubjects.CommandStream, NatsSubjects.CommandSubject);

    opts.PublishMessage<ExecuteOrchestrationCommand>()
        .ToNatsSubject(NatsSubjects.CommandSubject)
        .UseJetStream(NatsSubjects.CommandStream);

    opts.ListenToNatsSubject(NatsSubjects.CommandSubject)
        .UseJetStream(NatsSubjects.CommandStream, NatsSubjects.CommandConsumer)
        .Sequential();

    opts.Discovery.IncludeAssembly(typeof(ExecuteOrchestrationHandler).Assembly);

    opts.Policies.Failures.MaximumAttempts = 1;
});

builder.Services.AddSchedulingApi(builder.Configuration);

builder.Services.AddNotificationsCore();

var grpcOptions = builder.Configuration.GetSection(InternalGrpcOptions.SectionName).Get<InternalGrpcOptions>();
if (grpcOptions?.Enabled == true)
{
    builder.Services.AddGrpc(options =>
    {
        options.Interceptors.Add<InternalGrpcInterceptor>();
    });
    builder.Services.AddSingleton<InternalGrpcInterceptor>();

    builder.WebHost.ConfigureKestrel(options =>
    {
        // Main HTTP port
        options.ListenAnyIP(8080, o => o.Protocols = HttpProtocols.Http1AndHttp2);

        // Internal gRPC port with mTLS
        options.ListenAnyIP(grpcOptions.Port, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;

            if (File.Exists(grpcOptions.ServerCertPath) && File.Exists(grpcOptions.ServerKeyPath))
            {
                listenOptions.UseHttps(httpsOptions =>
                {
                    httpsOptions.ServerCertificate = X509Certificate2.CreateFromPemFile(
                        grpcOptions.ServerCertPath, grpcOptions.ServerKeyPath);

                    if (File.Exists(grpcOptions.CaCertPath))
                    {
                        httpsOptions.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.RequireCertificate;
                        httpsOptions.ClientCertificateValidation = (cert, chain, errors) =>
                        {
                            var caCert = X509CertificateLoader.LoadCertificateFromFile(grpcOptions.CaCertPath);
                            using var customChain = new X509Chain();
                            customChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                            customChain.ChainPolicy.CustomTrustStore.Add(caCert);
                            customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                            return customChain.Build(cert);
                        };
                    }
                });
            }
        });
    });
}

builder.Services.AddDataProtection()
    .SetApplicationName("DonkeyWork.Agents")
    .PersistKeysToDbContext<AgentsDbContext>();

builder.Services.AddHealthChecks();

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

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
};
// Trust all proxies in k3s/docker environment
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex is null && httpContext.Response.StatusCode < 400 &&
            httpContext.Request.Path.StartsWithSegments("/healthz"))
        {
            return Serilog.Events.LogEventLevel.Verbose;
        }

        return Serilog.Events.LogEventLevel.Information;
    };
});

app.UseDeveloperExceptionPage();

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
app.UseWebSockets();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapActorsEndpoints();

app.MapNotifications();

app.UseMcpApi();

// Redirect OAuth/OIDC discovery to Keycloak for clients that don't implement RFC 9728
var keycloakAuthority = app.Services.GetRequiredService<IOptions<KeycloakOptions>>().Value.Authority.TrimEnd('/');
app.MapGet("/.well-known/openid-configuration",
    () => Results.Redirect($"{keycloakAuthority}/.well-known/openid-configuration", permanent: true));
app.MapGet("/.well-known/oauth-authorization-server",
    () => Results.Redirect($"{keycloakAuthority}/.well-known/openid-configuration", permanent: true));

app.MapHealthChecks("/healthz");

if (grpcOptions?.Enabled == true)
{
    app.MapGrpcService<CredentialStoreGrpcService>();
}

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