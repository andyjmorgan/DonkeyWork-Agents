using DonkeyWork.Agents.Actions.Core.Providers;
using DonkeyWork.Agents.Actions.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actions.Core.Tests.Services;

/// <summary>
/// Integration tests for ActionExecutorService.
/// These tests verify that the executor can discover and execute real action providers.
/// </summary>
public class ActionExecutorServiceTests
{
    [Fact]
    public void Constructor_DiscoversHttpActionProvider_Successfully()
    {
        // Arrange & Act
        var executor = CreateExecutor();

        // Assert
        Assert.True(executor.IsActionRegistered("http_request"));
    }

    [Fact]
    public void GetRegisteredActions_IncludesHttpRequest()
    {
        // Arrange
        var executor = CreateExecutor();

        // Act
        var actions = executor.GetRegisteredActions().ToList();

        // Assert
        Assert.Contains("http_request", actions);
        Assert.NotEmpty(actions);
    }

    [Fact]
    public void IsActionRegistered_WithUnregisteredAction_ReturnsFalse()
    {
        // Arrange
        var executor = CreateExecutor();

        // Act & Assert
        Assert.False(executor.IsActionRegistered("nonexistent_action"));
    }

    [Fact]
    public async Task ExecuteAsync_WithUnregisteredAction_ThrowsException()
    {
        // Arrange
        var executor = CreateExecutor();
        var parameters = new Dictionary<string, object>();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => executor.ExecuteAsync("nonexistent_action", parameters));

        Assert.Contains("not registered", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithHttpRequestProvider_ExecutesSuccessfully()
    {
        // Arrange
        var executor = CreateExecutor();
        var parameters = new HttpRequestParameters
        {
            Method = DonkeyWork.Agents.Actions.Core.Providers.HttpMethod.GET,
            Url = "https://httpbin.org/get",
            TimeoutSeconds = 30
        };

        // Act
        var result = await executor.ExecuteAsync("http_request", parameters);

        // Assert
        Assert.NotNull(result);
        var output = Assert.IsType<HttpRequestOutput>(result);
        Assert.True(output.IsSuccess);
        Assert.Equal(200, output.StatusCode);
        Assert.NotEmpty(output.Body);
    }

    [Fact]
    public async Task ExecuteAsync_WithContext_PassesContextToProvider()
    {
        // Arrange
        var executor = CreateExecutor();
        var parameters = new HttpRequestParameters
        {
            Method = DonkeyWork.Agents.Actions.Core.Providers.HttpMethod.GET,
            Url = "https://httpbin.org/get",
            TimeoutSeconds = 30
        };
        var context = new { testValue = "test" };

        // Act
        var result = await executor.ExecuteAsync("http_request", parameters, context);

        // Assert
        Assert.NotNull(result);
        var output = Assert.IsType<HttpRequestOutput>(result);
        Assert.True(output.IsSuccess);
    }

    private static ActionExecutorService CreateExecutor()
    {
        var services = new ServiceCollection();

        // Register all action providers (same as DI registration)
        services.AddScoped<HttpActionProvider>();
        services.AddHttpClient();
        services.AddLogging();

        // Register parameter resolver and expression engine
        services.AddScoped<DonkeyWork.Agents.Actions.Contracts.Services.IParameterResolver, ParameterResolverService>();
        services.AddScoped<DonkeyWork.Agents.Actions.Contracts.Services.IExpressionEngine, ScribanExpressionEngine>();

        var serviceProvider = services.BuildServiceProvider();

        return new ActionExecutorService(
            serviceProvider,
            serviceProvider.GetRequiredService<ILogger<ActionExecutorService>>());
    }
}
