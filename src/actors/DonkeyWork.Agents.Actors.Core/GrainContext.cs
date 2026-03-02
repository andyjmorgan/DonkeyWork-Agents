using DonkeyWork.Agents.Actors.Contracts.Grains;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core;

public class GrainContext
{
    public string GrainKey { get; set; } = "";
    public string ConversationId { get; set; } = "";
    public string UserId { get; set; } = "";
    public IAgentResponseObserver? Observer { get; set; }
    public IGrainFactory GrainFactory { get; set; } = null!;
    public ILogger Logger { get; set; } = null!;
    public Action<string>? ProgressCallback { get; set; }
    public string? SeaweedFsBaseUrl { get; set; }

    public void ReportProgress(string breadcrumb) => ProgressCallback?.Invoke(breadcrumb);
}
