using System.ComponentModel;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using DonkeyWork.Agents.Scheduling.Contracts.Models;
using DonkeyWork.Agents.Scheduling.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Tools.Scheduling;

public sealed class SchedulingAgentTools
{
    private readonly ISchedulingService _schedulingService;

    public SchedulingAgentTools(ISchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    [AgentTool("create_schedule", DisplayName = "Create Schedule")]
    [Description("""
        Create a one-off or recurring scheduled job.

        For recurring schedules: set scheduleMode to "Recurring" and provide cronExpression as a Quartz 7-field cron string.
        Format: "seconds minute hour day-of-month month day-of-week year". Example: "0 0 8 ? * MON-FRI *" for 8am weekdays.
        5-field Linux cron (e.g. "0 8 * * 1-5") is also accepted and will be auto-translated. Minimum interval is 4 hours.

        For one-off schedules: set scheduleMode to "OneOff" and provide runAtUtc as a raw ISO 8601 datetime string
        WITHOUT quotes (e.g. 2026-04-03T21:00:00Z, NOT "2026-04-03T21:00:00Z"). The value must be a bare datetime, not a quoted string.

        Calculate runAtUtc from the user's intent relative to the current time. If the user says "in 2 hours", compute the
        absolute UTC timestamp. If they say "tomorrow at 9am", compute the UTC equivalent using Europe/Dublin timezone.
        """)]
    public async Task<ToolResult> CreateSchedule(
        [Description("Name for the schedule")] string name,
        [Description("The prompt/instruction to send to the agent when it runs")] string userPrompt,
        [Description("Schedule mode: OneOff or Recurring")] ScheduleMode scheduleMode,
        [Description("Job type: AgentInvocation, Reminder, ReportGeneration, etc.")] ScheduleJobType jobType = ScheduleJobType.AgentInvocation,
        [Description("Quartz 7-field cron expression for recurring schedules. Example: 0 0 8 ? * MON-FRI *")] string? cronExpression = null,
        [Description("UTC datetime for one-off schedules as bare ISO 8601 (e.g. 2026-04-03T21:00:00Z). Do NOT wrap in quotes.")] DateTimeOffset? runAtUtc = null,
        [Description("Target type: Navi, CustomAgent, or Orchestration")] ScheduleTargetType targetType = ScheduleTargetType.Navi,
        [Description("Agent definition ID (GUID) when targeting a custom agent")] Guid? targetAgentDefinitionId = null,
        [Description("IANA timezone (e.g. Europe/Dublin). Defaults to Europe/Dublin.")] string? timezone = null,
        [Description("Optional description of what this schedule does")] string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            var request = new CreateScheduleRequestV1
            {
                Name = name,
                Description = description,
                JobType = jobType,
                ScheduleMode = scheduleMode,
                CronExpression = cronExpression,
                RunAtUtc = runAtUtc,
                TimeZoneId = timezone,
                TargetType = targetType,
                TargetAgentDefinitionId = targetAgentDefinitionId,
                UserPrompt = userPrompt,
            };

            var result = await _schedulingService.CreateAsync(request, ct);
            return ToolResult.Json(result);
        }
        catch (ArgumentException ex)
        {
            return ToolResult.Error(ex.Message);
        }
    }

    [AgentTool("list_schedules", DisplayName = "List Schedules")]
    [Description("List the user's scheduled jobs with their status and next run times.")]
    public async Task<ToolResult> ListSchedules(CancellationToken ct = default)
    {
        var result = await _schedulingService.ListAsync(ct: ct);
        return ToolResult.Json(result);
    }

    [AgentTool("get_schedule", DisplayName = "Get Schedule")]
    [Description("Get details of a specific scheduled job including payload and fire times.")]
    public async Task<ToolResult> GetSchedule(
        [Description("The schedule ID")] Guid scheduleId,
        CancellationToken ct = default)
    {
        var detail = await _schedulingService.GetAsync(scheduleId, ct);
        return detail is not null ? ToolResult.Json(detail) : ToolResult.NotFound("Schedule", scheduleId);
    }

    [AgentTool("update_schedule", DisplayName = "Update Schedule")]
    [Description("Update a scheduled job's configuration, cron expression, prompt, or other fields.")]
    public async Task<ToolResult> UpdateSchedule(
        [Description("The schedule ID")] Guid scheduleId,
        [Description("New name")] string? name = null,
        [Description("New cron expression")] string? cronExpression = null,
        [Description("New user prompt")] string? userPrompt = null,
        [Description("New description")] string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            var request = new UpdateScheduleRequestV1
            {
                Name = name,
                CronExpression = cronExpression,
                UserPrompt = userPrompt,
                Description = description,
            };

            var result = await _schedulingService.UpdateAsync(scheduleId, request, ct);
            return result is not null ? ToolResult.Json(result) : ToolResult.NotFound("Schedule", scheduleId);
        }
        catch (ArgumentException ex)
        {
            return ToolResult.Error(ex.Message);
        }
    }

    [AgentTool("delete_schedule", DisplayName = "Delete Schedule")]
    [Description("Delete a scheduled job.")]
    public async Task<ToolResult> DeleteSchedule(
        [Description("The schedule ID")] Guid scheduleId,
        CancellationToken ct = default)
    {
        var result = await _schedulingService.DeleteAsync(scheduleId, ct);
        return result
            ? ToolResult.Success($"Schedule {scheduleId} deleted.")
            : ToolResult.NotFound("Schedule", scheduleId);
    }

    [AgentTool("trigger_schedule", DisplayName = "Trigger Schedule")]
    [Description("Manually trigger a scheduled job to run immediately.")]
    public async Task<ToolResult> TriggerSchedule(
        [Description("The schedule ID")] Guid scheduleId,
        CancellationToken ct = default)
    {
        var result = await _schedulingService.TriggerAsync(scheduleId, ct);
        return result
            ? ToolResult.Success($"Schedule {scheduleId} triggered for immediate execution.")
            : ToolResult.NotFound("Schedule", scheduleId);
    }

    [AgentTool("list_schedule_executions", DisplayName = "List Schedule Executions")]
    [Description("List recent execution history for a scheduled job.")]
    public async Task<ToolResult> ListScheduleExecutions(
        [Description("The schedule ID")] Guid scheduleId,
        CancellationToken ct = default)
    {
        var result = await _schedulingService.ListExecutionsAsync(scheduleId, ct: ct);
        return ToolResult.Json(result);
    }
}
