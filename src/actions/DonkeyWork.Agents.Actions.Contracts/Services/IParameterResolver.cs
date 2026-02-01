using DonkeyWork.Agents.Common.Sdk.Types;

namespace DonkeyWork.Agents.Actions.Contracts.Services;

/// <summary>
/// Service for resolving parameter values (literals and expressions)
/// </summary>
public interface IParameterResolver
{
    /// <summary>
    /// Resolve a Resolvable&lt;T&gt; to its actual value
    /// </summary>
    /// <typeparam name="T">Target type</typeparam>
    /// <param name="resolvable">Resolvable parameter</param>
    /// <param name="context">Evaluation context for expressions</param>
    /// <returns>Resolved value</returns>
    T Resolve<T>(Resolvable<T> resolvable, object? context = null);

    /// <summary>
    /// Resolve a string that may contain expressions
    /// </summary>
    /// <param name="value">String value (may contain {{expressions}})</param>
    /// <param name="context">Evaluation context</param>
    /// <returns>Resolved string</returns>
    string ResolveString(string value, object? context = null);

    /// <summary>
    /// Resolve a variable expression to a dictionary of headers
    /// </summary>
    /// <param name="variable">Variable expression (e.g., "{{headers}}")</param>
    /// <param name="context">Evaluation context</param>
    /// <returns>Dictionary of header key-value pairs</returns>
    Dictionary<string, string> ResolveHeaders(string variable, object? context = null);
}
