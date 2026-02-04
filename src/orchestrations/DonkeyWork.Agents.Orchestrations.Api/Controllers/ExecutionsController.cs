using System.Text.Json;
using Asp.Versioning;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Orchestrations.Api.Controllers;

/// <summary>
/// Manage agent executions.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/orchestrations")]
[Authorize]
[Produces("application/json")]
public class ExecutionsController : ControllerBase
{
    private readonly IOrchestrationService _agentService;
    private readonly IOrchestrationVersionService _versionService;
    private readonly IOrchestrationExecutor _orchestrator;
    private readonly IExecutionStreamService _streamService;
    private readonly IOrchestrationExecutionRepository _executionRepository;
    private readonly IIdentityContext _identityContext;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExecutionsController(
        IOrchestrationService agentService,
        IOrchestrationVersionService versionService,
        IOrchestrationExecutor orchestrator,
        IExecutionStreamService streamService,
        IOrchestrationExecutionRepository executionRepository,
        IIdentityContext identityContext)
    {
        _agentService = agentService;
        _versionService = versionService;
        _orchestrator = orchestrator;
        _streamService = streamService;
        _executionRepository = executionRepository;
        _identityContext = identityContext;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Execute an agent in production mode (uses latest published version).
    /// Supports both streaming (Accept: text/event-stream) and non-streaming responses.
    /// </summary>
    /// <param name="orchestrationId">The agent ID.</param>
    /// <param name="request">The execution request with input data.</param>
    /// <response code="200">Returns execution result for non-streaming, or starts SSE stream for streaming.</response>
    /// <response code="400">Invalid request or no published version available.</response>
    /// <response code="404">Agent not found.</response>
    [HttpPost("{orchestrationId:guid}/execute")]
    [ProducesResponseType<ExecuteOrchestrationResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Execute(Guid orchestrationId, [FromBody] ExecuteOrchestrationRequestV1 request)
    {
        // 1. Load agent
        var agent = await _agentService.GetByIdAsync(orchestrationId, _identityContext.UserId);
        if (agent == null)
            return NotFound(new { message = "Agent not found" });

        // 2. Determine version
        GetOrchestrationVersionResponseV1? version;
        if (request.VersionId.HasValue)
        {
            version = await _versionService.GetVersionAsync(orchestrationId, request.VersionId.Value, _identityContext.UserId);
            if (version == null)
                return NotFound(new { message = "Version not found" });
        }
        else
        {
            // Use latest published (CurrentVersionId)
            if (agent.CurrentVersionId == null)
                return BadRequest(new { message = "No published version available" });

            version = await _versionService.GetVersionAsync(orchestrationId, agent.CurrentVersionId.Value, _identityContext.UserId);
            if (version == null)
                return NotFound(new { message = "Published version not found" });
        }

        // 3. Check Accept header for streaming vs non-streaming
        var acceptHeader = Request.Headers["Accept"].ToString();
        if (acceptHeader.Contains("text/event-stream"))
        {
            // Streaming response
            return await StreamExecutionAsync(version.Id, request.Input);
        }
        else
        {
            // Non-streaming response
            return await ExecuteAndWaitAsync(version.Id, request.Input);
        }
    }

    /// <summary>
    /// Execute an agent in test/playground mode (uses draft version if available, otherwise latest published).
    /// Supports both streaming (Accept: text/event-stream) and non-streaming responses.
    /// </summary>
    /// <param name="orchestrationId">The agent ID.</param>
    /// <param name="request">The execution request with input data.</param>
    /// <response code="200">Returns execution result for non-streaming, or starts SSE stream for streaming.</response>
    /// <response code="400">Invalid request or no version available.</response>
    /// <response code="404">Agent not found.</response>
    [HttpPost("{orchestrationId:guid}/test")]
    [ProducesResponseType<ExecuteOrchestrationResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Test(Guid orchestrationId, [FromBody] ExecuteOrchestrationRequestV1 request)
    {
        // 1. Load agent
        var agent = await _agentService.GetByIdAsync(orchestrationId, _identityContext.UserId);
        if (agent == null)
            return NotFound(new { message = "Agent not found" });

        // 2. Determine version - prefer draft, fallback to latest published
        GetOrchestrationVersionResponseV1? version;
        if (request.VersionId.HasValue)
        {
            version = await _versionService.GetVersionAsync(orchestrationId, request.VersionId.Value, _identityContext.UserId);
            if (version == null)
                return NotFound(new { message = "Version not found" });
        }
        else
        {
            var versions = await _versionService.GetVersionsAsync(orchestrationId, _identityContext.UserId);
            var draftVersion = versions.FirstOrDefault(v => v.IsDraft);
            version = draftVersion ?? versions.FirstOrDefault(v => v.Id == agent.CurrentVersionId);

            if (version == null)
                return BadRequest(new { message = "No version available for testing" });
        }

        // 3. Check Accept header for streaming vs non-streaming
        var acceptHeader = Request.Headers["Accept"].ToString();
        if (acceptHeader.Contains("text/event-stream"))
        {
            // Streaming response
            return await StreamExecutionAsync(version.Id, request.Input);
        }
        else
        {
            // Non-streaming response
            return await ExecuteAndWaitAsync(version.Id, request.Input);
        }
    }

    /// <summary>
    /// Execute an agent in chat mode (uses latest published version).
    /// Accepts conversation history and streams the response.
    /// </summary>
    /// <param name="orchestrationId">The agent ID.</param>
    /// <param name="request">The chat request with message history.</param>
    /// <response code="200">Returns execution result for non-streaming, or starts SSE stream for streaming.</response>
    /// <response code="400">Invalid request or no published version available.</response>
    /// <response code="404">Agent not found.</response>
    [HttpPost("{orchestrationId:guid}/chat")]
    [ProducesResponseType<ExecuteOrchestrationResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Chat(Guid orchestrationId, [FromBody] ChatExecuteRequestV1 request)
    {
        // 1. Load agent
        var agent = await _agentService.GetByIdAsync(orchestrationId, _identityContext.UserId);
        if (agent == null)
            return NotFound(new { message = "Agent not found" });

        // 2. Determine version
        GetOrchestrationVersionResponseV1? version;
        if (request.VersionId.HasValue)
        {
            version = await _versionService.GetVersionAsync(orchestrationId, request.VersionId.Value, _identityContext.UserId);
            if (version == null)
                return NotFound(new { message = "Version not found" });
        }
        else
        {
            // Use latest published (CurrentVersionId)
            if (agent.CurrentVersionId == null)
                return BadRequest(new { message = "No published version available" });

            version = await _versionService.GetVersionAsync(orchestrationId, agent.CurrentVersionId.Value, _identityContext.UserId);
            if (version == null)
                return NotFound(new { message = "Published version not found" });
        }

        // 3. Build conversation context
        var conversation = BuildConversationContext(request.Messages);

        // 4. Check Accept header for streaming vs non-streaming
        var acceptHeader = Request.Headers["Accept"].ToString();
        if (acceptHeader.Contains("text/event-stream"))
        {
            return await StreamChatExecutionAsync(version.Id, conversation);
        }
        else
        {
            return await ExecuteChatAndWaitAsync(version.Id, conversation);
        }
    }

    /// <summary>
    /// Execute an agent in chat mode for testing (uses draft version if available).
    /// Accepts conversation history and streams the response.
    /// </summary>
    /// <param name="orchestrationId">The agent ID.</param>
    /// <param name="request">The chat request with message history.</param>
    /// <response code="200">Returns execution result for non-streaming, or starts SSE stream for streaming.</response>
    /// <response code="400">Invalid request or no version available.</response>
    /// <response code="404">Agent not found.</response>
    [HttpPost("{orchestrationId:guid}/test-chat")]
    [ProducesResponseType<ExecuteOrchestrationResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TestChat(Guid orchestrationId, [FromBody] ChatExecuteRequestV1 request)
    {
        // 1. Load agent
        var agent = await _agentService.GetByIdAsync(orchestrationId, _identityContext.UserId);
        if (agent == null)
            return NotFound(new { message = "Agent not found" });

        // 2. Determine version - prefer draft, fallback to latest published
        GetOrchestrationVersionResponseV1? version;
        if (request.VersionId.HasValue)
        {
            version = await _versionService.GetVersionAsync(orchestrationId, request.VersionId.Value, _identityContext.UserId);
            if (version == null)
                return NotFound(new { message = "Version not found" });
        }
        else
        {
            var versions = await _versionService.GetVersionsAsync(orchestrationId, _identityContext.UserId);
            var draftVersion = versions.FirstOrDefault(v => v.IsDraft);
            version = draftVersion ?? versions.FirstOrDefault(v => v.Id == agent.CurrentVersionId);

            if (version == null)
                return BadRequest(new { message = "No version available for testing" });
        }

        // 3. Build conversation context
        var conversation = BuildConversationContext(request.Messages);

        // 4. Check Accept header for streaming vs non-streaming
        var acceptHeader = Request.Headers["Accept"].ToString();
        if (acceptHeader.Contains("text/event-stream"))
        {
            return await StreamChatExecutionAsync(version.Id, conversation);
        }
        else
        {
            return await ExecuteChatAndWaitAsync(version.Id, conversation);
        }
    }

    /// <summary>
    /// Get execution status and result.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <response code="200">Returns the execution details.</response>
    /// <response code="404">Execution not found.</response>
    [HttpGet("executions/{executionId:guid}")]
    [ProducesResponseType<GetExecutionResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExecution(Guid executionId)
    {
        var execution = await _executionRepository.GetByIdAsync(executionId, _identityContext.UserId);

        if (execution == null)
            return NotFound(new { message = "Execution not found" });

        return Ok(execution);
    }

    /// <summary>
    /// Reconnect to execution stream with offset.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="offset">The stream offset to start from (default: 0).</param>
    /// <response code="200">Starts SSE stream from the specified offset.</response>
    /// <response code="404">Execution not found.</response>
    [HttpGet("executions/{executionId:guid}/stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReconnectStream(Guid executionId, [FromQuery] long offset = 0)
    {
        // Verify user owns this execution
        var execution = await _executionRepository.GetByIdAsync(executionId, _identityContext.UserId);
        if (execution == null)
            return NotFound(new { message = "Execution not found" });

        // Stream events from offset
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        await foreach (var evt in _streamService.ReadEventsAsync(executionId, offset))
        {
            var json = JsonSerializer.Serialize(evt, _jsonOptions);
            await Response.WriteAsync($"event: {evt.GetType().Name}\n");
            await Response.WriteAsync($"data: {json}\n\n");
            await Response.Body.FlushAsync();

            if (evt is ExecutionCompletedEvent || evt is ExecutionFailedEvent)
                break;
        }

        return new EmptyResult();
    }

    /// <summary>
    /// List executions for the current user, optionally filtered by agent.
    /// </summary>
    /// <param name="orchestrationId">Optional agent ID to filter by.</param>
    /// <param name="offset">Number of items to skip (default: 0).</param>
    /// <param name="limit">Maximum number of items to return (default: 20, max: 100).</param>
    /// <response code="200">Returns paginated list of executions.</response>
    [HttpGet("executions")]
    [ProducesResponseType<ListExecutionsResponseV1>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListExecutions(
        [FromQuery] Guid? orchestrationId = null,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 20)
    {
        // Enforce max limit
        limit = Math.Min(limit, 100);

        var (executions, totalCount) = await _executionRepository.ListAsync(
            orchestrationId,
            offset,
            limit,
            _identityContext.UserId);

        return Ok(new ListExecutionsResponseV1
        {
            Executions = executions,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Get execution logs for a specific execution.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="offset">Number of items to skip (default: 0).</param>
    /// <param name="limit">Maximum number of items to return (default: 100, max: 1000).</param>
    /// <response code="200">Returns paginated list of execution logs.</response>
    /// <response code="404">Execution not found.</response>
    [HttpGet("executions/{executionId:guid}/logs")]
    [ProducesResponseType<GetExecutionLogsResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExecutionLogs(
        Guid executionId,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100)
    {
        // Enforce max limit
        limit = Math.Min(limit, 1000);

        var (logs, totalCount) = await _executionRepository.GetLogsAsync(
            executionId,
            offset,
            limit,
            _identityContext.UserId);

        if (totalCount == 0 && logs.Count == 0)
        {
            // Verify execution exists
            var execution = await _executionRepository.GetByIdAsync(executionId, _identityContext.UserId);
            if (execution == null)
                return NotFound(new { message = "Execution not found" });
        }

        return Ok(new GetExecutionLogsResponseV1
        {
            Logs = logs,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Get node executions for a specific execution.
    /// Returns the execution trace showing each node that was executed.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="offset">Number of items to skip (default: 0).</param>
    /// <param name="limit">Maximum number of items to return (default: 100, max: 1000).</param>
    /// <response code="200">Returns paginated list of node executions.</response>
    /// <response code="404">Execution not found.</response>
    [HttpGet("executions/{executionId:guid}/nodes")]
    [ProducesResponseType<GetNodeExecutionsResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNodeExecutions(
        Guid executionId,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100)
    {
        // Enforce max limit
        limit = Math.Min(limit, 1000);

        var (nodeExecutions, totalCount) = await _executionRepository.GetNodeExecutionsAsync(
            executionId,
            offset,
            limit,
            _identityContext.UserId);

        if (totalCount == 0 && nodeExecutions.Count == 0)
        {
            // Verify execution exists
            var execution = await _executionRepository.GetByIdAsync(executionId, _identityContext.UserId);
            if (execution == null)
                return NotFound(new { message = "Execution not found" });
        }

        return Ok(new GetNodeExecutionsResponseV1
        {
            NodeExecutions = nodeExecutions,
            TotalCount = totalCount
        });
    }

    private async Task<IActionResult> StreamExecutionAsync(
        Guid versionId,
        JsonElement input)
    {
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        var executionId = Guid.NewGuid();
        await _streamService.CreateStreamAsync(executionId);

        _ = _orchestrator.ExecuteAsync(
            executionId,
            _identityContext.UserId,
            versionId,
            ExecutionInterface.Direct,
            input,
            HttpContext.RequestAborted);

        // Stream events from RabbitMQ in real-time
        await foreach (var evt in _streamService.ReadEventsAsync(executionId))
        {
            var json = JsonSerializer.Serialize(evt, _jsonOptions);
            await Response.WriteAsync($"event: {evt.GetType().Name}\n");
            await Response.WriteAsync($"data: {json}\n\n");
            await Response.Body.FlushAsync();

            if (evt is ExecutionCompletedEvent || evt is ExecutionFailedEvent)
                break;
        }

        return new EmptyResult();
    }

    private async Task<IActionResult> ExecuteAndWaitAsync(
        Guid versionId,
        JsonElement input)
    {
        var executionId = Guid.NewGuid();

        // Create stream and execute orchestration (blocks until completion)
        await _streamService.CreateStreamAsync(executionId);
        await _orchestrator.ExecuteAsync(
            executionId,
            _identityContext.UserId,
            versionId,
            ExecutionInterface.Direct,
            input,
            HttpContext.RequestAborted);

        // Get final execution state from database
        var execution = await _executionRepository.GetByIdAsync(executionId, _identityContext.UserId);

        if (execution == null)
        {
            return StatusCode(500, new { message = "Execution not found" });
        }

        if (execution.Status == ExecutionStatus.Completed)
        {
            var output = execution.Output.HasValue
                ? JsonSerializer.Serialize(execution.Output.Value)
                : null;

            return Ok(new ExecuteOrchestrationResponseV1
            {
                ExecutionId = executionId,
                Status = ExecutionStatus.Completed,
                Output = output
            });
        }

        if (execution.Status == ExecutionStatus.Failed)
        {
            return Ok(new ExecuteOrchestrationResponseV1
            {
                ExecutionId = executionId,
                Status = ExecutionStatus.Failed,
                Error = execution.ErrorMessage
            });
        }

        return StatusCode(500, new { message = $"Unexpected execution status: {execution.Status}" });
    }

    private ConversationContext BuildConversationContext(IReadOnlyList<ChatMessageV1> messages)
    {
        // Find the last user message as the current message
        var lastUserMessage = messages.LastOrDefault(m => m.Role.Equals("user", StringComparison.OrdinalIgnoreCase));
        var currentMessage = lastUserMessage?.Content ?? string.Empty;

        // Convert messages to conversation history (excluding the last user message which is "current")
        var historyMessages = messages
            .Take(messages.Count - 1) // Exclude last message (current user message)
            .Select(m => new ConversationMessage
            {
                Role = m.Role.Equals("user", StringComparison.OrdinalIgnoreCase)
                    ? ConversationRole.User
                    : ConversationRole.Assistant,
                Content = m.Content
            })
            .ToList();

        return new ConversationContext
        {
            Id = Guid.NewGuid(), // Ephemeral conversation ID for playground
            Messages = historyMessages,
            CurrentMessage = currentMessage
        };
    }

    private async Task<IActionResult> StreamChatExecutionAsync(
        Guid versionId,
        ConversationContext conversation)
    {
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        var executionId = Guid.NewGuid();
        await _streamService.CreateStreamAsync(executionId);

        _ = _orchestrator.ExecuteChatAsync(
            executionId,
            _identityContext.UserId,
            versionId,
            conversation,
            HttpContext.RequestAborted);

        // Stream events from RabbitMQ in real-time
        await foreach (var evt in _streamService.ReadEventsAsync(executionId))
        {
            var json = JsonSerializer.Serialize(evt, _jsonOptions);
            await Response.WriteAsync($"event: {evt.GetType().Name}\n");
            await Response.WriteAsync($"data: {json}\n\n");
            await Response.Body.FlushAsync();

            if (evt is ExecutionCompletedEvent || evt is ExecutionFailedEvent)
                break;
        }

        return new EmptyResult();
    }

    private async Task<IActionResult> ExecuteChatAndWaitAsync(
        Guid versionId,
        ConversationContext conversation)
    {
        var executionId = Guid.NewGuid();

        // Create stream and execute orchestration (blocks until completion)
        await _streamService.CreateStreamAsync(executionId);
        await _orchestrator.ExecuteChatAsync(
            executionId,
            _identityContext.UserId,
            versionId,
            conversation,
            HttpContext.RequestAborted);

        // Get final execution state from database
        var execution = await _executionRepository.GetByIdAsync(executionId, _identityContext.UserId);

        if (execution == null)
        {
            return StatusCode(500, new { message = "Execution not found" });
        }

        if (execution.Status == ExecutionStatus.Completed)
        {
            var output = execution.Output.HasValue
                ? JsonSerializer.Serialize(execution.Output.Value)
                : null;

            return Ok(new ExecuteOrchestrationResponseV1
            {
                ExecutionId = executionId,
                Status = ExecutionStatus.Completed,
                Output = output
            });
        }

        if (execution.Status == ExecutionStatus.Failed)
        {
            return Ok(new ExecuteOrchestrationResponseV1
            {
                ExecutionId = executionId,
                Status = ExecutionStatus.Failed,
                Error = execution.ErrorMessage
            });
        }

        return StatusCode(500, new { message = $"Unexpected execution status: {execution.Status}" });
    }
}
