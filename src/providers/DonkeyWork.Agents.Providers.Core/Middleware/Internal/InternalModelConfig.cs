using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal;

/// <summary>
/// Internal model configuration.
/// </summary>
internal class InternalModelConfig
{
    public required LlmProvider Provider { get; set; }
    public required string ModelId { get; set; }
    public required string ApiKey { get; set; }
}
