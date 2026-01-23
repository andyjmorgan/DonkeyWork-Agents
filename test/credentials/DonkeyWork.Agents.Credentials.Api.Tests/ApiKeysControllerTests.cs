using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Credentials.Api.Controllers;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Credentials.Api.Tests;

public class ApiKeysControllerTests
{
    private readonly Mock<IUserApiKeyService> _apiKeyServiceMock;
    private readonly ApiKeysController _controller;

    public ApiKeysControllerTests()
    {
        _apiKeyServiceMock = new Mock<IUserApiKeyService>();
        _controller = new ApiKeysController(_apiKeyServiceMock.Object);
    }

    [Fact]
    public async Task List_WithValidPagination_ReturnsOkWithPaginatedResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keys = new List<UserApiKey>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Test Key 1",
                Description = "Description 1",
                Key = "dk_abc***xyz",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Test Key 2",
                Description = null,
                Key = "dk_def***uvw",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _apiKeyServiceMock
            .Setup(s => s.ListAsync(0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((keys, 2));

        var pagination = new PaginationRequest { Offset = 0, Limit = 50 };

        // Act
        var result = await _controller.List(pagination);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<ApiKeyItemV1>>(okResult.Value);
        Assert.Equal(2, response.Items.Count);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(0, response.Offset);
        Assert.Equal(50, response.Limit);
    }

    [Fact]
    public async Task List_WithCustomPagination_PassesCorrectParameters()
    {
        // Arrange
        _apiKeyServiceMock
            .Setup(s => s.ListAsync(10, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserApiKey>(), 0));

        var pagination = new PaginationRequest { Offset = 10, Limit = 25 };

        // Act
        await _controller.List(pagination);

        // Assert
        _apiKeyServiceMock.Verify(s => s.ListAsync(10, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task List_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        _apiKeyServiceMock
            .Setup(s => s.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserApiKey>(), 0));

        var pagination = new PaginationRequest();

        // Act
        var result = await _controller.List(pagination);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<ApiKeyItemV1>>(okResult.Value);
        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public async Task Get_WithExistingId_ReturnsOkWithApiKey()
    {
        // Arrange
        var keyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var apiKey = new UserApiKey
        {
            Id = keyId,
            UserId = userId,
            Name = "Test Key",
            Description = "Test Description",
            Key = "dk_fullkeyvalue123456789012345678901234",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _apiKeyServiceMock
            .Setup(s => s.GetByIdAsync(keyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiKey);

        // Act
        var result = await _controller.Get(keyId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<GetApiKeyResponseV1>(okResult.Value);
        Assert.Equal(keyId, response.Id);
        Assert.Equal("Test Key", response.Name);
        Assert.Equal("Test Description", response.Description);
        Assert.Equal(apiKey.Key, response.Key);
    }

    [Fact]
    public async Task Get_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var keyId = Guid.NewGuid();

        _apiKeyServiceMock
            .Setup(s => s.GetByIdAsync(keyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserApiKey?)null);

        // Act
        var result = await _controller.Get(keyId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsOkWithCreatedApiKey()
    {
        // Arrange
        var keyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new CreateApiKeyRequestV1
        {
            Name = "New Key",
            Description = "New Description"
        };

        var createdKey = new UserApiKey
        {
            Id = keyId,
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            Key = "dk_newkeyvalue12345678901234567890123",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _apiKeyServiceMock
            .Setup(s => s.CreateAsync(request.Name, request.Description, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdKey);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CreateApiKeyResponseV1>(okResult.Value);
        Assert.Equal(keyId, response.Id);
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Description, response.Description);
        Assert.Equal(createdKey.Key, response.Key);
    }

    [Fact]
    public async Task Create_WithoutDescription_PassesNullDescription()
    {
        // Arrange
        var request = new CreateApiKeyRequestV1
        {
            Name = "New Key",
            Description = null
        };

        var createdKey = new UserApiKey
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = request.Name,
            Description = null,
            Key = "dk_newkeyvalue12345678901234567890123",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _apiKeyServiceMock
            .Setup(s => s.CreateAsync(request.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdKey);

        // Act
        await _controller.Create(request);

        // Assert
        _apiKeyServiceMock.Verify(s => s.CreateAsync(request.Name, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        var keyId = Guid.NewGuid();

        _apiKeyServiceMock
            .Setup(s => s.DeleteAsync(keyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(keyId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var keyId = Guid.NewGuid();

        _apiKeyServiceMock
            .Setup(s => s.DeleteAsync(keyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(keyId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task List_MapsKeyToMaskedKey_InResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var maskedKey = "dk_abc***xyz";
        var keys = new List<UserApiKey>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Test Key",
                Description = "Description",
                Key = maskedKey,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _apiKeyServiceMock
            .Setup(s => s.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((keys, 1));

        var pagination = new PaginationRequest();

        // Act
        var result = await _controller.List(pagination);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<ApiKeyItemV1>>(okResult.Value);
        Assert.Single(response.Items);
        Assert.Equal(maskedKey, response.Items[0].MaskedKey);
    }
}
