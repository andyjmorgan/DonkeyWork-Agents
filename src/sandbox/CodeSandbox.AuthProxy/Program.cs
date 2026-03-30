using CodeSandbox.AuthProxy.Configuration;
using CodeSandbox.AuthProxy.Credentials;
using CodeSandbox.AuthProxy.Health;
using CodeSandbox.AuthProxy.Proxy;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

Log.Information("Starting Auth Proxy Sidecar");

var proxyConfig = new ProxyConfiguration();
builder.Configuration.GetSection(nameof(ProxyConfiguration)).Bind(proxyConfig);
builder.Services.AddSingleton(proxyConfig);

builder.WebHost.UseUrls($"http://0.0.0.0:{proxyConfig.HealthPort}");

using var loggerFactory = LoggerFactory.Create(lb => lb.AddSerilog(Log.Logger));
var startupLogger = loggerFactory.CreateLogger("AuthProxy.Startup");
var caCert = CertificateGenerator.LoadOrGenerateCaCertificate(
    proxyConfig.CaCertificatePath,
    proxyConfig.CaPrivateKeyPath,
    startupLogger);

builder.Services.AddSingleton(sp => new CertificateGenerator(
    caCert,
    sp.GetRequiredService<ILogger<CertificateGenerator>>()));

builder.Services.AddSingleton<TlsMitmHandler>();

if (!string.IsNullOrEmpty(proxyConfig.CredentialStoreUrl))
{
    builder.Services.AddSingleton<ICredentialProvider>(sp =>
        new GrpcCredentialProvider(proxyConfig, sp.GetRequiredService<ILogger<GrpcCredentialProvider>>()));
}
else
{
    Log.Warning("No CredentialStoreUrl configured - credential injection disabled");
    builder.Services.AddSingleton<ICredentialProvider>(new NullCredentialProvider());
}

builder.Services.AddHostedService<ProxyServer>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthEndpoints();
app.MapHealthChecks("/health");

Log.Information("Auth Proxy configured: proxy port {ProxyPort}, health port {HealthPort}, blocked domains: {Domains}",
    proxyConfig.ProxyPort, proxyConfig.HealthPort,
    proxyConfig.BlockedDomains.Count > 0 ? string.Join(", ", proxyConfig.BlockedDomains) : "(none)");

Log.Information("Credential provider: {Provider}",
    string.IsNullOrEmpty(proxyConfig.CredentialStoreUrl) ? "None" : "GrpcCredentialProvider");

if (proxyConfig.DynamicCredentialDomains.Count > 0)
{
    Log.Information("Dynamic credential domains: [{Domains}]",
        string.Join(", ", proxyConfig.DynamicCredentialDomains));
}
else
{
    Log.Information("No dynamic credential domains configured");
}

await app.RunAsync();
