namespace DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events;

/// <summary>
/// An error occurred in the pipeline.
/// </summary>
public class ErrorEvent : ModelPipelineEvent
{
    /// <summary>
    /// Error message.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Error code, if available.
    /// </summary>
    public string? Code { get; set; }
}
