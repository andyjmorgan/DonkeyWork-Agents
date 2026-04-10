using CodeSandbox.Executor.Services;
using CodeSandbox.Executor.Tools.Editing;
using CodeSandbox.Executor.Tools.FileSystem;
using CodeSandbox.Executor.Tools.Reading;
using CodeSandbox.Executor.Tools.Search;
using CodeSandbox.Executor.Tools.Writing;
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

builder.Services.AddSingleton<IFileSystem, PhysicalFileSystem>();
builder.Services.AddSingleton<IRipgrepProvider, RipgrepProvider>();
builder.Services.AddSingleton<ReadTool>();
builder.Services.AddSingleton<EditTool>();
builder.Services.AddSingleton<MultiEditTool>();
builder.Services.AddSingleton<WriteTool>();
builder.Services.AddSingleton<GlobTool>();
builder.Services.AddSingleton<GrepTool>();

builder.Services.AddSingleton<ProcessTracker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ProcessTracker>());

builder.Services.AddSingleton<StdioBridge>();

builder.Services.AddControllers();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8666, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapControllers();
app.UseHealthChecks("/healthz");

Log.Information("Starting Executor on port 8666 (HTTP)");
await app.RunAsync();
