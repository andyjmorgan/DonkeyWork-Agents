using System.Net;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Integration.Tests.Base;
using DonkeyWork.Agents.Integration.Tests.Helpers;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;

namespace DonkeyWork.Agents.Integration.Tests.Tests.Controllers;

[Trait("Category", "Integration")]
public class ExternalApiKeysControllerTests : ControllerIntegrationTestBase
{
    private const string BaseUrl = "/api/v1/credentials";

    public ExternalApiKeysControllerTests(InfrastructureFixture infrastructure)
        : base(infrastructure)
    {
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedKey()
    {
        // Arrange
        var request = TestDataBuilder.CreateExternalApiKeyRequest(
            provider: ExternalApiKeyProvider.OpenAI,
            name: "My OpenAI Key",
            apiKey: "sk-test-12345");

        // Act
        var response = await PostResponseAsync(BaseUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var key = await response.Content.ReadFromJsonAsync<ExternalApiKeyResponseV1>(JsonOptions);
        Assert.NotNull(key);
        Assert.NotEqual(Guid.Empty, key.Id);
        Assert.Equal(ExternalApiKeyProvider.OpenAI, key.Provider);
        Assert.Equal("My OpenAI Key", key.Name);
        Assert.Equal("sk-test-12345", key.ApiKey);
    }

    [Fact]
    public async Task Create_AllProviders_CreatesSuccessfully()
    {
        // Arrange & Act & Assert
        var providers = new[]
        {
            ExternalApiKeyProvider.OpenAI,
            ExternalApiKeyProvider.Anthropic,
            ExternalApiKeyProvider.Google
        };

        foreach (var provider in providers)
        {
            var request = TestDataBuilder.CreateExternalApiKeyRequest(provider: provider);
            var key = await PostAsync<ExternalApiKeyResponseV1>(BaseUrl, request);
            Assert.NotNull(key);
            Assert.Equal(provider, key.Provider);
        }
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task Get_ExistingKey_ReturnsFullApiKey()
    {
        // Arrange
        var request = TestDataBuilder.CreateExternalApiKeyRequest(apiKey: "sk-secret-key-12345");
        var created = await PostAsync<ExternalApiKeyResponseV1>(BaseUrl, request);

        // Act
        var key = await GetAsync<ExternalApiKeyResponseV1>($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.NotNull(key);
        Assert.Equal(created.Id, key.Id);
        Assert.Equal("sk-secret-key-12345", key.ApiKey); // Full key returned
    }

    [Fact]
    public async Task Get_NonExistingKey_ReturnsNotFound()
    {
        // Act
        var response = await GetResponseAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region List Tests

    [Fact]
    public async Task List_ReturnsAllUserKeys()
    {
        // Arrange
        await PostAsync<ExternalApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateExternalApiKeyRequest(ExternalApiKeyProvider.OpenAI));
        await PostAsync<ExternalApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateExternalApiKeyRequest(ExternalApiKeyProvider.Anthropic));
        await PostAsync<ExternalApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateExternalApiKeyRequest(ExternalApiKeyProvider.Google));

        // Act
        var response = await GetAsync<PaginatedResponse<ExternalApiKeyItemV1>>(BaseUrl);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(3, response.TotalCount);
        Assert.Equal(3, response.Items.Count);
    }

    [Fact]
    public async Task List_WithNoKeys_ReturnsEmptyList()
    {
        // Act
        var response = await GetAsync<PaginatedResponse<ExternalApiKeyItemV1>>(BaseUrl);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(0, response.TotalCount);
        Assert.Empty(response.Items);
    }

    [Fact]
    public async Task List_ReturnsMaskedKeys()
    {
        // Arrange
        var created = await PostAsync<ExternalApiKeyResponseV1>(
            BaseUrl,
            TestDataBuilder.CreateExternalApiKeyRequest(apiKey: "sk-secret-key-12345"));

        // Act
        var response = await GetAsync<PaginatedResponse<ExternalApiKeyItemV1>>(BaseUrl);

        // Assert
        Assert.NotNull(response);
        Assert.Single(response.Items);

        var item = response.Items[0];
        Assert.NotEqual("sk-secret-key-12345", item.MaskedApiKey); // Should be masked
    }

    [Fact]
    public async Task List_SupportsPagination()
    {
        // Arrange - Create 5 keys
        for (var i = 1; i <= 5; i++)
        {
            await PostAsync<ExternalApiKeyResponseV1>(
                BaseUrl,
                TestDataBuilder.CreateExternalApiKeyRequest(name: $"Key {i}"));
        }

        // Act - Get second page with limit of 2
        var response = await GetAsync<PaginatedResponse<ExternalApiKeyItemV1>>($"{BaseUrl}?offset=2&limit=2");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(5, response.TotalCount);
        Assert.Equal(2, response.Items.Count);
        Assert.Equal(2, response.Offset);
        Assert.Equal(2, response.Limit);
    }

    #endregion

    #region ListByProvider Tests

    [Fact]
    public async Task ListByProvider_ReturnsOnlyMatchingProvider()
    {
        // Arrange
        await PostAsync<ExternalApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateExternalApiKeyRequest(ExternalApiKeyProvider.OpenAI, "OpenAI Key 1"));
        await PostAsync<ExternalApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateExternalApiKeyRequest(ExternalApiKeyProvider.OpenAI, "OpenAI Key 2"));
        await PostAsync<ExternalApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateExternalApiKeyRequest(ExternalApiKeyProvider.Anthropic, "Anthropic Key"));

