namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal;

/// <summary>
/// Internal context passed through the middleware pipeline.
/// </summary>
internal class ModelMiddlewareContext
{
    /// <summary>
    /// Conversation history. Mutable for re-entrancy.
    /// </summary>
    public required List<InternalMessage> Messages { get; set; }

    /// <summary>
    /// LLM configuration.
    /// </summary>
    public required InternalModelConfig Model { get; set; }

    /// <summary>
    /// Tool definitions available for this request.
    /// </summary>
    public InternalToolContext? ToolContext { get; set; }

    /// <summary>
    /// Middleware-shared state.
    /// </summary>
    public Dictionary<string, object> Variables { get; } = new();

    /// <summary>
    /// Provider-specific parameters.
    /// </summary>
    public Dictionary<string, object> ProviderParameters { get; set; } = new();
}

/// <summary>
/// Well-known variable keys.
/// </summary>
internal static class MiddlewareVariables
{
    public const string MaxToolExecutions = "MaxToolExecutions";
    public const string ParallelToolCalls = "ParallelToolCalls";
}
