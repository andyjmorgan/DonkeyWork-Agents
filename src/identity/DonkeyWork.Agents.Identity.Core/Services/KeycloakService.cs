using System.Net.Http.Headers;
using System.Net.Http.Json;
using DonkeyWork.Agents.Identity.Contracts.Models;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Identity.Core.Services;

/// <summary>
/// Service for backchannel communication with Keycloak.
/// </summary>
public sealed class KeycloakService : IKeycloakService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KeycloakService> _logger;

    public KeycloakService(HttpClient httpClient, ILogger<KeycloakService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<KeycloakUserInfo?> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "protocol/openid-connect/userinfo");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to retrieve user info from Keycloak. Status: {StatusCode}",
                    response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<KeycloakUserInfo>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user info from Keycloak");
            return null;
        }
    }
}
