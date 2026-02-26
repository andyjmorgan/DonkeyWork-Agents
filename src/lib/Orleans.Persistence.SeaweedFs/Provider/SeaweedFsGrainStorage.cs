using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Orleans.Persistence.SeaweedFs.Configuration;
using Orleans.Storage;

namespace Orleans.Persistence.SeaweedFs.Provider;

public sealed class SeaweedFsGrainStorage : IGrainStorage
{
    private readonly string _name;
    private readonly SeaweedFsStorageOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SeaweedFsGrainStorage> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SeaweedFsGrainStorage(
        string name,
        SeaweedFsStorageOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<SeaweedFsGrainStorage> logger)
    {
        _name = name;
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = options.IndentJson,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var path = BuildPath(stateName, grainId);
        var client = _httpClientFactory.CreateClient(_name);

        try
        {
            var response = await client.GetAsync($"{_options.BaseUrl}{path}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogDebug("No state found at {Path} for grain {GrainId}, returning default", path, grainId);
                grainState.State = Activator.CreateInstance<T>();
                grainState.RecordExists = false;
                grainState.ETag = null;
                return;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            grainState.State = JsonSerializer.Deserialize<T>(json, _jsonOptions)!;
            grainState.RecordExists = true;
            grainState.ETag = response.Headers.ETag?.Tag;

            _logger.LogDebug("Read state from {Path} for grain {GrainId}", path, grainId);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            grainState.State = Activator.CreateInstance<T>();
            grainState.RecordExists = false;
            grainState.ETag = null;
        }
    }

    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var path = BuildPath(stateName, grainId);
        var client = _httpClientFactory.CreateClient(_name);

        var json = JsonSerializer.Serialize(grainState.State, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PutAsync($"{_options.BaseUrl}{path}", content);
        response.EnsureSuccessStatusCode();

        grainState.RecordExists = true;
        grainState.ETag = response.Headers.ETag?.Tag;

        _logger.LogDebug("Wrote state to {Path} for grain {GrainId}", path, grainId);
    }

    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var path = BuildPath(stateName, grainId);
        var client = _httpClientFactory.CreateClient(_name);

        var response = await client.DeleteAsync($"{_options.BaseUrl}{path}");

        if (response.StatusCode != HttpStatusCode.NotFound)
            response.EnsureSuccessStatusCode();

        grainState.State = Activator.CreateInstance<T>();
        grainState.RecordExists = false;
        grainState.ETag = null;

        _logger.LogDebug("Cleared state at {Path} for grain {GrainId}", path, grainId);
    }

    private string BuildPath(string stateName, GrainId grainId)
    {
        var grainType = grainId.Type.ToString();
        var grainKey = grainId.Key.ToString();
        return $"{_options.BasePath}/{grainType}/{grainKey}/{stateName}.json";
    }
}
