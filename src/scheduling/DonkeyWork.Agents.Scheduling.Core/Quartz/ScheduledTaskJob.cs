using System.Text.Json;
using DonkeyWork.Agents.Actors.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Contracts.Services;
using DonkeyWork.Agents.Actors.Core.Tools;
using DonkeyWork.Agents.AgentDefinitions.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using DonkeyWork.Agents.Scheduling.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DonkeyWork.Agents.Scheduling.Core.Quartz;

[DisallowConcurrentExecution]
public class ScheduledTaskJob : IJob
{
    private static readonly JsonSerializerOptions ContractJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowOutOfOrderMetadataProperties = true,
    };

    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly IAgentExecutionRepository _agentExecutionRepository;
    private readonly IScheduledJobExecutionRepository _scheduledJobExecutionRepository;
    private readonly IAgentDefinitionService _agentDefinitionService;
    private readonly AgentContractRegistry _contractRegistry;
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<ScheduledTaskJob> _logger;

    public ScheduledTaskJob(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        IAgentExecutionRepository agentExecutionRepository,
        IScheduledJobExecutionRepository scheduledJobExecutionRepository,
        IAgentDefinitionService agentDefinitionService,
        AgentContractRegistry contractRegistry,
        IGrainFactory grainFactory,
        ILogger<ScheduledTaskJob> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _agentExecutionRepository = agentExecutionRepository;
        _scheduledJobExecutionRepository = scheduledJobExecutionRepository;
        _agentDefinitionService = agentDefinitionService;
        _contractRegistry = contractRegistry;
        _grainFactory = grainFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var dataMap = context.MergedJobDataMap;
        var scheduleIdStr = dataMap.ContainsKey(ScheduleDataMapKeys.ScheduleId)
            ? dataMap.GetString(ScheduleDataMapKeys.ScheduleId)
            : null;
        if (!Guid.TryParse(scheduleIdStr, out var scheduleId))
        {
            _logger.LogError("ScheduledTaskJob fired without valid ScheduleId in JobDataMap");
            return;
        }

        var schedule = await _dbContext.ScheduledJobs
            .Include(j => j.Payload)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(j => j.Id == scheduleId, context.CancellationToken);

        if (schedule is null)
        {
            _logger.LogError("ScheduledTaskJob fired for non-existent schedule {ScheduleId}", scheduleId);
            return;
        }

        if (schedule.Payload is null)
        {
            _logger.LogError("Schedule {ScheduleId} has no payload", scheduleId);
            return;
        }

        _identityContext.SetIdentity(
            schedule.UserId,
            schedule.CreatorEmail,
            schedule.CreatorName,
            schedule.CreatorUsername);

        _logger.LogInformation("Executing scheduled job {ScheduleId} ({Name}) targeting {TargetType}",
            scheduleId, schedule.Name, schedule.TargetType);

        AgentContract contract;
        try
        {
            contract = await ResolveContractAsync(schedule, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve contract for schedule {ScheduleId}", scheduleId);
            throw new JobExecutionException(ex, refireImmediately: false);
        }

        var syntheticConversationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var agentKey = AgentKeys.Create(
            contract.KeyPrefix,
            schedule.UserId,
            syntheticConversationId,
            taskId);

        var contractJson = JsonSerializer.Serialize(contract, ContractJsonOptions);
        var agentExecutionId = await _agentExecutionRepository.CreateAsync(
            schedule.UserId,
            syntheticConversationId,
            contract.AgentType,
            $"Scheduled: {schedule.Name}",
            agentKey,
            null,
            contractJson,
            schedule.Payload.UserPrompt,
            contract.ModelId,
            context.CancellationToken);

        var scheduleExecutionIdStr = context.Get(ScheduleDataMapKeys.ScheduleExecutionId) as string;
        if (Guid.TryParse(scheduleExecutionIdStr, out var scheduleExecutionId))
        {
            await _scheduledJobExecutionRepository.UpdateCompletionAsync(
                scheduleExecutionId,
                ScheduleExecutionStatus.Running,
                null,
                null,
                agentExecutionId,
                context.CancellationToken);
        }

        RequestContext.Set(GrainCallContextKeys.UserId, schedule.UserId.ToString());
        RequestContext.Set(GrainCallContextKeys.ConversationId, syntheticConversationId.ToString());
        RequestContext.Set(GrainCallContextKeys.ExecutionId, agentExecutionId.ToString());

        var grain = _grainFactory.GetGrain<IAgentGrain>(agentKey);

        try
        {
            var result = await grain.RunAsync(contract, schedule.Payload.UserPrompt, null);

            _logger.LogInformation("Scheduled job {ScheduleId} ({Name}) completed with {PartCount} result parts",
                scheduleId, schedule.Name, result.Parts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled job {ScheduleId} ({Name}) failed during agent execution",
                scheduleId, schedule.Name);
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }

    private async Task<AgentContract> ResolveContractAsync(
        Persistence.Entities.Scheduling.ScheduledJobEntity schedule,
        CancellationToken ct)
    {
        return schedule.TargetType switch
        {
            ScheduleTargetType.Navi => await ResolveNaviContractAsync(ct),
            ScheduleTargetType.CustomAgent => await ResolveCustomAgentContractAsync(schedule, ct),
            ScheduleTargetType.Orchestration => throw new NotSupportedException("Orchestration targeting is not yet implemented for scheduled jobs."),
            _ => throw new ArgumentException($"Unknown target type: {schedule.TargetType}")
        };
    }

    private async Task<AgentContract> ResolveNaviContractAsync(CancellationToken ct)
    {
        var descriptor = _contractRegistry.GetContract("conversation")
            ?? throw new InvalidOperationException("Conversation contract not found in registry.");
        var naviContract = descriptor.Contract;

        var naviAgents = await _agentDefinitionService.GetNaviConnectedAsync(ct);
        if (naviAgents.Count > 0)
        {
            var catalog = "\n\n## Custom Agents\n\nAvailable via `spawn_agent` (use agent name):\n";
            foreach (var agent in naviAgents)
            {
                var desc = !string.IsNullOrEmpty(agent.Description) ? agent.Description : "No description";
                catalog += $"- **{agent.Name}** (agent_id: `{agent.Id}`): {desc}\n";
            }

            var extendedPrompt = naviContract.SystemPrompt.Append(catalog).ToArray();
            naviContract = new AgentContract
            {
                SystemPrompt = extendedPrompt,
                ToolGroups = naviContract.ToolGroups,
                MaxTokens = naviContract.MaxTokens,
                ThinkingBudgetTokens = naviContract.ThinkingBudgetTokens,
                Stream = naviContract.Stream,
                WebSearch = naviContract.WebSearch,
                WebFetch = naviContract.WebFetch,
                PersistMessages = naviContract.PersistMessages,
                Lifecycle = naviContract.Lifecycle,
                LingerSeconds = naviContract.LingerSeconds,
                AgentType = naviContract.AgentType,
                KeyPrefix = naviContract.KeyPrefix,
                TimeoutSeconds = naviContract.TimeoutSeconds,
                McpServers = naviContract.McpServers,
                EnableSandbox = naviContract.EnableSandbox,
                ModelId = naviContract.ModelId,
                Prompts = naviContract.Prompts,
                SubAgents = naviContract.SubAgents,
                ReasoningEffort = naviContract.ReasoningEffort,
                ToolConfiguration = naviContract.ToolConfiguration,
                DisplayName = naviContract.DisplayName,
                Icon = naviContract.Icon,
                AllowDelegation = naviContract.AllowDelegation,
                ContextManagement = naviContract.ContextManagement,
                A2aServers = naviContract.A2aServers,
                Orchestrations = naviContract.Orchestrations,
            };
        }

        return naviContract;
    }

    private async Task<AgentContract> ResolveCustomAgentContractAsync(
        Persistence.Entities.Scheduling.ScheduledJobEntity schedule,
        CancellationToken ct)
    {
        if (!schedule.TargetAgentDefinitionId.HasValue)
            throw new InvalidOperationException($"Schedule {schedule.Id} targets CustomAgent but has no TargetAgentDefinitionId.");

        var definition = await _agentDefinitionService.GetByIdAsync(schedule.TargetAgentDefinitionId.Value, ct);
        if (definition is null)
            throw new InvalidOperationException(
                $"Agent definition {schedule.TargetAgentDefinitionId} referenced by schedule {schedule.Id} no longer exists.");

        var contract = JsonSerializer.Deserialize<AgentContract>(
            definition.Contract.GetRawText(), ContractJsonOptions)
            ?? throw new JsonException($"Failed to deserialize agent contract for definition {definition.Id}.");

        return contract;
    }
}
