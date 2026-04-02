using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Contracts.Models;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace DonkeyWork.Agents.Actors.Core.Grains;

[Reentrant]
public sealed class AgentRegistryGrain : Grain, IAgentRegistryGrain
{
    private readonly ILogger<AgentRegistryGrain> _logger;
    private readonly Dictionary<string, AgentEntry> _agents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _nameIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _nameCounters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _sharedContext = new(StringComparer.OrdinalIgnoreCase);

    public AgentRegistryGrain(ILogger<AgentRegistryGrain> logger)
    {
        _logger = logger;
    }

    #region Registration

    public Task<string> RegisterAsync(string agentKey, string label, string name, string parentAgentKey, TimeSpan? timeout = null)
    {
        if (_agents.ContainsKey(agentKey))
        {
            _logger.LogWarning("Agent {AgentKey} already registered, ignoring duplicate", agentKey);
            var existing = _agents[agentKey].Info.Name;
            return Task.FromResult(existing);
        }

        var uniqueName = AssignUniqueName(name);

        var entry = new AgentEntry
        {
            Info = new TrackedAgent(agentKey, label, parentAgentKey, AgentStatus.Pending, null, DateTime.UtcNow, uniqueName),
        };

        _agents[agentKey] = entry;
        _nameIndex[uniqueName] = agentKey;

        _logger.LogInformation("Registered agent {AgentKey} (name: {Name}, label: {Label})", agentKey, uniqueName, label);
        return Task.FromResult(uniqueName);
    }

    #endregion

    #region Completion

    public async Task ReportCompletionAsync(string agentKey, AgentResult result, bool isError = false)
    {
        if (!_agents.TryGetValue(agentKey, out var entry))
        {
            _logger.LogWarning("ReportCompletion for unknown agent {AgentKey}", agentKey);
            return;
        }

        var newStatus = isError ? AgentStatus.Failed : AgentStatus.Completed;
        entry.Info = entry.Info with { Status = newStatus, Result = result };

        if (isError)
        {
            var errorText = result.Parts.OfType<AgentTextPart>().FirstOrDefault()?.Text ?? "Agent failed";
            entry.Tcs.TrySetException(new InvalidOperationException(errorText));
        }
        else
        {
            entry.Tcs.TrySetResult(result);
        }

        _logger.LogInformation("Agent {AgentKey} completed (status: {Status})", agentKey, newStatus);
        await DeliverToParentAsync(entry, agentKey, result, isError);
    }

    public Task ReportIdleAsync(string agentKey, AgentResult? result = null)
    {
        if (!_agents.TryGetValue(agentKey, out var entry))
        {
            _logger.LogWarning("ReportIdle for unknown agent {AgentKey}", agentKey);
            return Task.CompletedTask;
        }

        entry.Info = entry.Info with { Status = AgentStatus.Idle, Result = result };
        entry.Tcs.TrySetResult(result ?? AgentResult.Empty);
        _logger.LogInformation("Agent {AgentKey} is now idle", agentKey);
        return Task.CompletedTask;
    }

    public Task ReportResumedAsync(string agentKey)
    {
        if (!_agents.TryGetValue(agentKey, out var entry))
        {
            _logger.LogWarning("ReportResumed for unknown agent {AgentKey}", agentKey);
            return Task.CompletedTask;
        }

        entry.Info = entry.Info with { Status = AgentStatus.Pending, Result = null };
        entry.Tcs = new TaskCompletionSource<AgentResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        entry.Delivered = false;
        _logger.LogInformation("Agent {AgentKey} resumed from idle", agentKey);
        return Task.CompletedTask;
    }

    #endregion

    #region Waiting

