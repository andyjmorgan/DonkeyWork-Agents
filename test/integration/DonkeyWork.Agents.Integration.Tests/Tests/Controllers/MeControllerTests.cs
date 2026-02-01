using System.Net;
using DonkeyWork.Agents.Identity.Contracts.Models;
using DonkeyWork.Agents.Integration.Tests.Base;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;

namespace DonkeyWork.Agents.Integration.Tests.Tests.Controllers;

[Trait("Category", "Integration")]
public class MeControllerTests : ControllerIntegrationTestBase
{
    private const string BaseUrl = "/api/v1/me";

    public MeControllerTests(InfrastructureFixture infrastructure)
        : base(infrastructure)
    {
    }

    #region Get Tests

    [Fact]
    public async Task Get_AuthenticatedUser_ReturnsUserInfo()
    {
        // Arrange - Use default test user
        UseDefaultTestUser();

        // Act
        var response = await GetAsync<GetMeResponseV1>(BaseUrl);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(TestUser.Default.UserId, response.UserId);
        Assert.Equal(TestUser.Default.Email, response.Email);
        Assert.Equal(TestUser.Default.Name, response.Name);
        Assert.Equal(TestUser.Default.Username, response.Username);
        Assert.True(response.IsAuthenticated);
    }

    [Fact]
    public async Task Get_CustomUser_ReturnsCorrectUserInfo()
    {
        // Arrange
        var customUser = TestUser.CreateRandom();
        SetTestUser(customUser);

        // Act
        var response = await GetAsync<GetMeResponseV1>(BaseUrl);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(customUser.UserId, response.UserId);
        Assert.Equal(customUser.Email, response.Email);
        Assert.Equal(customUser.Name, response.Name);
        Assert.Equal(customUser.Username, response.Username);
        Assert.True(response.IsAuthenticated);
    }

    [Fact]
    public async Task Get_SwitchingUsers_ReturnsCorrectUserForEach()
    {
        // Arrange
        var user1 = TestUser.CreateRandom();
        var user2 = TestUser.CreateRandom();

        // Act - Get as user 1
        SetTestUser(user1);
        var response1 = await GetAsync<GetMeResponseV1>(BaseUrl);

        // Act - Get as user 2
        SetTestUser(user2);
        var response2 = await GetAsync<GetMeResponseV1>(BaseUrl);

        // Assert
        Assert.NotNull(response1);
        Assert.NotNull(response2);
        Assert.Equal(user1.UserId, response1.UserId);
        Assert.Equal(user2.UserId, response2.UserId);
        Assert.NotEqual(response1.UserId, response2.UserId);
    }

    [Fact]
    public async Task Get_ReturnsOkStatusCode()
    {
        // Act
        var response = await GetResponseAsync(BaseUrl);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsValidJsonResponse()
    {
        // Act
        var response = await GetResponseAsync(BaseUrl);
        var content = await response.Content.ReadAsStringAsync();
        var contentLower = content.ToLower();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("userid", contentLower);
        Assert.Contains("email", contentLower);
        Assert.Contains("name", contentLower);
        Assert.Contains("username", contentLower);
        Assert.Contains("isauthenticated", contentLower);
    }

    #endregion
}
