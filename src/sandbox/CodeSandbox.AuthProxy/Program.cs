using CodeSandbox.AuthProxy.Configuration;
using CodeSandbox.AuthProxy.Credentials;
using CodeSandbox.AuthProxy.Health;
using CodeSandbox.AuthProxy.Proxy;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

Log.Information("Starting Auth Proxy Sidecar");

// Bind configuration
var proxyConfig = new ProxyConfiguration();
builder.Configuration.GetSection(nameof(ProxyConfiguration)).Bind(proxyConfig);
builder.Services.AddSingleton(proxyConfig);

// Configure Kestrel to listen on the health port
builder.WebHost.UseUrls($"http://0.0.0.0:{proxyConfig.HealthPort}");

// Load or generate CA certificate
using var loggerFactory = LoggerFactory.Create(lb => lb.AddSerilog(Log.Logger));
var startupLogger = loggerFactory.CreateLogger("AuthProxy.Startup");
var caCert = CertificateGenerator.LoadOrGenerateCaCertificate(
    proxyConfig.CaCertificatePath,
    proxyConfig.CaPrivateKeyPath,
    startupLogger);

// Register services
builder.Services.AddSingleton(sp => new CertificateGenerator(
    caCert,
    sp.GetRequiredService<ILogger<CertificateGenerator>>()));

builder.Services.AddSingleton<TlsMitmHandler>();

// Register credential provider based on configuration
if (!string.IsNullOrEmpty(proxyConfig.CredentialStoreUrl))
{
    builder.Services.AddSingleton<ICredentialProvider>(sp =>
        new GrpcCredentialProvider(proxyConfig, sp.GetRequiredService<ILogger<GrpcCredentialProvider>>()));
}
else
{
    builder.Services.AddSingleton<ICredentialProvider>(new StaticCredentialProvider(proxyConfig));
}

builder.Services.AddHostedService<ProxyServer>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Map health endpoints
app.MapHealthEndpoints();
app.MapHealthChecks("/health");

Log.Information("Auth Proxy configured: proxy port {ProxyPort}, health port {HealthPort}, blocked domains: {Domains}",
    proxyConfig.ProxyPort, proxyConfig.HealthPort,
    proxyConfig.BlockedDomains.Count > 0 ? string.Join(", ", proxyConfig.BlockedDomains) : "(none)");

if (proxyConfig.DomainCredentials.Count > 0)
{
    foreach (var cred in proxyConfig.DomainCredentials)
    {
        Log.Information("Domain credentials configured for {Domain}: {HeaderCount} header(s) [{Headers}]",
            cred.BaseDomain, cred.Headers.Count, string.Join(", ", cred.Headers.Keys));
    }
}
else
{
    Log.Information("No domain credentials configured - header injection disabled");
}

await app.RunAsync();
