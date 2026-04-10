using CodeSandbox.Manager.Configuration;
using CodeSandbox.Manager.Endpoints;
using CodeSandbox.Manager.Services.Background;
using CodeSandbox.Manager.Services.Container;
using CodeSandbox.Manager.Services.Executor;
using CodeSandbox.Manager.Services.Mcp;
using CodeSandbox.Manager.Services.Terminal;
using k8s;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });
});

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

Log.Information("Starting Sandbox Manager API");

builder.Services.AddOptions<SandboxManagerConfig>()
    .BindConfiguration(nameof(SandboxManagerConfig))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var managerConfig = builder.Configuration.GetSection(nameof(SandboxManagerConfig)).Get<SandboxManagerConfig>();
if (!string.IsNullOrEmpty(managerConfig?.RedisConnectionString))
{
    var redis = ConnectionMultiplexer.Connect(managerConfig.RedisConnectionString);
    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

    builder.Services.AddDataProtection()
        .PersistKeysToStackExchangeRedis(redis, "CodeSandbox:DataProtectionKeys");

    Log.Information("Redis configured for data protection key persistence");
}

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

builder.Services.AddHttpClient("executor", client =>
{
    client.Timeout = TimeSpan.FromMinutes(10);
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ISandboxService, SandboxService>();
builder.Services.AddScoped<IMcpContainerService, McpContainerService>();
builder.Services.AddScoped<ITerminalService, TerminalService>();
builder.Services.AddScoped<IExecutorClient, ExecutorClient>();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation { Name = "codesandbox", Version = "1.0.0" };
    })
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithToolsFromAssembly();

builder.Services.AddHostedService<ContainerCleanupService>();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

builder.Services.AddOpenApi();

var app = builder.Build();

var config = app.Services.GetRequiredService<IOptions<SandboxManagerConfig>>().Value;
Log.Information("Configuration loaded: Namespace={Namespace}, RuntimeClass={RuntimeClass}",
    config.TargetNamespace, config.RuntimeClassName);

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

app.MapOpenApi();
app.MapScalarApiReference("/scalar", (options, context) =>
{
    options.AddServer(new ScalarServer($"https://{context.Request.Host}"));
    options.AddServer(new ScalarServer($"http://{context.Request.Host}"));
});

Log.Information("API documentation enabled at /scalar/v1");

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapHealthChecks("/healthz");

app.MapTerminalEndpoints();

app.MapSandboxEndpoints();
app.MapMcpEndpoints();
app.MapMcp("/mcp");

Log.Information("Sandbox Manager API started successfully");
await app.RunAsync();