    public async Task<AgentWaitResult?> WaitForNextAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);

        var alreadyDone = _agents.Values
            .Where(e => !e.Delivered && e.Info.Status is AgentStatus.Completed or AgentStatus.Failed or AgentStatus.Idle)
            .OrderBy(e => e.Info.SpawnedAt)
            .FirstOrDefault();

        if (alreadyDone is not null)
        {
            alreadyDone.Delivered = true;
            return BuildWaitResult(alreadyDone);
        }

        var pendingTasks = _agents.Values
            .Where(e => e.Info.Status == AgentStatus.Pending)
            .Select(e => e.Tcs.Task)
            .ToList();

        if (pendingTasks.Count == 0)
            return null;

        var timeoutTask = Task.Delay(timeout.Value);
        var completedTask = await Task.WhenAny([.. pendingTasks, timeoutTask]);

        if (completedTask == timeoutTask)
            return null;

        var completed = _agents.Values
            .Where(e => !e.Delivered && e.Info.Status is AgentStatus.Completed or AgentStatus.Failed or AgentStatus.Idle)
            .OrderBy(e => e.Info.SpawnedAt)
            .FirstOrDefault();

        if (completed is null)
            return null;

        completed.Delivered = true;
        return BuildWaitResult(completed);
    }

    public async Task<AgentWaitResult?> WaitForSpecificAsync(string agentKey, TimeSpan? timeout = null)
    {
        if (!_agents.TryGetValue(agentKey, out var entry))
            return null;

        if (entry.Info.Status is AgentStatus.Completed or AgentStatus.Failed or AgentStatus.Idle)
            return BuildWaitResult(entry);

        timeout ??= TimeSpan.FromSeconds(30);

        var timeoutTask = Task.Delay(timeout.Value);
        var completedTask = await Task.WhenAny(entry.Tcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
            return null;

        return BuildWaitResult(entry);
    }

    public Task<IReadOnlyList<TrackedAgent>> ListAsync()
    {
        var agents = _agents.Values
            .OrderBy(e => e.Info.SpawnedAt)
            .Select(e => e.Info)
            .ToList();

        return Task.FromResult<IReadOnlyList<TrackedAgent>>(agents);
    }

    #endregion

    #region Messaging

    public Task<string?> ResolveAgentKeyByNameAsync(string name)
    {
        _nameIndex.TryGetValue(name, out var agentKey);
        return Task.FromResult(agentKey);
    }

    public async Task<bool> SendMessageAsync(string fromAgentKey, string toAgentKey, AgentMessage message)
    {
        if (!_agents.TryGetValue(toAgentKey, out var entry))
        {
            _logger.LogWarning("SendMessage to unknown agent {ToAgentKey}", toAgentKey);
            return false;
        }

        if (entry.Info.Status is not (AgentStatus.Pending or AgentStatus.Idle))
        {
            _logger.LogWarning("SendMessage to agent {ToAgentKey} in status {Status}, skipping", toAgentKey, entry.Info.Status);
            return false;
        }

        var grain = GrainFactory.GetGrain<IAgentGrain>(toAgentKey);
        await grain.DeliverMessageAsync(message);

        _logger.LogInformation("Delivered message from {From} to {To}", fromAgentKey, toAgentKey);
        return true;
    }

    public async Task BroadcastMessageAsync(string fromAgentKey, AgentMessage message)
    {
        var targets = _agents
            .Where(kv => !string.Equals(kv.Key, fromAgentKey, StringComparison.OrdinalIgnoreCase)
                         && kv.Value.Info.Status is AgentStatus.Pending or AgentStatus.Idle)
            .ToList();

        foreach (var (agentKey, _) in targets)
        {
            try
            {
                var grain = GrainFactory.GetGrain<IAgentGrain>(agentKey);
                await grain.DeliverMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast message to {AgentKey}", agentKey);
            }
        }

        _logger.LogInformation("Broadcast message from {From} to {Count} agent(s)", fromAgentKey, targets.Count);
    }

    #endregion

    #region Shared Context

    public Task WriteSharedContextAsync(string key, string value)
    {
        _sharedContext[key] = value;
        return Task.CompletedTask;
    }

    public Task<string?> ReadSharedContextAsync(string key)
    {
        _sharedContext.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyDictionary<string, string>> ReadAllSharedContextAsync()
    {
        return Task.FromResult<IReadOnlyDictionary<string, string>>(
            new Dictionary<string, string>(_sharedContext, StringComparer.OrdinalIgnoreCase));
    }

    public Task<bool> RemoveSharedContextAsync(string key)
    {
        return Task.FromResult(_sharedContext.Remove(key));
    }

    #endregion

    #region Private Methods

    private string AssignUniqueName(string baseName)
    {
        if (!_nameCounters.TryGetValue(baseName, out var count))
        {
            _nameCounters[baseName] = 1;
            return baseName;
        }

        var next = count + 1;
        _nameCounters[baseName] = next;
        return $"{baseName}_{next}";
    }

    private async Task DeliverToParentAsync(AgentEntry entry, string agentKey, AgentResult result, bool isError)
    {
        try
        {
            var parentKey = entry.Info.ParentAgentKey;

            if (!parentKey.StartsWith(AgentKeys.ConversationPrefix))
                return;

            var conversationGrain = GrainFactory.GetGrain<IConversationGrain>(parentKey);
            await conversationGrain.DeliverAgentResultAsync(
                agentKey,
                entry.Info.Label,
                isError ? result : null,
                isError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver completion for {AgentKey} to parent", agentKey);
        }
    }

    private static AgentWaitResult BuildWaitResult(AgentEntry entry)
    {
        AgentResult result;
        if (entry.Tcs.Task.IsCompletedSuccessfully)
            result = entry.Tcs.Task.Result;
        else if (entry.Tcs.Task.IsFaulted)
            result = AgentResult.FromText(entry.Tcs.Task.Exception?.InnerException?.Message ?? "Agent failed");
        else
            result = entry.Info.Result ?? AgentResult.Empty;

        return new AgentWaitResult(entry.Info.AgentKey, entry.Info.Label, result, entry.Info.Status);
    }

    #endregion

    #region Inner Types

    private sealed class AgentEntry
    {
        public TrackedAgent Info { get; set; } = null!;
        public TaskCompletionSource<AgentResult> Tcs { get; set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Delivered { get; set; }
    }

    #endregion
}
