using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Scheduling.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScheduleJobType
{
    AgentInvocation,
    Reminder,
    Maintenance,
    Cleanup,
    Archival,
    ReportGeneration,
    WorkflowExecution
}
