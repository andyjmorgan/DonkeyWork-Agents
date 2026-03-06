using System.Net;
using System.Net.Sockets;
using System.Text;
using CodeSandbox.AuthProxy.Configuration;
using CodeSandbox.AuthProxy.Credentials;

namespace CodeSandbox.AuthProxy.Proxy;

public class ProxyServer : BackgroundService
{
    private readonly ProxyConfiguration _config;
    private readonly TlsMitmHandler _mitmHandler;
    private readonly ICredentialProvider _credentialProvider;
    private readonly ILogger<ProxyServer> _logger;
    private readonly HashSet<string> _blockedDomains;

    public ProxyServer(
        ProxyConfiguration config,
        TlsMitmHandler mitmHandler,
        ICredentialProvider credentialProvider,
        ILogger<ProxyServer> logger)
    {
        _config = config;
        _mitmHandler = mitmHandler;
        _credentialProvider = credentialProvider;
        _logger = logger;

        _blockedDomains = new HashSet<string>(config.BlockedDomains, StringComparer.OrdinalIgnoreCase);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, _config.ProxyPort);
        listener.Start();

        _logger.LogInformation("Proxy server listening on port {Port}", _config.ProxyPort);
        _logger.LogInformation("Blocked domains: {Domains}",
            _blockedDomains.Count > 0 ? string.Join(", ", _blockedDomains) : "(none)");
        _logger.LogInformation("Credential provider: {Provider}", _credentialProvider.GetType().Name);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);
                _ = Task.Run(() => HandleClientAsync(client, stoppingToken), stoppingToken);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            var stream = client.GetStream();
            try
            {
                var requestLine = await ReadHttpRequestLineAsync(stream, cancellationToken);
                if (requestLine == null)
                {
                    return;
                }

                var (method, host, port) = ParseConnectRequest(requestLine);
                if (method == null || !method.Equals("CONNECT", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Non-CONNECT method received: {Method}", method);
                    await SendResponseAsync(stream, "HTTP/1.1 405 Method Not Allowed\r\n\r\n", cancellationToken);
                    return;
                }

                if (host == null)
                {
                    _logger.LogWarning("Invalid CONNECT request: {RequestLine}", requestLine);
                    await SendResponseAsync(stream, "HTTP/1.1 400 Bad Request\r\n\r\n", cancellationToken);
                    return;
                }

                await ReadRemainingHeadersAsync(stream, cancellationToken);

                if (IsDomainBlocked(host))
                {
                    _logger.LogWarning("CONNECT request: {Host}:{Port} - BLOCKED (in blocklist)", host, port);
                    await SendResponseAsync(stream,
                        "HTTP/1.1 403 Forbidden\r\nContent-Type: text/plain\r\n\r\nDomain is blocked\r\n",
                        cancellationToken);
                    return;
                }

                await SendResponseAsync(stream, "HTTP/1.1 200 Connection Established\r\n\r\n", cancellationToken);

                var headersToInject = await _credentialProvider.GetHeadersForDomainAsync(host, cancellationToken);
                if (headersToInject is not null && headersToInject.Count > 0)
                {
                    // MITM + header injection for credential domains
                    _logger.LogInformation("CONNECT {Host}:{Port} - MITM | injecting {Count} header(s)",
                        host, port, headersToInject.Count);
                    await _mitmHandler.HandleMitmConnectionAsync(stream, host, port, cancellationToken, headersToInject);
                }
                else
                {
                    // Raw TCP tunnel for all other domains (no TLS interception)
                    _logger.LogDebug("CONNECT {Host}:{Port} - TUNNEL (passthrough)", host, port);
                    await RawTunnelAsync(stream, host, port, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
            {
                _logger.LogDebug("Client connection closed: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling client connection");
            }
        }
    }

    private async Task<string?> ReadHttpRequestLineAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var sb = new StringBuilder();

        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
                return null;

            sb.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));

            var data = sb.ToString();
            var lineEnd = data.IndexOf("\r\n", StringComparison.Ordinal);
            if (lineEnd >= 0)
            {
                return data[..lineEnd];
            }

            if (sb.Length > 4096)
            {
                _logger.LogWarning("Request line too long, aborting");
                return null;
            }
        }
    }

    private async Task ReadRemainingHeadersAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var sb = new StringBuilder();

        while (true)
        {
            if (sb.ToString().Contains("\r\n\r\n", StringComparison.Ordinal) ||
                sb.ToString().EndsWith("\r\n", StringComparison.Ordinal))
            {
                break;
            }

            if (stream.DataAvailable)
            {
                var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0) break;
                sb.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
            }
            else
            {
                break;
            }
        }
    }

    internal static (string? Method, string? Host, int Port) ParseConnectRequest(string requestLine)
    {
        var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return (null, null, 0);

        var method = parts[0];
        var target = parts[1];

        var colonIndex = target.LastIndexOf(':');
        if (colonIndex > 0 && int.TryParse(target[(colonIndex + 1)..], out var port))
        {
            var host = target[..colonIndex];
            return (method, host, port);
        }

        return (method, target, 443);
    }

    public bool IsDomainBlocked(string host)
    {
        return _blockedDomains.Contains(host);
    }

    private static async Task RawTunnelAsync(NetworkStream clientStream, string host, int port, CancellationToken cancellationToken)
    {
        using var upstream = new TcpClient();
        await upstream.ConnectAsync(host, port, cancellationToken);
        var upstreamStream = upstream.GetStream();

        var clientToUpstream = clientStream.CopyToAsync(upstreamStream, cancellationToken);
        var upstreamToClient = upstreamStream.CopyToAsync(clientStream, cancellationToken);

        await Task.WhenAny(clientToUpstream, upstreamToClient);
    }

    private static async Task SendResponseAsync(NetworkStream stream, string response, CancellationToken cancellationToken)
    {
        var bytes = Encoding.ASCII.GetBytes(response);
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
