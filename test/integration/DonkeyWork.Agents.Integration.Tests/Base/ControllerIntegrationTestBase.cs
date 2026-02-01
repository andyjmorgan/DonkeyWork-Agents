using System.Text.Json;
using System.Text.Json.Serialization;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;

namespace DonkeyWork.Agents.Integration.Tests.Base;

public abstract class ControllerIntegrationTestBase : IntegrationTestBase
{
    protected HttpClient Client = null!;
    protected readonly JsonSerializerOptions JsonOptions;

    protected ControllerIntegrationTestBase(InfrastructureFixture infrastructure)
        : base(infrastructure)
    {
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Client = Factory.CreateClient();
    }

    protected void SetTestUser(TestUser user)
    {
        Client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.UserIdHeader);
        Client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.EmailHeader);
        Client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.NameHeader);
        Client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.UsernameHeader);

        Client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, user.UserId.ToString());
        Client.DefaultRequestHeaders.Add(TestAuthenticationHandler.EmailHeader, user.Email);
        Client.DefaultRequestHeaders.Add(TestAuthenticationHandler.NameHeader, user.Name);
        Client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UsernameHeader, user.Username);
    }

    protected void UseDefaultTestUser()
    {
        SetTestUser(TestUser.Default);
    }

    protected async Task<T?> GetAsync<T>(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    protected async Task<HttpResponseMessage> GetResponseAsync(string url)
    {
        return await Client.GetAsync(url);
    }

    protected async Task<T?> PostAsync<T>(string url, object content)
    {
        var response = await Client.PostAsJsonAsync(url, content, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    protected async Task<HttpResponseMessage> PostResponseAsync(string url, object content)
    {
        return await Client.PostAsJsonAsync(url, content, JsonOptions);
    }

    protected async Task<T?> PutAsync<T>(string url, object content)
    {
        var response = await Client.PutAsJsonAsync(url, content, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    protected async Task<HttpResponseMessage> PutResponseAsync(string url, object content)
    {
        return await Client.PutAsJsonAsync(url, content, JsonOptions);
    }

    protected async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        return await Client.DeleteAsync(url);
    }

    public override async Task DisposeAsync()
    {
        Client.Dispose();
        await base.DisposeAsync();
    }
}
