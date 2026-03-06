using DonkeyWork.Agents.Credentials.Api.Controllers;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Credentials.Api.Tests.Controllers;

public class SandboxCredentialMappingsControllerTests
{
    private readonly Mock<ISandboxCredentialMappingService> _serviceMock;
    private readonly SandboxCredentialMappingsController _controller;

    public SandboxCredentialMappingsControllerTests()
    {
        _serviceMock = new Mock<ISandboxCredentialMappingService>();
        _controller = new SandboxCredentialMappingsController(_serviceMock.Object);
    }

    private static SandboxCredentialMappingV1 CreateMapping(
        Guid? id = null,
        string baseDomain = "api.example.com",
        string headerName = "Authorization",
        string? headerValuePrefix = "Bearer ",
        Guid? credentialId = null,
        string credentialType = "ExternalApiKey",
        CredentialFieldType credentialFieldType = CredentialFieldType.ApiKey)
    {
        return new SandboxCredentialMappingV1
        {
            Id = id ?? Guid.NewGuid(),
            BaseDomain = baseDomain,
            HeaderName = headerName,
            HeaderValuePrefix = headerValuePrefix,
            CredentialId = credentialId ?? Guid.NewGuid(),
            CredentialType = credentialType,
            CredentialFieldType = credentialFieldType,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    #region List Tests

    [Fact]
    public async Task List_ReturnsMappings_ReturnsOk()
    {
        // Arrange
        var mappings = new List<SandboxCredentialMappingV1>
        {
            CreateMapping(baseDomain: "api.openai.com"),
            CreateMapping(baseDomain: "graph.microsoft.com"),
        };

        _serviceMock
            .Setup(s => s.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mappings);

        // Act
        var result = await _controller.List();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IReadOnlyList<SandboxCredentialMappingV1>>(okResult.Value);
        Assert.Equal(2, response.Count);
    }

    [Fact]
    public async Task List_EmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SandboxCredentialMappingV1>());

        // Act
        var result = await _controller.List();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IReadOnlyList<SandboxCredentialMappingV1>>(okResult.Value);
        Assert.Empty(response);
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task Get_ExistingMapping_ReturnsOk()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        var mapping = CreateMapping(id: mappingId);

        _serviceMock
            .Setup(s => s.GetByIdAsync(mappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);

        // Act
        var result = await _controller.Get(mappingId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SandboxCredentialMappingV1>(okResult.Value);
        Assert.Equal(mappingId, response.Id);
    }

    [Fact]
    public async Task Get_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var mappingId = Guid.NewGuid();

        _serviceMock
            .Setup(s => s.GetByIdAsync(mappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SandboxCredentialMappingV1?)null);

        // Act
        var result = await _controller.Get(mappingId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        var request = new CreateSandboxCredentialMappingRequestV1
        {
            BaseDomain = "api.openai.com",
            HeaderName = "Authorization",
            HeaderValuePrefix = "Bearer ",
            CredentialId = Guid.NewGuid(),
            CredentialType = "ExternalApiKey",
            CredentialFieldType = CredentialFieldType.ApiKey,
        };

        var createdMapping = CreateMapping(
            id: mappingId,
            baseDomain: request.BaseDomain,
            headerName: request.HeaderName,
            headerValuePrefix: request.HeaderValuePrefix,
            credentialId: request.CredentialId,
            credentialType: request.CredentialType,
            credentialFieldType: request.CredentialFieldType);

        _serviceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdMapping);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(SandboxCredentialMappingsController.Get), createdResult.ActionName);
        Assert.Equal(mappingId, ((SandboxCredentialMappingV1)createdResult.Value!).Id);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ExistingMapping_ReturnsOk()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        var existingMapping = CreateMapping(id: mappingId, headerName: "X-Api-Key");
        var updatedMapping = CreateMapping(id: mappingId, headerName: "Authorization");

        var request = new UpdateSandboxCredentialMappingRequestV1
        {
            HeaderName = "Authorization",
        };

        _serviceMock
            .Setup(s => s.GetByIdAsync(mappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMapping);

        _serviceMock
            .Setup(s => s.UpdateAsync(mappingId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedMapping);

        // Act
        var result = await _controller.Update(mappingId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SandboxCredentialMappingV1>(okResult.Value);
        Assert.Equal("Authorization", response.HeaderName);
    }

    [Fact]
    public async Task Update_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        var request = new UpdateSandboxCredentialMappingRequestV1
        {
            HeaderName = "Authorization",
        };

        _serviceMock
            .Setup(s => s.GetByIdAsync(mappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SandboxCredentialMappingV1?)null);

        // Act
        var result = await _controller.Update(mappingId, request);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingMapping_ReturnsNoContent()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        var existingMapping = CreateMapping(id: mappingId);

        _serviceMock
            .Setup(s => s.GetByIdAsync(mappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMapping);

        // Act
        var result = await _controller.Delete(mappingId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _serviceMock.Verify(s => s.DeleteAsync(mappingId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var mappingId = Guid.NewGuid();

        _serviceMock
            .Setup(s => s.GetByIdAsync(mappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SandboxCredentialMappingV1?)null);

        // Act
        var result = await _controller.Delete(mappingId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _serviceMock.Verify(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetConfiguredDomains Tests

    [Fact]
    public async Task GetConfiguredDomains_ReturnsDomains_ReturnsOk()
    {
        // Arrange
        var domains = new List<string> { "api.openai.com", "graph.microsoft.com" };

        _serviceMock
            .Setup(s => s.GetConfiguredDomainsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(domains);

        // Act
        var result = await _controller.GetConfiguredDomains();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IReadOnlyList<string>>(okResult.Value);
        Assert.Equal(2, response.Count);
    }

    #endregion
}
