using System.Net;
using System.Text.Json;
using DonkeyWork.Agents.Identity.Contracts.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace DonkeyWork.Agents.Identity.Core.Tests;

using DonkeyWork.Agents.Identity.Core.Services;

public class KeycloakServiceTests
{
    private readonly Mock<ILogger<KeycloakService>> _loggerMock;

    public KeycloakServiceTests()
    {
        _loggerMock = new Mock<ILogger<KeycloakService>>();
    }

    [Fact]
    public async Task GetUserInfoAsync_WithValidToken_ReturnsUserInfo()
    {
        // Arrange
        var userInfo = new KeycloakUserInfo
        {
            Sub = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            Name = "Test User",
            PreferredUsername = "testuser"
        };

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, userInfo);
        var service = new KeycloakService(httpClient, _loggerMock.Object);

        // Act
        var result = await service.GetUserInfoAsync("valid-token");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userInfo.Sub, result.Sub);
        Assert.Equal(userInfo.Email, result.Email);
        Assert.Equal(userInfo.Name, result.Name);
        Assert.Equal(userInfo.PreferredUsername, result.PreferredUsername);
    }

    [Fact]
    public async Task GetUserInfoAsync_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.Unauthorized, null);
        var service = new KeycloakService(httpClient, _loggerMock.Object);

        // Act
        var result = await service.GetUserInfoAsync("invalid-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserInfoAsync_WhenServerError_ReturnsNull()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, null);
        var service = new KeycloakService(httpClient, _loggerMock.Object);

        // Act
        var result = await service.GetUserInfoAsync("some-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserInfoAsync_WhenExceptionThrown_ReturnsNullAndLogs()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://auth.example.com/realms/test/")
        };
        var service = new KeycloakService(httpClient, _loggerMock.Object);

        // Act
        var result = await service.GetUserInfoAsync("some-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserInfoAsync_SetsAuthorizationHeader()
    {
        // Arrange
        var accessToken = "test-access-token";
        HttpRequestMessage? capturedRequest = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new KeycloakUserInfo { Sub = "123" }))
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://auth.example.com/realms/test/")
        };
        var service = new KeycloakService(httpClient, _loggerMock.Object);

        // Act
        await service.GetUserInfoAsync(accessToken);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.Equal(accessToken, capturedRequest.Headers.Authorization?.Parameter);
    }

    private static HttpClient CreateMockHttpClient(HttpStatusCode statusCode, KeycloakUserInfo? responseContent)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage(statusCode);

        if (responseContent != null)
        {
            response.Content = new StringContent(JsonSerializer.Serialize(responseContent));
        }

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://auth.example.com/realms/test/")
        };
    }
}
