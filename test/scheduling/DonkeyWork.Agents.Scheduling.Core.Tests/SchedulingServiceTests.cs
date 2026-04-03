using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using DonkeyWork.Agents.Scheduling.Contracts.Models;
using DonkeyWork.Agents.Scheduling.Contracts.Services;
using DonkeyWork.Agents.Scheduling.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;

namespace DonkeyWork.Agents.Scheduling.Core.Tests;

public class SchedulingServiceTests
{
    private readonly Mock<IScheduledJobRepository> _jobRepoMock;
    private readonly Mock<IScheduledJobExecutionRepository> _execRepoMock;
    private readonly Mock<ISchedulerFactory> _schedulerFactoryMock;
    private readonly Mock<IScheduler> _schedulerMock;
    private readonly SchedulingService _service;

    public SchedulingServiceTests()
    {
        _jobRepoMock = new Mock<IScheduledJobRepository>();
        _execRepoMock = new Mock<IScheduledJobExecutionRepository>();
        _schedulerFactoryMock = new Mock<ISchedulerFactory>();
        _schedulerMock = new Mock<IScheduler>();

        _schedulerFactoryMock
            .Setup(f => f.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_schedulerMock.Object);

        var options = new SchedulingServiceOptions
        {
            DefaultTimeZone = "Europe/Dublin",
            MinimumCronIntervalHours = 4,
        };

        _service = new SchedulingService(
            _jobRepoMock.Object,
            _execRepoMock.Object,
            _schedulerFactoryMock.Object,
            options,
            Mock.Of<ILogger<SchedulingService>>());
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRecurringRequest_PersistsAndSchedules()
    {
        var request = new CreateScheduleRequestV1
        {
            Name = "Test Recurring",
            ScheduleMode = ScheduleMode.Recurring,
            CronExpression = "0 0 8 * * ?",
            JobType = ScheduleJobType.AgentInvocation,
            TargetType = ScheduleTargetType.Navi,
            UserPrompt = "Do something",
        };

        _jobRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<CreateScheduleRequestV1>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduledJobDetailV1
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                CreatedAt = DateTimeOffset.UtcNow,
            });

        var result = await _service.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        _jobRepoMock.Verify(r => r.CreateAsync(It.IsAny<CreateScheduleRequestV1>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _schedulerMock.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidOneOffRequest_PersistsAndSchedules()
    {
        var request = new CreateScheduleRequestV1
        {
            Name = "Test OneOff",
            ScheduleMode = ScheduleMode.OneOff,
            RunAtUtc = DateTimeOffset.UtcNow.AddHours(2),
            JobType = ScheduleJobType.Reminder,
            TargetType = ScheduleTargetType.Navi,
            UserPrompt = "Remind me",
        };

        _jobRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<CreateScheduleRequestV1>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduledJobDetailV1
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                CreatedAt = DateTimeOffset.UtcNow,
            });

        var result = await _service.CreateAsync(request);

        Assert.NotNull(result);
        _schedulerMock.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCron_ThrowsArgumentException()
    {
        var request = new CreateScheduleRequestV1
        {
            Name = "Bad Cron",
            ScheduleMode = ScheduleMode.Recurring,
            CronExpression = "not a cron",
            JobType = ScheduleJobType.AgentInvocation,
            TargetType = ScheduleTargetType.Navi,
            UserPrompt = "test",
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithCronUnder4Hours_ThrowsArgumentException()
    {
        var request = new CreateScheduleRequestV1
        {
            Name = "Too Frequent",
            ScheduleMode = ScheduleMode.Recurring,
            CronExpression = "0 0 * * * ?",
            JobType = ScheduleJobType.AgentInvocation,
            TargetType = ScheduleTargetType.Navi,
            UserPrompt = "test",
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_SetsDefaultTimezone()
    {
        var request = new CreateScheduleRequestV1
        {
            Name = "No TZ",
            ScheduleMode = ScheduleMode.OneOff,
            RunAtUtc = DateTimeOffset.UtcNow.AddHours(2),
            JobType = ScheduleJobType.Reminder,
            TargetType = ScheduleTargetType.Navi,
            UserPrompt = "test",
        };

        _jobRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<CreateScheduleRequestV1>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduledJobDetailV1 { Id = Guid.NewGuid(), Name = request.Name, CreatedAt = DateTimeOffset.UtcNow });

        await _service.CreateAsync(request);

        _jobRepoMock.Verify(r => r.CreateAsync(
            It.Is<CreateScheduleRequestV1>(req => req.TimeZoneId == "Europe/Dublin"),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingSchedule_DeletesFromBothDbAndQuartz()
    {
        var id = Guid.NewGuid();
        _jobRepoMock.Setup(r => r.GetAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduledJobDetailV1
            {
                Id = id,
                QuartzJobKey = "scheduled-jobs.schedule-" + id,
            });
        _jobRepoMock.Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _schedulerMock.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.DeleteAsync(id);

        Assert.True(result);
        _schedulerMock.Verify(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Once);
        _jobRepoMock.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistent_ReturnsFalse()
    {
        _jobRepoMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScheduledJobDetailV1?)null);

        var result = await _service.DeleteAsync(Guid.NewGuid());
        Assert.False(result);
    }

    #endregion

    #region EnableAsync / DisableAsync Tests

    [Fact]
    public async Task EnableAsync_ExistingSchedule_ResumesQuartzJob()
    {
        var id = Guid.NewGuid();
        _jobRepoMock.Setup(r => r.GetAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduledJobDetailV1 { Id = id, QuartzJobKey = "scheduled-jobs.schedule-" + id });
        _jobRepoMock.Setup(r => r.SetEnabledAsync(id, true, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.EnableAsync(id);

        Assert.True(result);
        _schedulerMock.Verify(s => s.ResumeJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisableAsync_ExistingSchedule_PausesQuartzJob()
    {
        var id = Guid.NewGuid();
        _jobRepoMock.Setup(r => r.GetAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduledJobDetailV1 { Id = id, QuartzJobKey = "scheduled-jobs.schedule-" + id });
        _jobRepoMock.Setup(r => r.SetEnabledAsync(id, false, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.DisableAsync(id);

        Assert.True(result);
        _schedulerMock.Verify(s => s.PauseJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region TriggerAsync Tests

    [Fact]
    public async Task TriggerAsync_ExistingSchedule_TriggersQuartzJob()
    {
        var id = Guid.NewGuid();
        _jobRepoMock.Setup(r => r.GetAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduledJobDetailV1 { Id = id, Name = "Test", QuartzJobKey = "scheduled-jobs.schedule-" + id });

        var result = await _service.TriggerAsync(id);

        Assert.True(result);
        _schedulerMock.Verify(s => s.TriggerJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_DelegatesWithFilters()
    {
        _jobRepoMock
            .Setup(r => r.ListAsync(
                ScheduleJobType.Reminder, null, null, true, false,
                It.IsAny<PaginationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResponse<ScheduledJobSummaryV1>
            {
                Items = [],
                TotalCount = 0,
                Offset = 0,
                Limit = 50,
            });

        var result = await _service.ListAsync(
            jobType: ScheduleJobType.Reminder,
            isEnabled: true);

        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    #endregion
}
