using DonkeyWork.Agents.Actors.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Contracts.Services;
using DonkeyWork.Agents.AgentDefinitions.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Scheduling;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using DonkeyWork.Agents.Scheduling.Contracts.Services;
using DonkeyWork.Agents.Scheduling.Core.Quartz;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;

namespace DonkeyWork.Agents.Scheduling.Core.Tests;

public class ScheduledTaskJobTests : IDisposable
{
    private readonly AgentsDbContext _dbContext;
    private readonly Mock<IIdentityContext> _identityMock;
    private readonly Mock<IAgentExecutionRepository> _agentExecRepoMock;
    private readonly Mock<IScheduledJobExecutionRepository> _schedExecRepoMock;
    private readonly Mock<IAgentDefinitionService> _agentDefServiceMock;
    private readonly Mock<IConversationContractHydrator> _contractHydratorMock;
    private readonly Mock<IAgentContractRegistry> _contractRegistryMock;
    private readonly Mock<IGrainFactory> _grainFactoryMock;
    private readonly ScheduledTaskJob _job;

    public ScheduledTaskJobTests()
    {
        var options = new DbContextOptionsBuilder<AgentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _identityMock = new Mock<IIdentityContext>();
        _identityMock.Setup(x => x.UserId).Returns(Guid.Empty);

        _dbContext = new AgentsDbContext(options, _identityMock.Object);

        _agentExecRepoMock = new Mock<IAgentExecutionRepository>();
        _schedExecRepoMock = new Mock<IScheduledJobExecutionRepository>();
        _agentDefServiceMock = new Mock<IAgentDefinitionService>();
        _grainFactoryMock = new Mock<IGrainFactory>();

        _contractHydratorMock = new Mock<IConversationContractHydrator>();
        _contractHydratorMock
            .Setup(h => h.HydrateAsync(It.IsAny<AgentContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentContract c, CancellationToken _) => c);

        _contractRegistryMock = new Mock<IAgentContractRegistry>();
        _contractRegistryMock
            .Setup(r => r.GetContract("conversation"))
            .Returns(new AgentContractDescriptor("conversation", "test", new AgentContract()));

        _job = new ScheduledTaskJob(
            _dbContext,
            _identityMock.Object,
            _agentExecRepoMock.Object,
            _schedExecRepoMock.Object,
            _agentDefServiceMock.Object,
            _contractHydratorMock.Object,
            _contractRegistryMock.Object,
            _grainFactoryMock.Object,
            Mock.Of<ILogger<ScheduledTaskJob>>());
    }

    public void Dispose() => _dbContext?.Dispose();

    #region Execute Tests

    [Fact]
    public async Task Execute_WithMissingScheduleId_LogsErrorAndReturns()
    {
        var context = CreateJobContext(new JobDataMap());

        await _job.Execute(context);

        _grainFactoryMock.Verify(
            f => f.GetGrain<IAgentGrain>(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_WithNonExistentSchedule_LogsErrorAndReturns()
    {
        var dataMap = new JobDataMap
        {
            { ScheduleDataMapKeys.ScheduleId, Guid.NewGuid().ToString() }
        };
        var context = CreateJobContext(dataMap);

        await _job.Execute(context);

        _grainFactoryMock.Verify(
            f => f.GetGrain<IAgentGrain>(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_WithMissingPayload_LogsErrorAndReturns()
    {
        var scheduleId = Guid.NewGuid();
        await SeedSchedule(scheduleId, includePayload: false);

        var dataMap = new JobDataMap
        {
            { ScheduleDataMapKeys.ScheduleId, scheduleId.ToString() }
        };
        var context = CreateJobContext(dataMap);

        await _job.Execute(context);

        _grainFactoryMock.Verify(
            f => f.GetGrain<IAgentGrain>(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_HydratesIdentityFromScheduleCreator()
    {
        var scheduleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedSchedule(scheduleId, userId: userId, email: "test@test.com", name: "Test User");

        var dataMap = new JobDataMap
        {
            { ScheduleDataMapKeys.ScheduleId, scheduleId.ToString() }
        };
        var context = CreateJobContext(dataMap);

        _agentExecRepoMock.Setup(r => r.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var grainMock = new Mock<IAgentGrain>();
        grainMock.Setup(g => g.RunAsync(It.IsAny<AgentContract>(), It.IsAny<string>(), null))
            .ReturnsAsync(AgentResult.Empty);

        _grainFactoryMock.Setup(f => f.GetGrain<IAgentGrain>(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(grainMock.Object);

        _agentDefServiceMock.Setup(s => s.GetNaviConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentDefinitions.Contracts.Models.NaviAgentDefinitionV1>());

        await _job.Execute(context);

        _identityMock.Verify(i => i.SetIdentity(
            userId,
            "test@test.com",
            "Test User",
            It.IsAny<string?>()), Times.Once);
    }

    #endregion

    private async Task SeedSchedule(Guid id, Guid? userId = null, string? email = null, string? name = null, bool includePayload = true)
    {
        var entity = new ScheduledJobEntity
        {
            Id = id,
            UserId = userId ?? Guid.NewGuid(),
            Name = "Test Schedule",
            JobType = ScheduleJobType.AgentInvocation,
            ScheduleMode = ScheduleMode.Recurring,
            CronExpression = "0 0 8 * * ?",
            TimeZoneId = "Europe/Dublin",
            IsEnabled = true,
            TargetType = ScheduleTargetType.Navi,
            QuartzJobKey = $"scheduled-jobs.schedule-{id}",
            QuartzTriggerKey = $"scheduled-triggers.trigger-{id}",
            CreatorEmail = email,
            CreatorName = name,
            CreatorUsername = "testuser",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        if (includePayload)
        {
            entity.Payload = new ScheduledJobPayloadEntity
            {
                Id = Guid.NewGuid(),
                UserId = entity.UserId,
                UserPrompt = "Test prompt",
                Version = 1,
                CreatedAt = DateTimeOffset.UtcNow,
            };
        }

        _dbContext.ScheduledJobs.Add(entity);
        await _dbContext.SaveChangesAsync();
    }

    private static IJobExecutionContext CreateJobContext(JobDataMap dataMap)
    {
        var context = new Mock<IJobExecutionContext>();
        context.Setup(c => c.MergedJobDataMap).Returns(dataMap);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        context.Setup(c => c.Get(It.IsAny<string>())).Returns((string?)null);
        return context.Object;
    }
}
