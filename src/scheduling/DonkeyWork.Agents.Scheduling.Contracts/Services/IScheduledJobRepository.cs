using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using DonkeyWork.Agents.Scheduling.Contracts.Models;

namespace DonkeyWork.Agents.Scheduling.Contracts.Services;

public interface IScheduledJobRepository
{
    Task<ScheduledJobDetailV1> CreateAsync(Guid scheduleId, CreateScheduleRequestV1 request, string quartzJobKey, string quartzTriggerKey, CancellationToken ct = default);

    Task<ScheduledJobDetailV1?> GetAsync(Guid id, CancellationToken ct = default);

    Task<ScheduledJobDetailV1?> GetByQuartzJobKeyAsync(string quartzJobKey, CancellationToken ct = default);

    Task<PaginatedResponse<ScheduledJobSummaryV1>> ListAsync(
        ScheduleJobType? jobType = null,
        ScheduleTargetType? targetType = null,
        ScheduleMode? scheduleMode = null,
        bool? isEnabled = null,
        bool includeSystem = false,
        PaginationRequest? pagination = null,
        CancellationToken ct = default);

    Task<ScheduledJobDetailV1?> UpdateAsync(Guid id, UpdateScheduleRequestV1 request, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<bool> SetEnabledAsync(Guid id, bool enabled, CancellationToken ct = default);
}
