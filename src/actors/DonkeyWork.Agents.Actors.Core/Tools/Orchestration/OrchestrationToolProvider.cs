using System.Text.Json;
using System.Text.RegularExpressions;
using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Core.Providers;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Tools.Orchestration;

internal sealed partial class OrchestrationToolProvider
{
    private readonly Dictionary<string, OrchestrationToolInfo> _tools = new(StringComparer.OrdinalIgnoreCase);
    private IServiceProvider _serviceProvider = null!;
    private ILogger _logger = null!;

    public async Task InitializeAsync(
        OrchestrationReference[] references,
        Guid userId,
        IOrchestrationService orchestrationService,
        IOrchestrationVersionService versionService,
        ILogger logger,
        CancellationToken ct)
    {
        _logger = logger;

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

                RegisterTool(orchestration.Name, orchestration.FriendlyName, orchestration.Description, version, orchestrationId,
                    versionId.Value, userId, reference.ToolName, reference.Description);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize orchestration '{Id}', skipping", reference.Id);
            }
        }
    }

    public void InitializeFromPreloadedData(
        IReadOnlyList<(GetOrchestrationResponseV1 Orchestration, GetOrchestrationVersionResponseV1 Version)> orchestrations,
        Guid userId,
        ILogger logger)
    {
        _logger = logger;

        foreach (var (orchestration, version) in orchestrations)
        {
            try
            {
                RegisterTool(orchestration.Name, orchestration.FriendlyName, orchestration.Description, version, orchestration.Id,
                    version.Id, userId, null, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to register orchestration tool '{Name}', skipping", orchestration.Name);
            }
        }
    }

    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private void RegisterTool(
        string orchestrationName,
        string? friendlyName,
        string? orchestrationDescription,
        GetOrchestrationVersionResponseV1 version,
        Guid orchestrationId,
        Guid versionId,
        Guid userId,
        string? toolNameOverride,
        string? descriptionOverride)
    {
        var toolName = toolNameOverride ?? SanitizeToolName(orchestrationName);
        var displayName = friendlyName ?? orchestrationName;
        var description = descriptionOverride
            ?? orchestrationDescription
            ?? $"Execute the {orchestrationName} orchestration";

        if (_tools.ContainsKey(toolName))
        {
            _logger.LogWarning("Duplicate orchestration tool name '{ToolName}', skipping", toolName);
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

        _tools[toolName] = new OrchestrationToolInfo(definition, orchestrationId, versionId, userId);

        _logger.LogInformation(
            "Registered orchestration tool '{ToolName}' → {DisplayName} (version {VersionId})",
            toolName, displayName, versionId);
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

        var executionId = Guid.NewGuid();

        try
        {
            _logger.LogInformation("Executing orchestration tool '{ToolName}' (execution {ExecutionId})", toolName, executionId);

            await using var scope = _serviceProvider.CreateAsyncScope();
            var scopedIdentity = scope.ServiceProvider.GetService<IIdentityContext>();
            scopedIdentity?.SetIdentity(toolInfo.UserId);

            var executor = scope.ServiceProvider.GetRequiredService<IOrchestrationExecutor>();
            await executor.ExecuteAsync(
                executionId,
                toolInfo.UserId,
                toolInfo.VersionId,
                ExecutionInterface.Direct,
                arguments,
                ct);

            _logger.LogInformation("Orchestration execution {ExecutionId} completed, reading result", executionId);

            var executionRepo = scope.ServiceProvider.GetRequiredService<IOrchestrationExecutionRepository>();
            var execution = await executionRepo.GetByIdAsync(executionId, toolInfo.UserId, ct);

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
            _logger.LogError(ex, "Orchestration tool '{ToolName}' execution {ExecutionId} failed", toolName, executionId);
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
    Guid UserId);
