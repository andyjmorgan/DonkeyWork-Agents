using DonkeyWork.Agents.Common.Sdk.Types;

namespace DonkeyWork.Agents.Common.Sdk.Services;

/// <summary>
/// Context for resolving parameter expressions.
/// </summary>
public interface IResolutionContext
{
    /// <summary>
    /// Gets a variable value by name.
    /// </summary>
    object? GetVariable(string name);

    /// <summary>
    /// Checks if a variable exists.
    /// </summary>
    bool HasVariable(string name);

    /// <summary>
    /// Gets all available variable names.
    /// </summary>
    IEnumerable<string> GetVariableNames();
}

/// <summary>
/// Service for resolving Resolvable&lt;T&gt; expressions at runtime.
/// </summary>
public interface IParameterResolver
{
    /// <summary>
    /// Resolves a Resolvable value to its typed value.
    /// </summary>
    T Resolve<T>(Resolvable<T> resolvable, IResolutionContext context);

    /// <summary>
    /// Resolves a nullable Resolvable value to its typed value.
    /// Returns default(T) if the resolvable has no value.
    /// </summary>
    T? ResolveOrDefault<T>(Resolvable<T>? resolvable, IResolutionContext context);

    /// <summary>
    /// Attempts to resolve a Resolvable value.
    /// </summary>
    bool TryResolve<T>(Resolvable<T> resolvable, IResolutionContext context, out T? value);
}
