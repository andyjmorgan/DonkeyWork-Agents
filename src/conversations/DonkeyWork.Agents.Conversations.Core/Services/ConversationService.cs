using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Conversations.Contracts.Models;
using DonkeyWork.Agents.Conversations.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Conversations;
using DonkeyWork.Agents.Storage.Contracts.Models;
using DonkeyWork.Agents.Storage.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Conversations.Core.Services;

public class ConversationService : IConversationService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly IStorageService _storageService;
    private readonly IImageValidationService _imageValidationService;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        IStorageService storageService,
        IImageValidationService imageValidationService,
        ILogger<ConversationService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _storageService = storageService;
        _imageValidationService = imageValidationService;
        _logger = logger;
    }

    public async Task<ConversationDetailsV1> CreateAsync(CreateConversationRequestV1 request, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        _logger.LogInformation("Creating conversation for user {UserId} with orchestration {OrchestrationId}", userId, request.OrchestrationId);

        string? orchestrationName = null;

        if (request.OrchestrationId.HasValue)
        {
            // Verify orchestration exists and belongs to user
            var orchestration = await _dbContext.Orchestrations
                .AsNoTracking()
                .Include(o => o.CurrentVersion)
                .FirstOrDefaultAsync(o => o.Id == request.OrchestrationId.Value, cancellationToken);

            if (orchestration == null)
            {
                throw new InvalidOperationException($"Orchestration {request.OrchestrationId} not found");
            }

            // Verify orchestration has Chat interface
            if (orchestration.CurrentVersion == null)
            {
                throw new InvalidOperationException($"Orchestration {request.OrchestrationId} has no published version");
            }

            if (!orchestration.CurrentVersion.NaviEnabled)
            {
                throw new InvalidOperationException($"Orchestration {request.OrchestrationId} does not support Chat interface");
            }

            orchestrationName = orchestration.Name;
        }

        var conversationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var title = request.Title ?? "New conversation";

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
            OrchestrationName = orchestrationName,
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

    public async Task<PaginatedResponse<ConversationSummaryV1>> ListAsync(PaginationRequest pagination, bool? agentOnly = null, CancellationToken cancellationToken = default)
    {
        var limit = Math.Min(pagination.Limit, 20); // Default page size 20

        var query = _dbContext.Conversations
            .AsNoTracking()
            .Include(c => c.Orchestration)
            .Include(c => c.Messages)
            .AsQueryable();

        if (agentOnly == true)
        {
            query = query.Where(c => c.OrchestrationId == null);
        }
        else if (agentOnly == false)
        {
            query = query.Where(c => c.OrchestrationId != null);
        }

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

        await _storageService.DeleteByPrefixAsync(
            $"conversations/{conversationId}",
            cancellationToken);

        _logger.LogInformation("Deleted images for conversation {ConversationId}", conversationId);

        _dbContext.Conversations.Remove(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted conversation {ConversationId}", conversationId);

        return true;
    }

    public async Task<int> BulkDeleteAsync(BulkDeleteConversationsRequestV1 request, CancellationToken cancellationToken = default)
    {
        var conversations = await _dbContext.Conversations
            .Where(c => request.Ids.Contains(c.Id))
            .ToListAsync(cancellationToken);

        if (conversations.Count == 0)
        {
            return 0;
        }

        foreach (var conversation in conversations)
        {
            await _storageService.DeleteByPrefixAsync(
                $"conversations/{conversation.Id}",
                cancellationToken);
        }

        _dbContext.Conversations.RemoveRange(conversations);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Bulk deleted {Count} conversations", conversations.Count);

        return conversations.Count;
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

    public async Task<UploadImageResponseV1?> UploadImageAsync(
        Guid conversationId,
        string fileName,
        string contentType,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        // Verify conversation exists and belongs to user
        var conversation = await _dbContext.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation == null)
        {
            return null;
        }

        var fileSize = fileStream.Length;

        var validationResult = await _imageValidationService.ValidateAsync(contentType, fileSize, fileStream);

        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException(validationResult.ErrorMessage);
        }

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        // Upload to storage under conversations/{convId}/ prefix
        var uploadRequest = new UploadFileRequest
        {
            FileName = fileName,
            ContentType = validationResult.DetectedMimeType ?? contentType,
            Content = fileStream,
            KeyPrefix = $"conversations/{conversationId}"
        };

        var result = await _storageService.UploadAsync(uploadRequest, cancellationToken);

        var relativeKey = result.ObjectKey;
        var userPrefix = $"{_identityContext.UserId}/";
        if (relativeKey.StartsWith(userPrefix))
        {
            relativeKey = relativeKey[userPrefix.Length..];
        }

        _logger.LogInformation(
            "Uploaded image {ObjectKey} for conversation {ConversationId}. Size: {Size} bytes, Type: {ContentType}",
            result.ObjectKey, conversationId, result.SizeBytes, result.ContentType);

        return new UploadImageResponseV1
        {
            ObjectKey = relativeKey,
            FileName = result.FileName,
            ContentType = result.ContentType,
            SizeBytes = result.SizeBytes
        };
    }

    private static ConversationSummaryV1 MapToSummary(ConversationEntity conversation)
    {
        return new ConversationSummaryV1
        {
            Id = conversation.Id,
            OrchestrationId = conversation.OrchestrationId,
            OrchestrationName = conversation.Orchestration?.Name,
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
            OrchestrationName = conversation.Orchestration?.Name,
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

}
