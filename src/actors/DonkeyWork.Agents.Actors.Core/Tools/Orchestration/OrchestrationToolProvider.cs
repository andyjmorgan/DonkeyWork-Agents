using System.Text.Json;
using System.Text.RegularExpressions;
using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Core.Providers;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Tools.Orchestration;

internal sealed partial class OrchestrationToolProvider
{
    private readonly Dictionary<string, OrchestrationToolInfo> _tools = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initialize from OrchestrationReferences by fetching data from services.
    /// Used by AgentGrain where DB calls are safe.
    /// </summary>
    public async Task InitializeAsync(
        OrchestrationReference[] references,
        Guid userId,
        IOrchestrationService orchestrationService,
        IOrchestrationVersionService versionService,
        IOrchestrationExecutor executor,
        IOrchestrationExecutionRepository executionRepo,
        ILogger logger,
        CancellationToken ct)
    {
        foreach (var reference in references)
        {
            try
            {
                if (!Guid.TryParse(reference.Id, out var orchestrationId))
                {
                    logger.LogWarning("Invalid orchestration ID '{Id}', skipping", reference.Id);
                    continue;
                }

                var orchestration = await orchestrationService.GetByIdAsync(orchestrationId, userId, ct);
                if (orchestration == null)
                {
                    logger.LogWarning("Orchestration '{Id}' not found, skipping", reference.Id);
                    continue;
                }

                var versionId = reference.VersionId != null && Guid.TryParse(reference.VersionId, out var vid)
                    ? vid
                    : orchestration.CurrentVersionId;

                if (versionId == null)
                {
                    logger.LogWarning("Orchestration '{Name}' has no published version, skipping", orchestration.Name);
                    continue;
                }

                var version = await versionService.GetVersionAsync(orchestrationId, versionId.Value, userId, ct);
                if (version == null)
                {
                    logger.LogWarning("Version '{VersionId}' for orchestration '{Name}' not found, skipping",
                        versionId, orchestration.Name);
                    continue;
                }

                RegisterTool(orchestration.Name, orchestration.Description, version, orchestrationId,
                    versionId.Value, userId, reference.ToolName, reference.Description, executor, executionRepo, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize orchestration '{Id}', skipping", reference.Id);
            }
        }
    }

    /// <summary>
    /// Initialize from pre-fetched orchestration and version data.
    /// Used by ConversationGrain to avoid DB calls during ExecuteTurnAsync.
    /// </summary>
    public void InitializeFromPreloadedData(
        IReadOnlyList<(GetOrchestrationResponseV1 Orchestration, GetOrchestrationVersionResponseV1 Version)> orchestrations,
        Guid userId,
        IOrchestrationExecutor executor,
        IOrchestrationExecutionRepository executionRepo,
        ILogger logger)
    {
        foreach (var (orchestration, version) in orchestrations)
        {
            try
            {
                RegisterTool(orchestration.Name, orchestration.Description, version, orchestration.Id,
                    version.Id, userId, null, null, executor, executionRepo, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to register orchestration tool '{Name}', skipping", orchestration.Name);
            }
        }
    }

    private void RegisterTool(
        string orchestrationName,
        string? orchestrationDescription,
        GetOrchestrationVersionResponseV1 version,
        Guid orchestrationId,
        Guid versionId,
        Guid userId,
        string? toolNameOverride,
        string? descriptionOverride,
        IOrchestrationExecutor executor,
        IOrchestrationExecutionRepository executionRepo,
        ILogger logger)
    {
        var toolInterface = version.Interfaces.OfType<ToolInterfaceConfig>().FirstOrDefault();
        var toolName = toolNameOverride ?? SanitizeToolName(orchestrationName);
        var displayName = toolInterface?.Name ?? orchestrationName;
        var description = toolInterface?.Description
            ?? descriptionOverride
            ?? orchestrationDescription
            ?? $"Execute the {orchestrationName} orchestration";

        if (_tools.ContainsKey(toolName))
        {
            logger.LogWarning("Duplicate orchestration tool name '{ToolName}', skipping", toolName);
            return;
        }

        var definition = new InternalToolDefinition
        {
            Name = toolName,
            DisplayName = displayName,
            Description = description,
            InputSchema = version.InputSchema,
            DeferLoading = false
        };

        _tools[toolName] = new OrchestrationToolInfo(
            definition, orchestrationId, versionId, userId, executor, executionRepo);

        logger.LogInformation(
            "Registered orchestration tool '{ToolName}' → {OrchestrationName} (version {VersionId})",
            toolName, orchestrationName, versionId);
    }

    public IReadOnlyList<InternalToolDefinition> GetToolDefinitions()
        => _tools.Values.Select(t => t.Definition).ToList();

    public bool HasTool(string toolName) => _tools.ContainsKey(toolName);

    public string? GetDisplayName(string toolName)
        => _tools.TryGetValue(toolName, out var info) ? info.Definition.DisplayName : null;

    public async Task<ToolResult> ExecuteAsync(string toolName, JsonElement arguments, CancellationToken ct)
    {
        if (!_tools.TryGetValue(toolName, out var toolInfo))
            return ToolResult.Error($"Orchestration tool '{toolName}' not found.");

        try
        {
            var executionId = Guid.NewGuid();

            await toolInfo.Executor.ExecuteAsync(
                executionId,
                toolInfo.UserId,
                toolInfo.VersionId,
                ExecutionInterface.Direct,
                arguments,
                ct);

            var execution = await toolInfo.ExecutionRepo.GetByIdAsync(executionId, toolInfo.UserId, ct);

            if (execution == null)
                return ToolResult.Error("Orchestration execution completed but result not found.");

            if (execution.Status == ExecutionStatus.Failed)
                return ToolResult.Error(execution.ErrorMessage ?? "Orchestration execution failed.");

            return execution.Output.HasValue
                ? ToolResult.Success(execution.Output.Value.GetRawText())
                : ToolResult.Success("Orchestration completed with no output.");
        }
        catch (Exception ex)
        {
            return ToolResult.Error($"Orchestration execution failed: {ex.Message}");
        }
    }

    private static string SanitizeToolName(string name)
        => ToolNameRegex().Replace(name.ToLowerInvariant().Replace(' ', '_'), "");

    [GeneratedRegex("[^a-z0-9_]")]
    private static partial Regex ToolNameRegex();
}

internal sealed record OrchestrationToolInfo(
    InternalToolDefinition Definition,
    Guid OrchestrationId,
    Guid VersionId,
    Guid UserId,
    IOrchestrationExecutor Executor,
    IOrchestrationExecutionRepository ExecutionRepo);
