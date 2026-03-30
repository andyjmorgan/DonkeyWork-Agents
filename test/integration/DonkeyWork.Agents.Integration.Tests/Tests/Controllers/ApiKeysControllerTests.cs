using System.Net;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Integration.Tests.Base;
using DonkeyWork.Agents.Integration.Tests.Helpers;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;

namespace DonkeyWork.Agents.Integration.Tests.Tests.Controllers;

[Trait("Category", "Integration")]
public class ApiKeysControllerTests : ControllerIntegrationTestBase
{
    private const string BaseUrl = "/api/v1/apikeys";

    public ApiKeysControllerTests(InfrastructureFixture infrastructure)
        : base(infrastructure)
    {
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedApiKey()
    {
        // Arrange
        var request = TestDataBuilder.CreateApiKeyRequest("my-api-key", "My test API key");

        // Act
        var response = await PostResponseAsync(BaseUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiKey = await response.Content.ReadFromJsonAsync<CreateApiKeyResponseV1>(JsonOptions);
        Assert.NotNull(apiKey);
        Assert.NotEqual(Guid.Empty, apiKey.Id);
        Assert.Equal(request.Name, apiKey.Name);
        Assert.Equal(request.Description, apiKey.Description);
        Assert.NotNull(apiKey.Key);
        Assert.NotEmpty(apiKey.Key);
    }

    [Fact]
    public async Task Create_ReturnsFullKeyOnlyOnCreate()
    {
        // Arrange
        var request = TestDataBuilder.CreateApiKeyRequest();

        // Act
        var created = await PostAsync<CreateApiKeyResponseV1>(BaseUrl, request);

        // Assert - Full key is returned on create
        Assert.NotNull(created);
        Assert.NotNull(created.Key);
        // Key should be unmasked (full key)
        Assert.DoesNotContain("*", created.Key);
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task Get_ExistingApiKey_ReturnsApiKey()
    {
        // Arrange
        var request = TestDataBuilder.CreateApiKeyRequest();
        var created = await PostAsync<CreateApiKeyResponseV1>(BaseUrl, request);

        // Act
        var apiKey = await GetAsync<GetApiKeyResponseV1>($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.NotNull(apiKey);
        Assert.Equal(created.Id, apiKey.Id);
        Assert.Equal(created.Name, apiKey.Name);
    }

    [Fact]
    public async Task Get_NonExistingApiKey_ReturnsNotFound()
    {
        // Act
        var response = await GetResponseAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region List Tests

    [Fact]
    public async Task List_ReturnsAllUserApiKeys()
    {
        // Arrange
        await PostAsync<CreateApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateApiKeyRequest("key-one"));
        await PostAsync<CreateApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateApiKeyRequest("key-two"));
        await PostAsync<CreateApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateApiKeyRequest("key-three"));

        // Act
        var response = await GetAsync<PaginatedResponse<ApiKeyItemV1>>(BaseUrl);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(3, response.TotalCount);
        Assert.Equal(3, response.Items.Count);
    }

    [Fact]
    public async Task List_WithNoApiKeys_ReturnsEmptyList()
    {
        // Act
        var response = await GetAsync<PaginatedResponse<ApiKeyItemV1>>(BaseUrl);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(0, response.TotalCount);
        Assert.Empty(response.Items);
    }

    [Fact]
    public async Task List_ReturnsMaskedKeys()
    {
        // Arrange
        var created = await PostAsync<CreateApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateApiKeyRequest());

        // Act
        var response = await GetAsync<PaginatedResponse<ApiKeyItemV1>>(BaseUrl);

        // Assert
        Assert.NotNull(response);
        Assert.Single(response.Items);

        var item = response.Items[0];
        // Masked key should contain asterisks or be different from full key
        Assert.NotEqual(created!.Key, item.MaskedKey);
    }

    [Fact]
    public async Task List_SupportsPagination()
    {
        // Arrange - Create 5 API keys
        for (var i = 1; i <= 5; i++)
        {
            await PostAsync<CreateApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateApiKeyRequest($"key-{i}"));
        }

        // Act - Get second page with limit of 2
        var response = await GetAsync<PaginatedResponse<ApiKeyItemV1>>($"{BaseUrl}?offset=2&limit=2");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(5, response.TotalCount);
        Assert.Equal(2, response.Items.Count);
        Assert.Equal(2, response.Offset);
        Assert.Equal(2, response.Limit);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingApiKey_ReturnsNoContent()
    {
        // Arrange
        var created = await PostAsync<CreateApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateApiKeyRequest());

        // Act
        var response = await DeleteAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deletion
        var getResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistingApiKey_ReturnsNotFound()
    {
        // Act
        var response = await DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task Get_ApiKeyBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create API key as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var created = await PostAsync<CreateApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateApiKeyRequest());

        // Act - Try to get as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await GetResponseAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_OnlyReturnsApiKeysForCurrentUser()
    {
        // Arrange - Create API keys for user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        await PostAsync<CreateApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateApiKeyRequest("user1-key"));

        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        await PostAsync<CreateApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateApiKeyRequest("user2-key"));

        // Act - List as user 2
        var response = await GetAsync<PaginatedResponse<ApiKeyItemV1>>(BaseUrl);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(1, response.TotalCount);
        Assert.Single(response.Items);
        Assert.Equal("user2-key", response.Items[0].Name);
    }

    [Fact]
    public async Task Delete_ApiKeyBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create API key as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var created = await PostAsync<CreateApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateApiKeyRequest());

        // Act - Try to delete as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await DeleteAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verify API key still exists for user 1
        SetTestUser(user1);
        var getResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    #endregion
}
