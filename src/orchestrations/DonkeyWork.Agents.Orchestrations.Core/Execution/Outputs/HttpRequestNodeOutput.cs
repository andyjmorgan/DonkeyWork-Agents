namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;

/// <summary>
/// Output from an HTTP Request node execution.
/// </summary>
public sealed class HttpRequestNodeOutput : NodeOutput
{
    /// <summary>
    /// HTTP status code of the response.
    /// </summary>
    public required int StatusCode { get; init; }

    /// <summary>
    /// Response body as a string.
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// Response headers.
    /// </summary>
    public Dictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// Whether the request was successful (2xx status code).
    /// </summary>
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;

    public override string ToMessageOutput() => Body;
}
