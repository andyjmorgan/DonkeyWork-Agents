using CodeSandbox.Executor.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using Serilog.Formatting.Compact;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        new CompactJsonFormatter(),
        "logs/execution-.log",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog();

// Add gRPC services
builder.Services.AddGrpc();

// Add ProcessTracker as a singleton hosted service
builder.Services.AddSingleton<ProcessTracker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ProcessTracker>());

// Add StdioBridge as a singleton (one MCP process per pod)
builder.Services.AddSingleton<StdioBridge>();

// Configure Kestrel for HTTP/2 (required for gRPC over plaintext)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8666, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddHealthChecks();
var app = builder.Build();

// Map gRPC services
app.MapGrpcService<ExecutorGrpcService>();
app.MapGrpcService<McpServerGrpcService>();

// Keep health check endpoint (uses HTTP/2 but works fine)
app.UseHealthChecks("/healthz");

Log.Information("Starting gRPC server on port 8666 (HTTP/2)");
await app.RunAsync();
