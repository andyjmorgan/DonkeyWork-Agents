using System.Text.Json;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Messages;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Handlers;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orchestrations.Core.Tests.Handlers;

public class ExecuteOrchestrationHandlerTests
{
    private readonly Mock<IIdentityContext> _identityContextMock;
    private readonly Mock<IOrchestrationExecutor> _executorMock;
    private readonly Mock<IUserStreamManager> _streamManagerMock;
    private readonly Mock<IExecutionStreamWriter> _streamWriterMock;
    private readonly Mock<ILogger<ExecuteOrchestrationCommand>> _loggerMock;

    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _testExecutionId = Guid.NewGuid();
    private readonly Guid _testVersionId = Guid.NewGuid();

    public ExecuteOrchestrationHandlerTests()
    {
        _identityContextMock = new Mock<IIdentityContext>();
        _executorMock = new Mock<IOrchestrationExecutor>();
        _streamManagerMock = new Mock<IUserStreamManager>();
        _streamWriterMock = new Mock<IExecutionStreamWriter>();
        _loggerMock = new Mock<ILogger<ExecuteOrchestrationCommand>>();
    }

    private ExecuteOrchestrationCommand CreateCommand(
        ExecutionInterface executionInterface = ExecutionInterface.Direct,
        string? inputJson = "{}",
        string? conversationJson = null)
    {
        return new ExecuteOrchestrationCommand
        {
            ExecutionId = _testExecutionId,
            UserId = _testUserId,
            UserEmail = "test@example.com",
            UserName = "Test User",
            UserUsername = "testuser",
            VersionId = _testVersionId,
            ExecutionInterface = executionInterface,
            InputJson = inputJson,
            ConversationJson = conversationJson
        };
    }

    #region Identity Hydration Tests

    [Fact]
    public async Task Handle_DirectExecution_HydratesIdentityFromCommand()
    {
        var command = CreateCommand();

        await ExecuteHandler(command);

        _identityContextMock.Verify(
            x => x.SetIdentity(_testUserId, "test@example.com", "Test User", "testuser"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NullIdentityFields_HydratesWithNulls()
    {
        var command = new ExecuteOrchestrationCommand
        {
            ExecutionId = _testExecutionId,
            UserId = _testUserId,
            UserEmail = null,
            UserName = null,
            UserUsername = null,
            VersionId = _testVersionId,
            ExecutionInterface = ExecutionInterface.Direct,
            InputJson = "{}"
        };

        await ExecuteHandler(command);

        _identityContextMock.Verify(
            x => x.SetIdentity(_testUserId, null, null, null),
            Times.Once);
    }

    #endregion

    #region Stream Setup Tests

    [Fact]
    public async Task Handle_DirectExecution_EnsuresUserStream()
    {
        var command = CreateCommand();

        await ExecuteHandler(command);

        _streamManagerMock.Verify(
            x => x.EnsureStreamAsync(_testUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DirectExecution_InitializesStreamWriter()
    {
        var command = CreateCommand();

        await ExecuteHandler(command);

        _streamWriterMock.Verify(
            x => x.InitializeAsync(_testUserId, _testExecutionId),
            Times.Once);
    }

    #endregion

    #region Direct Execution Tests

    [Fact]
    public async Task Handle_DirectExecution_CallsExecuteAsync()
    {
        var command = CreateCommand();

        await ExecuteHandler(command);

        _executorMock.Verify(
            x => x.ExecuteAsync(
                _testExecutionId,
                _testUserId,
                _testVersionId,
                ExecutionInterface.Direct,
                It.IsAny<JsonElement>(),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_McpExecution_PassesCorrectInterface()
    {
        var command = CreateCommand(executionInterface: ExecutionInterface.MCP);

        await ExecuteHandler(command);

        _executorMock.Verify(
            x => x.ExecuteAsync(
                _testExecutionId,
                _testUserId,
                _testVersionId,
                ExecutionInterface.MCP,
                It.IsAny<JsonElement>(),
                CancellationToken.None),
            Times.Once);
    }

    #endregion

    #region Chat Execution Tests

    [Fact]
    public async Task Handle_ChatExecution_CallsExecuteChatAsync()
    {
        var conversation = new ConversationContext
        {
            Id = Guid.NewGuid(),
            Messages = [],
            CurrentMessage = []
        };
        var conversationJson = JsonSerializer.Serialize(conversation);
        var command = CreateCommand(
            executionInterface: ExecutionInterface.Chat,
            inputJson: null,
            conversationJson: conversationJson);

        await ExecuteHandler(command);

        _executorMock.Verify(
            x => x.ExecuteChatAsync(
                _testExecutionId,
                _testUserId,
                _testVersionId,
                It.IsAny<ConversationContext>(),
                CancellationToken.None),
            Times.Once);

        _executorMock.Verify(
            x => x.ExecuteAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<ExecutionInterface>(),
                It.IsAny<JsonElement>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Handle_ExecutorThrows_EmitsExecutionFailedEvent()
    {
        var command = CreateCommand();
        _executorMock
            .Setup(x => x.ExecuteAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<ExecutionInterface>(), It.IsAny<JsonElement>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Graph analysis failed"));

        await ExecuteHandler(command);

        _streamWriterMock.Verify(
            x => x.WriteEventAsync(It.Is<ExecutionFailedEvent>(
                e => e.ErrorMessage == "Graph analysis failed")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExecutorThrows_DoesNotRethrow()
    {
        var command = CreateCommand();
        _executorMock
            .Setup(x => x.ExecuteAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<ExecutionInterface>(), It.IsAny<JsonElement>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected failure"));

        var exception = await Record.ExceptionAsync(() => ExecuteHandler(command));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Handle_StreamWriterFailsDuringErrorHandling_DoesNotRethrow()
    {
        var command = CreateCommand();
        _executorMock
            .Setup(x => x.ExecuteAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<ExecutionInterface>(), It.IsAny<JsonElement>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Executor failed"));

        _streamWriterMock
            .Setup(x => x.WriteEventAsync(It.IsAny<ExecutionFailedEvent>()))
            .ThrowsAsync(new Exception("NATS unavailable"));

        var exception = await Record.ExceptionAsync(() => ExecuteHandler(command));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Handle_StreamManagerThrows_EmitsFailedEventAndDoesNotRethrow()
    {
        var command = CreateCommand();
        _streamManagerMock
            .Setup(x => x.EnsureStreamAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("NATS connection refused"));

        var exception = await Record.ExceptionAsync(() => ExecuteHandler(command));

        Assert.Null(exception);
        _streamWriterMock.Verify(
            x => x.WriteEventAsync(It.Is<ExecutionFailedEvent>(
                e => e.ErrorMessage == "NATS connection refused")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SuccessfulExecution_DoesNotEmitFailedEvent()
    {
        var command = CreateCommand();

        await ExecuteHandler(command);

        _streamWriterMock.Verify(
            x => x.WriteEventAsync(It.IsAny<ExecutionFailedEvent>()),
            Times.Never);
    }

    #endregion

    private Task ExecuteHandler(ExecuteOrchestrationCommand command)
    {
        return ExecuteOrchestrationHandler.Handle(
            command,
            _identityContextMock.Object,
            _executorMock.Object,
            _streamManagerMock.Object,
            _streamWriterMock.Object,
            _loggerMock.Object);
    }
}
