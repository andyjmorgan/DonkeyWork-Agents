using DonkeyWork.Agents.Orleans.Contracts.Grains;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orleans.Core;

public class GrainContext
{
    public string GrainKey { get; set; } = "";
    public Guid UserId { get; set; }
    public string ConversationId { get; set; } = "";
    public IAgentResponseObserver? Observer { get; set; }
    public IGrainFactory GrainFactory { get; set; } = null!;
    public ILogger Logger { get; set; } = null!;
    public Action<string>? ProgressCallback { get; set; }

    public void ReportProgress(string breadcrumb) => ProgressCallback?.Invoke(breadcrumb);
}
