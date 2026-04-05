using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Mcp.Core.Services;
using DonkeyWork.Agents.Persistence.Entities.Mcp;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Mcp.Core.Tests.Services;

public class McpTraceServiceTests
{
    private readonly Mock<IMcpTraceRepository> _repositoryMock;
    private readonly Mock<IIdentityContext> _identityContextMock;
    private readonly McpTraceService _service;
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public McpTraceServiceTests()
    {
        _repositoryMock = new Mock<IMcpTraceRepository>();
        _identityContextMock = new Mock<IIdentityContext>();
        _identityContextMock.Setup(x => x.UserId).Returns(_testUserId);
        _service = new McpTraceService(_repositoryMock.Object, _identityContextMock.Object);
    }

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_ReturnsPagedResults()
    {
        // Arrange
        var entities = new List<McpTraceEntity>
        {
            CreateEntity("tools/list"),
            CreateEntity("tools/call"),
        };
        _repositoryMock.Setup(r => r.ListAsync(_testUserId, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((entities as IReadOnlyList<McpTraceEntity>, 2));

        // Act
        var result = await _service.ListAsync(0, 20);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(0, result.Offset);
        Assert.Equal(20, result.Limit);
    }

    [Fact]
    public async Task ListAsync_MapsEntityFieldsToSummary()
    {
        // Arrange
        var entity = CreateEntity("tools/call", isSuccess: true, durationMs: 150);
        _repositoryMock.Setup(r => r.ListAsync(_testUserId, 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([entity], 1));

        // Act
        var result = await _service.ListAsync(0, 10);

        // Assert
        var summary = Assert.Single(result.Items);
        Assert.Equal(entity.Id, summary.Id);
        Assert.Equal("tools/call", summary.Method);
        Assert.Equal(200, summary.HttpStatusCode);
        Assert.True(summary.IsSuccess);
        Assert.Equal(150, summary.DurationMs);
        Assert.Equal(entity.StartedAt, summary.StartedAt);
    }

    [Fact]
    public async Task ListAsync_UsesIdentityContextUserId()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<McpTraceEntity>() as IReadOnlyList<McpTraceEntity>, 0));

        // Act
        await _service.ListAsync(0, 20);

        // Assert
        _repositoryMock.Verify(r => r.ListAsync(_testUserId, 0, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_EmptyResults_ReturnsEmptyPaginatedResponse()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ListAsync(_testUserId, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<McpTraceEntity>() as IReadOnlyList<McpTraceEntity>, 0));

        // Act
        var result = await _service.ListAsync(0, 20);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.False(result.HasMore);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingTrace_ReturnsDetail()
    {
        // Arrange
        var entity = CreateEntity("tools/call", isSuccess: true, durationMs: 200);
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetByIdAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.UserId, result.UserId);
        Assert.Equal("tools/call", result.Method);
        Assert.Equal(entity.JsonRpcId, result.JsonRpcId);
        Assert.Equal(entity.RequestBody, result.RequestBody);
        Assert.Equal(entity.ResponseBody, result.ResponseBody);
        Assert.Equal(200, result.HttpStatusCode);
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.DurationMs);
        Assert.Equal(entity.StartedAt, result.StartedAt);
        Assert.Equal(entity.CompletedAt, result.CompletedAt);
        Assert.Equal(entity.CreatedAt, result.CreatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentTrace_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((McpTraceEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ErrorTrace_MapsErrorFields()
    {
        // Arrange
        var entity = CreateEntity("tools/call", isSuccess: false, errorMessage: "Tool not found");
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetByIdAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal("Tool not found", result.ErrorMessage);
    }

    #endregion

    private McpTraceEntity CreateEntity(
        string method,
        bool isSuccess = true,
        int? durationMs = null,
        string? errorMessage = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new McpTraceEntity
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Method = method,
            JsonRpcId = "1",
            RequestBody = $$$"""{"jsonrpc":"2.0","method":"{{{method}}}","id":"1"}""",
            ResponseBody = """{"jsonrpc":"2.0","result":{},"id":"1"}""",
            HttpStatusCode = isSuccess ? 200 : 500,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            ClientIpAddress = "127.0.0.1",
            UserAgent = "TestClient/1.0",
            StartedAt = now.AddSeconds(-1),
            CompletedAt = now,
            DurationMs = durationMs ?? 100,
            CreatedAt = now,
        };
    }
}
