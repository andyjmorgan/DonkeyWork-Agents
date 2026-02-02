using System.Reflection;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Providers;

/// <summary>
/// Information about a discovered node method.
/// </summary>
public sealed class NodeMethodInfo
{
    /// <summary>
    /// The node type this method handles.
    /// </summary>
    public required NodeType NodeType { get; init; }

    /// <summary>
    /// The provider type that contains this method.
    /// </summary>
    public required Type ProviderType { get; init; }

    /// <summary>
    /// The method to invoke for execution.
    /// </summary>
    public required MethodInfo Method { get; init; }

    /// <summary>
    /// The configuration type expected as the first parameter.
    /// </summary>
    public required Type ConfigType { get; init; }

    /// <summary>
    /// The output type (from Task&lt;T&gt;).
    /// </summary>
    public required Type OutputType { get; init; }

    /// <summary>
    /// Whether the method has a CancellationToken parameter.
    /// </summary>
    public bool HasCancellationToken { get; init; }
}
