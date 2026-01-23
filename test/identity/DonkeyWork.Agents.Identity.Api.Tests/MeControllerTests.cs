using DonkeyWork.Agents.Identity.Api.Controllers;
using DonkeyWork.Agents.Identity.Contracts.Models;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Identity.Api.Tests;

public class MeControllerTests
{
    private readonly Mock<IIdentityContext> _identityContextMock;
    private readonly MeController _controller;

    public MeControllerTests()
    {
        _identityContextMock = new Mock<IIdentityContext>();
        _controller = new MeController(_identityContextMock.Object);
    }

    [Fact]
    public void Get_WithAuthenticatedUser_ReturnsOkWithUserInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var name = "Test User";
        var username = "testuser";

        _identityContextMock.Setup(x => x.UserId).Returns(userId);
        _identityContextMock.Setup(x => x.Email).Returns(email);
        _identityContextMock.Setup(x => x.Name).Returns(name);
        _identityContextMock.Setup(x => x.Username).Returns(username);
        _identityContextMock.Setup(x => x.IsAuthenticated).Returns(true);

        // Act
        var result = _controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<GetMeResponseV1>(okResult.Value);
        Assert.Equal(userId, response.UserId);
        Assert.Equal(email, response.Email);
        Assert.Equal(name, response.Name);
        Assert.Equal(username, response.Username);
        Assert.True(response.IsAuthenticated);
    }

    [Fact]
    public void Get_WithPartialUserInfo_ReturnsOkWithAvailableInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _identityContextMock.Setup(x => x.UserId).Returns(userId);
        _identityContextMock.Setup(x => x.Email).Returns((string?)null);
        _identityContextMock.Setup(x => x.Name).Returns((string?)null);
        _identityContextMock.Setup(x => x.Username).Returns((string?)null);
        _identityContextMock.Setup(x => x.IsAuthenticated).Returns(true);

        // Act
        var result = _controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<GetMeResponseV1>(okResult.Value);
        Assert.Equal(userId, response.UserId);
        Assert.Null(response.Email);
        Assert.Null(response.Name);
        Assert.Null(response.Username);
        Assert.True(response.IsAuthenticated);
    }

    [Fact]
    public void Get_ReturnsIsAuthenticatedFromContext()
    {
        // Arrange
        _identityContextMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _identityContextMock.Setup(x => x.IsAuthenticated).Returns(false);

        // Act
        var result = _controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<GetMeResponseV1>(okResult.Value);
        Assert.False(response.IsAuthenticated);
    }

    [Fact]
    public void Get_MapsAllIdentityContextProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "user@domain.com";
        var name = "Full Name";
        var username = "user123";

        _identityContextMock.Setup(x => x.UserId).Returns(userId);
        _identityContextMock.Setup(x => x.Email).Returns(email);
        _identityContextMock.Setup(x => x.Name).Returns(name);
        _identityContextMock.Setup(x => x.Username).Returns(username);
        _identityContextMock.Setup(x => x.IsAuthenticated).Returns(true);

        // Act
        var result = _controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<GetMeResponseV1>(okResult.Value);

        // Verify all properties are mapped correctly
        _identityContextMock.Verify(x => x.UserId, Times.Once);
        _identityContextMock.Verify(x => x.Email, Times.Once);
        _identityContextMock.Verify(x => x.Name, Times.Once);
        _identityContextMock.Verify(x => x.Username, Times.Once);
        _identityContextMock.Verify(x => x.IsAuthenticated, Times.Once);
    }
}
