using CodeSandbox.Executor.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        new CompactJsonFormatter(),
        "logs/execution-.log",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

builder.Services.AddGrpc();

builder.Services.AddSingleton<ProcessTracker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ProcessTracker>());

builder.Services.AddSingleton<StdioBridge>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8666, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddHealthChecks();
var app = builder.Build();

app.MapGrpcService<ExecutorGrpcService>();
app.MapGrpcService<McpServerGrpcService>();

// Keep health check endpoint (uses HTTP/2 but works fine)
app.UseHealthChecks("/healthz");

Log.Information("Starting gRPC server on port 8666 (HTTP/2)");
await app.RunAsync();
