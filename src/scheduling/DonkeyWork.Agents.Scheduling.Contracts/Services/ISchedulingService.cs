using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using DonkeyWork.Agents.Scheduling.Contracts.Models;

namespace DonkeyWork.Agents.Scheduling.Contracts.Services;

public interface ISchedulingService
{
    Task<CreateScheduleResponseV1> CreateAsync(CreateScheduleRequestV1 request, CancellationToken ct = default);

    Task<ScheduledJobDetailV1?> GetAsync(Guid id, CancellationToken ct = default);

    Task<PaginatedResponse<ScheduledJobSummaryV1>> ListAsync(
        ScheduleJobType? jobType = null,
        ScheduleTargetType? targetType = null,
        ScheduleMode? scheduleMode = null,
        bool? isEnabled = null,
        bool includeSystem = false,
        PaginationRequest? pagination = null,
        CancellationToken ct = default);

    Task<UpdateScheduleResponseV1?> UpdateAsync(Guid id, UpdateScheduleRequestV1 request, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<bool> EnableAsync(Guid id, CancellationToken ct = default);

    Task<bool> DisableAsync(Guid id, CancellationToken ct = default);

    Task<bool> TriggerAsync(Guid id, CancellationToken ct = default);

    Task<PaginatedResponse<ScheduledJobExecutionV1>> ListExecutionsAsync(Guid scheduleId, PaginationRequest? pagination = null, CancellationToken ct = default);

    Task<ScheduledJobExecutionV1?> GetExecutionAsync(Guid executionId, CancellationToken ct = default);
}