        // Act
        var keys = await GetAsync<List<ExternalApiKeyItemV1>>($"{BaseUrl}/provider/OpenAI");

        // Assert
        Assert.NotNull(keys);
        Assert.Equal(2, keys.Count);
        Assert.All(keys, k => Assert.Equal(ExternalApiKeyProvider.OpenAI, k.Provider));
    }

    [Fact]
    public async Task ListByProvider_NoMatchingKeys_ReturnsEmptyList()
    {
        // Arrange
        await PostAsync<ExternalApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateExternalApiKeyRequest(ExternalApiKeyProvider.OpenAI));

        // Act
        var keys = await GetAsync<List<ExternalApiKeyItemV1>>($"{BaseUrl}/provider/Anthropic");

        // Assert
        Assert.NotNull(keys);
        Assert.Empty(keys);
    }

    #endregion

    #region GetConfiguredLlmProviders Tests

    [Fact]
    public async Task GetConfiguredLlmProviders_ReturnsConfiguredProviders()
    {
        // Arrange
        await PostAsync<ExternalApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateExternalApiKeyRequest(ExternalApiKeyProvider.OpenAI));
        await PostAsync<ExternalApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateExternalApiKeyRequest(ExternalApiKeyProvider.Anthropic));

        // Act
        var providers = await GetAsync<List<ExternalApiKeyProvider>>($"{BaseUrl}/llm-providers");

        // Assert
        Assert.NotNull(providers);
        Assert.Contains(ExternalApiKeyProvider.OpenAI, providers);
        Assert.Contains(ExternalApiKeyProvider.Anthropic, providers);
    }

    [Fact]
    public async Task GetConfiguredLlmProviders_NoKeys_ReturnsEmptyList()
    {
        // Act
        var providers = await GetAsync<List<ExternalApiKeyProvider>>($"{BaseUrl}/llm-providers");

        // Assert
        Assert.NotNull(providers);
        Assert.Empty(providers);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingKey_ReturnsNoContent()
    {
        // Arrange
        var created = await PostAsync<ExternalApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateExternalApiKeyRequest());

        // Act
        var response = await DeleteAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deletion
        var getResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistingKey_ReturnsNotFound()
    {
        // Act
        var response = await DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task Get_KeyBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create key as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var created = await PostAsync<ExternalApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateExternalApiKeyRequest());

        // Act - Try to get as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await GetResponseAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_OnlyReturnsKeysForCurrentUser()
    {
        // Arrange - Create keys for user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        await PostAsync<ExternalApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateExternalApiKeyRequest(name: "User1 Key"));

        // Create keys for user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        await PostAsync<ExternalApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateExternalApiKeyRequest(name: "User2 Key"));

        // Act - List as user 2
        var response = await GetAsync<PaginatedResponse<ExternalApiKeyItemV1>>(BaseUrl);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(1, response.TotalCount);
        Assert.Single(response.Items);
        Assert.Equal("User2 Key", response.Items[0].Name);
    }

    [Fact]
    public async Task Delete_KeyBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create key as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var created = await PostAsync<ExternalApiKeyResponseV1>(BaseUrl, TestDataBuilder.CreateExternalApiKeyRequest());

        // Act - Try to delete as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await DeleteAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verify key still exists for user 1
        SetTestUser(user1);
        var getResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    #endregion
}
