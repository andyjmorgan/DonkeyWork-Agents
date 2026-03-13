using CodeSandbox.Manager.Configuration;
using CodeSandbox.Manager.Endpoints;
using CodeSandbox.Manager.Services.Background;
using CodeSandbox.Manager.Services.Container;
using CodeSandbox.Manager.Services.Mcp;
using CodeSandbox.Manager.Services.Terminal;
using k8s;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel: HTTP/1.1 on port 8080 for REST + WebSocket
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
    });
});

// Add Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

Log.Information("Starting Sandbox Manager API");

// Configure and validate SandboxManagerConfig with IOptions
builder.Services.AddOptions<SandboxManagerConfig>()
    .BindConfiguration(nameof(SandboxManagerConfig))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Register Kubernetes client
builder.Services.AddSingleton<IKubernetes>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var config = sp.GetRequiredService<IOptions<SandboxManagerConfig>>().Value;

    // Priority 1: Try in-cluster configuration
    try
    {
        var k8sConfig = KubernetesClientConfiguration.InClusterConfig();
        logger.LogInformation("Using in-cluster Kubernetes configuration");
        return new Kubernetes(k8sConfig);
    }
    catch (Exception)
    {
        logger.LogInformation("Not running in-cluster, checking for direct connection config");
    }

    // Priority 2: Use direct connection from appsettings
    if (config.Connection?.ServerUrl != null && config.Connection?.Token != null)
    {
        logger.LogInformation("Using direct Kubernetes connection: {ServerUrl}", config.Connection.ServerUrl);
        var k8sConfig = new KubernetesClientConfiguration
        {
            Host = config.Connection.ServerUrl,
            AccessToken = config.Connection.Token,
            SkipTlsVerify = config.Connection.SkipTlsVerify
        };
        return new Kubernetes(k8sConfig);
    }

    // Priority 3: Fall back to kubeconfig
    logger.LogInformation("Using kubeconfig from default location");
    var defaultConfig = KubernetesClientConfiguration.BuildDefaultConfig();
    return new Kubernetes(defaultConfig);
});

// Register application services
builder.Services.AddScoped<ISandboxService, SandboxService>();
builder.Services.AddScoped<IMcpContainerService, McpContainerService>();
builder.Services.AddScoped<ITerminalService, TerminalService>();

// Register background services
builder.Services.AddHostedService<ContainerCleanupService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

// Add OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Validate configuration on startup
var config = app.Services.GetRequiredService<IOptions<SandboxManagerConfig>>().Value;
Log.Information("Configuration loaded: Namespace={Namespace}, RuntimeClass={RuntimeClass}",
    config.TargetNamespace, config.RuntimeClassName);

// Configure HTTP pipeline
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        var path = httpContext.Request.Path.Value ?? "";
        if (path.Equals("/healthz", StringComparison.OrdinalIgnoreCase))
        {
            return Serilog.Events.LogEventLevel.Debug;
        }
        return Serilog.Events.LogEventLevel.Information;
    };
});

// Enable OpenAPI and Scalar
app.MapOpenApi();
app.MapScalarApiReference("/scalar", (options, context) =>
{
    options.AddServer(new ScalarServer($"https://{context.Request.Host}"));
    options.AddServer(new ScalarServer($"http://{context.Request.Host}"));
});

Log.Information("API documentation enabled at /scalar/v1");

// Enable WebSockets for terminal connections (HTTP/1.1, port 8080)
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// Map health check endpoint
app.MapHealthChecks("/healthz");

// Map WebSocket terminal endpoint
app.MapTerminalEndpoints();

// Map REST endpoints (HTTP/1.1, port 8080)
app.MapSandboxEndpoints();
app.MapMcpEndpoints();

Log.Information("Sandbox Manager API started successfully");
await app.RunAsync();
