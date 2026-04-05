using System.Text.Json;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Messages;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orchestrations.Core.Handlers;

public static class ExecuteOrchestrationHandler
{
    public static async Task Handle(
        ExecuteOrchestrationCommand command,
        IIdentityContext identityContext,
        IOrchestrationExecutor executor,
        IUserStreamManager streamManager,
        IExecutionStreamWriter streamWriter,
        ILogger<ExecuteOrchestrationCommand> logger)
    {
        try
        {
            identityContext.SetIdentity(
                command.UserId,
                command.UserEmail,
                command.UserName,
                command.UserUsername);

            await streamManager.EnsureStreamAsync(command.UserId);
            await streamWriter.InitializeAsync(command.UserId, command.ExecutionId);

            if (command.ExecutionInterface == ExecutionInterface.Chat)
            {
                var conversation = JsonSerializer.Deserialize<ConversationContext>(command.ConversationJson!);
                await executor.ExecuteChatAsync(
                    command.ExecutionId,
                    command.UserId,
                    command.VersionId,
                    conversation!,
                    CancellationToken.None);
            }
            else
            {
                var input = JsonDocument.Parse(command.InputJson ?? "{}").RootElement;
                await executor.ExecuteAsync(
                    command.ExecutionId,
                    command.UserId,
                    command.VersionId,
                    command.ExecutionInterface,
                    input,
                    CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Execution handler failed for {ExecutionId}", command.ExecutionId);

            try
            {
                await streamWriter.WriteEventAsync(new ExecutionFailedEvent
                {
                    ErrorMessage = ex.Message
                });
            }
            catch (Exception innerEx)
            {
                logger.LogCritical(innerEx,
                    "Failed to emit terminal event for {ExecutionId}", command.ExecutionId);
            }
        }
    }
}
