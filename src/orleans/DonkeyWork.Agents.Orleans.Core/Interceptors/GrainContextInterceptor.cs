using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace DonkeyWork.Agents.Orleans.Core.Interceptors;

public sealed class GrainContextInterceptor : IIncomingGrainCallFilter
{
    private readonly ILogger<GrainContextInterceptor> _logger;

    public GrainContextInterceptor(ILogger<GrainContextInterceptor> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var grainKey = context.TargetId.Key.ToString();
        _logger.LogDebug("Grain call: {GrainType}.{Method} key={Key}",
            context.TargetId.Type,
            context.ImplementationMethod?.Name ?? "unknown",
            grainKey);

        await context.Invoke();
    }
}
