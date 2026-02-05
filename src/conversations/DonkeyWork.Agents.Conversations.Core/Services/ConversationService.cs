using System.Runtime.CompilerServices;
using System.Text;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Conversations.Contracts.Models;
using DonkeyWork.Agents.Conversations.Contracts.Models.Events;
using DonkeyWork.Agents.Conversations.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Conversations;
using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;
using DonkeyWork.Agents.Storage.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Conversations.Core.Services;

public class ConversationService : IConversationService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly IOrchestrationExecutor _orchestrationExecutor;
    private readonly IExecutionStreamService _streamService;
    private readonly IStorageService _storageService;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        IOrchestrationExecutor orchestrationExecutor,
        IExecutionStreamService streamService,
        IStorageService storageService,
        ILogger<ConversationService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _orchestrationExecutor = orchestrationExecutor;
        _streamService = streamService;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<ConversationDetailsV1> CreateAsync(CreateConversationRequestV1 request, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        _logger.LogInformation("Creating conversation for user {UserId} with orchestration {OrchestrationId}", userId, request.OrchestrationId);

        // Verify orchestration exists and belongs to user
        var orchestration = await _dbContext.Orchestrations
            .AsNoTracking()
            .Include(o => o.CurrentVersion)
            .FirstOrDefaultAsync(o => o.Id == request.OrchestrationId, cancellationToken);

        if (orchestration == null)
        {
            throw new InvalidOperationException($"Orchestration {request.OrchestrationId} not found");
        }

        // Verify orchestration has Chat interface
        if (orchestration.CurrentVersion == null)
        {
            throw new InvalidOperationException($"Orchestration {request.OrchestrationId} has no published version");
        }

        if (orchestration.CurrentVersion.Interface is not ChatInterfaceConfig)
        {
            throw new InvalidOperationException($"Orchestration {request.OrchestrationId} does not support Chat interface");
        }

        var conversationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Generate default title if not provided
        var conversationNumber = await _dbContext.Conversations
            .CountAsync(cancellationToken) + 1;
        var title = request.Title ?? $"Conversation_{conversationNumber}";

        var conversation = new ConversationEntity
        {
            Id = conversationId,
            UserId = userId,
            OrchestrationId = request.OrchestrationId,
            Title = title,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Conversations.Add(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created conversation {ConversationId}", conversationId);

        return new ConversationDetailsV1
        {
            Id = conversationId,
            OrchestrationId = request.OrchestrationId,
            OrchestrationName = orchestration.Name,
            Title = title,
            Messages = [],
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public async Task<ConversationDetailsV1?> GetByIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.Conversations
            .AsNoTracking()
            .Include(c => c.Orchestration)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation == null)
        {
            return null;
        }

        return MapToDetails(conversation);
    }

    public async Task<PaginatedResponse<ConversationSummaryV1>> ListAsync(PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var limit = Math.Min(pagination.Limit, 20); // Default page size 20

        var query = _dbContext.Conversations
            .AsNoTracking()
            .Include(c => c.Orchestration)
            .Include(c => c.Messages);

        var totalCount = await query.CountAsync(cancellationToken);

        var conversations = await query
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Skip(pagination.Offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<ConversationSummaryV1>
        {
            Items = conversations.Select(MapToSummary).ToList(),
            Offset = pagination.Offset,
            Limit = limit,
            TotalCount = totalCount
        };
    }

    public async Task<ConversationDetailsV1?> UpdateTitleAsync(Guid conversationId, UpdateConversationRequestV1 request, CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation == null)
        {
            return null;
        }

        conversation.Title = request.Title;
        conversation.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated conversation {ConversationId} title to {Title}", conversationId, request.Title);

        return await GetByIdAsync(conversationId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation == null)
        {
            return false;
        }

        _dbContext.Conversations.Remove(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted conversation {ConversationId}", conversationId);

        return true;
    }

    public async Task<ConversationMessageV1?> SendMessageAsync(Guid conversationId, SendMessageRequestV1 request, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;

        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation == null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var messageId = Guid.NewGuid();

        var message = new ConversationMessageEntity
        {
            Id = messageId,
            UserId = userId,
            ConversationId = conversationId,
            Role = Persistence.Entities.Conversations.MessageRole.User,
            Content = request.Content.ToList(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.ConversationMessages.Add(message);

        // Update conversation timestamp
        conversation.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added message {MessageId} to conversation {ConversationId}", messageId, conversationId);

        return new ConversationMessageV1
        {
            Id = messageId,
            Role = Contracts.Models.MessageRole.User,
            Content = request.Content.ToList(),
            CreatedAt = now
        };
    }

    public async IAsyncEnumerable<ConversationStreamEvent>? SendMessageStreamingAsync(
        Guid conversationId,
        SendMessageRequestV1 request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;

        // Load conversation with orchestration
        var conversation = await _dbContext.Conversations
            .Include(c => c.Orchestration)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation == null)
        {
            yield break;
        }

        var now = DateTimeOffset.UtcNow;

        // 1. Save user message
        var userMessageId = Guid.NewGuid();

        var userMessage = new ConversationMessageEntity
        {
            Id = userMessageId,
            UserId = userId,
            ConversationId = conversationId,
            Role = Persistence.Entities.Conversations.MessageRole.User,
            Content = request.Content.ToList(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.ConversationMessages.Add(userMessage);
        conversation.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added user message {MessageId} to conversation {ConversationId}", userMessageId, conversationId);

        // 2. Get latest published version for the orchestration
        var version = await _dbContext.OrchestrationVersions
            .Where(v => v.OrchestrationId == conversation.OrchestrationId && !v.IsDraft)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (version == null)
        {
            yield return new ResponseErrorEvent { Error = "No published version available for this orchestration" };
            yield break;
        }

        // 3. Prepare execution ID and create stream
        var executionId = Guid.NewGuid();
        var assistantMessageId = Guid.NewGuid();

        await _streamService.CreateStreamAsync(executionId);

        // 4. Emit response_start
        yield return new ResponseStartEvent { MessageId = assistantMessageId };

        // 5. Emit part_start for the text response
        yield return new PartStartEvent { PartType = "text", PartIndex = 0 };

        // 6. Build conversation context from history + new user message (with hydrated images)
        var historyMessages = new List<Orchestrations.Contracts.Models.ConversationMessage>();

        // Add existing conversation messages (excluding the one we just added)
        foreach (var msg in conversation.Messages.Where(m => m.Id != userMessageId))
        {
            var hydratedContent = await HydrateContentPartsAsync(msg.Content, cancellationToken);

            historyMessages.Add(new Orchestrations.Contracts.Models.ConversationMessage
            {
                Role = msg.Role == Persistence.Entities.Conversations.MessageRole.User
                    ? Orchestrations.Contracts.Models.ConversationRole.User
                    : Orchestrations.Contracts.Models.ConversationRole.Assistant,
                Content = hydratedContent
            });
        }

        // Hydrate current message content
        var hydratedCurrentMessage = await HydrateContentPartsAsync(request.Content, cancellationToken);

        var conversationContext = new Orchestrations.Contracts.Models.ConversationContext
        {
            Id = conversationId,
            Messages = historyMessages,
            CurrentMessage = hydratedCurrentMessage
        };

        // 7. Fire-and-forget orchestration execution
        _ = _orchestrationExecutor.ExecuteChatAsync(
            executionId,
            userId,
            version.Id,
            conversationContext,
            cancellationToken);

        // 8. Stream events from orchestration execution
        var responseBuilder = new StringBuilder();
        string? errorMessage = null;

        await foreach (var evt in _streamService.ReadEventsAsync(executionId))
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            switch (evt)
            {
                case TokenDeltaEvent tokenDelta:
                    responseBuilder.Append(tokenDelta.Delta);
                    yield return new PartDeltaEvent
                    {
                        PartIndex = 0,
                        Content = tokenDelta.Delta
                    };
                    break;

                case ExecutionCompletedEvent:
                    // Execution completed successfully
                    break;

                case ExecutionFailedEvent failedEvent:
                    errorMessage = failedEvent.ErrorMessage;
                    break;
            }
        }

        // 9. Emit part_end
        yield return new PartEndEvent { PartIndex = 0 };

        // 10. Handle error case
        if (!string.IsNullOrEmpty(errorMessage))
        {
            yield return new ResponseErrorEvent { Error = errorMessage };
            yield break;
        }

        // 11. Save assistant message to database
        var responseText = responseBuilder.ToString();
        var assistantContent = new List<ContentPart> { new TextContentPart { Text = responseText } };

        var assistantMessage = new ConversationMessageEntity
        {
            Id = assistantMessageId,
            UserId = userId,
            ConversationId = conversationId,
            Role = Persistence.Entities.Conversations.MessageRole.Assistant,
            Content = assistantContent,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.ConversationMessages.Add(assistantMessage);
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        _logger.LogInformation("Saved assistant message {MessageId} to conversation {ConversationId}", assistantMessageId, conversationId);

        // 12. Emit response_end with the final message
        yield return new ResponseEndEvent
        {
            Message = new ConversationMessageV1
            {
                Id = assistantMessageId,
                Role = Contracts.Models.MessageRole.Assistant,
                Content = assistantContent,
                CreatedAt = assistantMessage.CreatedAt
            }
        };
    }

    public async Task<bool> DeleteMessageAsync(Guid conversationId, Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.ConversationMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId, cancellationToken);

        if (message == null)
        {
            return false;
        }

        _dbContext.ConversationMessages.Remove(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted message {MessageId} from conversation {ConversationId}", messageId, conversationId);

        return true;
    }

    private static ConversationSummaryV1 MapToSummary(ConversationEntity conversation)
    {
        return new ConversationSummaryV1
        {
            Id = conversation.Id,
            OrchestrationId = conversation.OrchestrationId,
            OrchestrationName = conversation.Orchestration?.Name ?? "Unknown",
            Title = conversation.Title,
            MessageCount = conversation.Messages.Count,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt
        };
    }

    private static ConversationDetailsV1 MapToDetails(ConversationEntity conversation)
    {
        return new ConversationDetailsV1
        {
            Id = conversation.Id,
            OrchestrationId = conversation.OrchestrationId,
            OrchestrationName = conversation.Orchestration?.Name ?? "Unknown",
            Title = conversation.Title,
            Messages = conversation.Messages.Select(MapMessage).ToList(),
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt
        };
    }

    private static ConversationMessageV1 MapMessage(ConversationMessageEntity message)
    {
        return new ConversationMessageV1
        {
            Id = message.Id,
            Role = (Contracts.Models.MessageRole)(int)message.Role,
            Content = message.Content,
            CreatedAt = message.CreatedAt
        };
    }

    /// <summary>
    /// Hydrates content parts by downloading images from storage and converting to base64.
    /// </summary>
    private async Task<IReadOnlyList<ChatContentPart>> HydrateContentPartsAsync(
        IEnumerable<ContentPart> contentParts,
        CancellationToken cancellationToken)
    {
        var result = new List<ChatContentPart>();

        foreach (var part in contentParts)
        {
            switch (part)
            {
                case TextContentPart textPart:
                    result.Add(new TextChatContentPart { Text = textPart.Text });
                    break;

                case ImageContentPart imagePart:
                    var downloadResult = await _storageService.DownloadAsync(imagePart.FileId, cancellationToken);
                    if (downloadResult != null)
                    {
                        using var memoryStream = new MemoryStream();
                        await downloadResult.Content.CopyToAsync(memoryStream, cancellationToken);
                        var base64Data = Convert.ToBase64String(memoryStream.ToArray());

                        result.Add(new ImageChatContentPart
                        {
                            SourceType = "base64",
                            MediaType = imagePart.MediaType,
                            Data = base64Data
                        });
                    }
                    else
                    {
                        _logger.LogWarning("Failed to download image {FileId} for hydration", imagePart.FileId);
                    }
                    break;
            }
        }

        return result;
    }
}
