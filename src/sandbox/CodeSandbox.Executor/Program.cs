using CodeSandbox.Executor.Services;
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

// Configure HTTP JSON options (used by TypedResults including SSE)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = false;
});

// Add controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;
    });

// Add ProcessTracker as a singleton hosted service
builder.Services.AddSingleton<ProcessTracker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ProcessTracker>());

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Listen on all interfaces for Docker compatibility
builder.WebHost.UseUrls("http://0.0.0.0:8666");
builder.Services.AddHealthChecks();
var app = builder.Build();

app.UseCors();
app.MapControllers();
app.UseHealthChecks("/healthz");

Log.Information("Starting HTTP+SSE server on port 8666");
await app.RunAsync();
