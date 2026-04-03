using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using DonkeyWork.Agents.Scheduling.Contracts.Models;

namespace DonkeyWork.Agents.Scheduling.Contracts.Services;

public interface IScheduledJobExecutionRepository
{
    Task<Guid> CreateAsync(Guid scheduledJobId, Guid userId, ScheduleTriggerSource triggerSource, string? quartzFireInstanceId, string? executingNodeId, CancellationToken ct = default);

    Task UpdateCompletionAsync(Guid id, ScheduleExecutionStatus status, string? errorDetails, string? outputSummary, Guid? correlationId, CancellationToken ct = default);

    Task<ScheduledJobExecutionV1?> GetAsync(Guid id, CancellationToken ct = default);

    Task<PaginatedResponse<ScheduledJobExecutionV1>> ListByScheduleIdAsync(Guid scheduledJobId, PaginationRequest? pagination = null, CancellationToken ct = default);
}
