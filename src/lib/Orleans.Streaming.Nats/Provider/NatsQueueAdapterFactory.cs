using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Orleans.Configuration;
using Orleans.Providers.Streams.Common;
using Orleans.Streaming.Nats.Configuration;
using Orleans.Streams;

namespace Orleans.Streaming.Nats.Provider;

public sealed class NatsQueueAdapterFactory : IQueueAdapterFactory
{
    private readonly string _providerName;
    private readonly NatsStreamOptions _options;
    private readonly SimpleQueueAdapterCache _cache;
    private readonly HashRingBasedStreamQueueMapper _mapper;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    private NatsConnection? _connection;
    private INatsJSContext? _jsContext;

    public NatsQueueAdapterFactory(
        string providerName,
        NatsStreamOptions options,
        SimpleQueueCacheOptions cacheOptions,
        HashRingStreamQueueMapperOptions mapperOptions,
        ILoggerFactory loggerFactory)
    {
        _providerName = providerName;
        _options = options;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<NatsQueueAdapterFactory>();

        _cache = new SimpleQueueAdapterCache(cacheOptions, providerName, loggerFactory);
        _mapper = new HashRingBasedStreamQueueMapper(mapperOptions, providerName);
    }

    public async Task<IQueueAdapter> CreateAdapter()
    {
        var (connection, js) = await GetOrCreateConnection();
        await EnsureStreamExists(js);

        var adapter = new NatsQueueAdapter(
            _providerName,
            js,
            _options.StreamName,
            _options.SubjectPrefix,
            _options.ConsumerName,
            _loggerFactory)
        {
            PartitionCount = _options.Partitions
        };

        return adapter;
    }

    public IQueueAdapterCache GetQueueAdapterCache() => _cache;

    public IStreamQueueMapper GetStreamQueueMapper() => _mapper;

    public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId) =>
        Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler());

    private async Task<(NatsConnection Connection, INatsJSContext Js)> GetOrCreateConnection()
    {
        if (_connection is not null && _jsContext is not null)
            return (_connection, _jsContext);

        _logger.LogInformation("Connecting to NATS at {Url}", _options.Url);

        _connection = new NatsConnection(new NatsOpts { Url = _options.Url });
        await _connection.ConnectAsync();

        _jsContext = new NatsJSContext(_connection);

        return (_connection, _jsContext);
    }

    private async Task EnsureStreamExists(INatsJSContext js)
    {
        var subjects = new List<string>();
        for (var i = 0; i < _options.Partitions; i++)
            subjects.Add($"{_options.SubjectPrefix}.{i}");

        var config = new StreamConfig(_options.StreamName, subjects);

        if (_options.MaxAge.HasValue)
            config.MaxAge = _options.MaxAge.Value;
        if (_options.MaxBytes.HasValue)
            config.MaxBytes = _options.MaxBytes.Value;

        try
        {
            await js.CreateStreamAsync(config);
            _logger.LogInformation(
                "Created JetStream stream {StreamName} with {Partitions} partition subjects",
                _options.StreamName, _options.Partitions);
        }
        catch (NatsJSApiException ex) when (ex.Error.ErrCode == 10058)
        {
            // Stream name already in use — update it to ensure subjects match
            try
            {
                await js.UpdateStreamAsync(config);
                _logger.LogDebug("JetStream stream {StreamName} already exists, updated config",
                    _options.StreamName);
            }
            catch (Exception updateEx)
            {
                _logger.LogDebug(updateEx, "JetStream stream {StreamName} already exists, config update skipped",
                    _options.StreamName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create JetStream stream {StreamName} - it may already exist",
                _options.StreamName);
        }
    }

    public static NatsQueueAdapterFactory Create(IServiceProvider services, string name)
    {
        var optionsSnapshot = services.GetOptionsByName<NatsStreamOptions>(name);
        var cacheOptions = services.GetOptionsByName<SimpleQueueCacheOptions>(name);
        var mapperOptions = services.GetOptionsByName<HashRingStreamQueueMapperOptions>(name);
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();

        return new NatsQueueAdapterFactory(name, optionsSnapshot, cacheOptions, mapperOptions, loggerFactory);
    }
}
