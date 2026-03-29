using System.Diagnostics;
using System.Net.Http.Json;
using DonkeyWork.Agents.A2a.Contracts.Models;
using DonkeyWork.Agents.A2a.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.A2a.Core.Services;

public class A2aServerTestService : IA2aServerTestService
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);

    private readonly IA2aServerConfigurationService _configService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<A2aServerTestService> _logger;

    public A2aServerTestService(
        IA2aServerConfigurationService configService,
        IHttpClientFactory httpClientFactory,
        ILogger<A2aServerTestService> logger)
    {
        _configService = configService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<TestA2aServerResponseV1> TestConnectionAsync(Guid serverId, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        var config = await _configService.GetConnectionConfigByIdAsync(serverId, cancellationToken);
        if (config is null)
        {
            sw.Stop();
            return new TestA2aServerResponseV1
            {
                Success = false,
                ElapsedMs = sw.ElapsedMilliseconds,
                Error = "Server not found.",
            };
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TestTimeout);

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TestTimeout;

            var agentCardUrl = $"{config.Address.TrimEnd('/')}/.well-known/agent.json";

            using var request = new HttpRequestMessage(HttpMethod.Get, agentCardUrl);
            foreach (var header in config.Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            using var response = await client.SendAsync(request, timeoutCts.Token);
            response.EnsureSuccessStatusCode();

            var agentCard = await response.Content.ReadFromJsonAsync<A2aAgentCardV1>(timeoutCts.Token);
            sw.Stop();

            _logger.LogInformation(
                "Test connection to A2A server '{ServerName}' at {Address} succeeded in {ElapsedMs}ms",
                config.Name, config.Address, sw.ElapsedMilliseconds);

            return new TestA2aServerResponseV1
            {
                Success = true,
                ElapsedMs = sw.ElapsedMilliseconds,
                AgentCard = agentCard,
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            _logger.LogError(
                "Test connection to A2A server '{ServerName}' at {Address} timed out after {ElapsedMs}ms",
                config.Name, config.Address, sw.ElapsedMilliseconds);

            return new TestA2aServerResponseV1
            {
                Success = false,
                ElapsedMs = sw.ElapsedMilliseconds,
                Error = "Connection timed out after 30 seconds.",
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "Test connection to A2A server '{ServerName}' at {Address} failed after {ElapsedMs}ms",
                config.Name, config.Address, sw.ElapsedMilliseconds);

            return new TestA2aServerResponseV1
            {
                Success = false,
                ElapsedMs = sw.ElapsedMilliseconds,
                Error = ex.Message,
            };
        }
    }
}
