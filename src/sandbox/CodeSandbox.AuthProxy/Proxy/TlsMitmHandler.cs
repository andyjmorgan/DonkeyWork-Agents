using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace CodeSandbox.AuthProxy.Proxy;

public class TlsMitmHandler
{
    private readonly CertificateGenerator _certGenerator;
    private readonly ILogger<TlsMitmHandler> _logger;
    private const int BufferSize = 8192;

    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Proxy-Authorization", "Cookie", "Set-Cookie",
        "X-Api-Key", "X-Auth-Token", "X-Access-Token"
    };

    public TlsMitmHandler(CertificateGenerator certGenerator, ILogger<TlsMitmHandler> logger)
    {
        _certGenerator = certGenerator;
        _logger = logger;
    }

    public async Task HandleMitmConnectionAsync(
        Stream clientStream, string targetHost, int targetPort, CancellationToken cancellationToken,
        Dictionary<string, string>? headersToInject = null)
    {
        var domainCert = _certGenerator.GetOrCreateCertificate(targetHost);

        var clientSsl = new SslStream(clientStream, leaveInnerStreamOpen: true);
        try
        {
            await clientSsl.AuthenticateAsServerAsync(
                new SslServerAuthenticationOptions
                {
                    ServerCertificate = domainCert,
                    ClientCertificateRequired = false,
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                                          System.Security.Authentication.SslProtocols.Tls13
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TLS handshake failed with client for {Host}", targetHost);
            clientSsl.Dispose();
            return;
        }

        TcpClient? upstreamTcp = null;
        SslStream? upstreamSsl = null;
        try
        {
            upstreamTcp = new TcpClient();
            await upstreamTcp.ConnectAsync(targetHost, targetPort, cancellationToken);

            upstreamSsl = new SslStream(upstreamTcp.GetStream(), leaveInnerStreamOpen: false,
                (sender, certificate, chain, errors) => true);

            await upstreamSsl.AuthenticateAsClientAsync(
                new SslClientAuthenticationOptions
                {
                    TargetHost = targetHost,
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                                          System.Security.Authentication.SslProtocols.Tls13
                },
                cancellationToken);

            _logger.LogInformation("Upstream TLS established to {Host}:{Port}", targetHost, targetPort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to upstream {Host}:{Port}", targetHost, targetPort);
            clientSsl.Dispose();
            upstreamSsl?.Dispose();
            upstreamTcp?.Dispose();
            return;
        }

        try
        {
            var inject = headersToInject is { Count: > 0 };
            _logger.LogInformation(
                "MITM relay started for {Host}:{Port} | injection={Inject}",
                targetHost, targetPort, inject ? $"yes ({headersToInject!.Count} header(s))" : "no");

            // Client→Upstream: always parse requests for audit, optionally inject headers
            var clientToUpstream = RelayRequestsAsync(
                clientSsl, upstreamSsl, targetHost, headersToInject, cancellationToken);

            // Upstream→Client: parse responses for audit logging
            var upstreamToClient = RelayResponsesAsync(
                upstreamSsl, clientSsl, targetHost, cancellationToken);

            await Task.WhenAny(clientToUpstream, upstreamToClient);

            _logger.LogInformation("MITM relay ended for {Host}:{Port}", targetHost, targetPort);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
        {
            _logger.LogDebug("Connection terminated for {Host}: {Message}", targetHost, ex.Message);
        }
        finally
        {
            clientSsl.Dispose();
            upstreamSsl.Dispose();
            upstreamTcp.Dispose();
        }
    }

    /// <summary>
    /// Relays HTTP requests from client to upstream, parsing each request for audit logging
    /// and optionally injecting headers into every request.
    /// </summary>
    internal async Task RelayRequestsAsync(
        Stream source, Stream destination, string host,
        Dictionary<string, string>? headersToInject, CancellationToken cancellationToken)
    {
        // Pre-build the header lines to inject (empty if no injection)
        byte[] injectionBytes;
        if (headersToInject is { Count: > 0 })
        {
            var sb = new StringBuilder();
            foreach (var (name, value) in headersToInject)
            {
                sb.Append(name).Append(": ").Append(value).Append("\r\n");
            }
            injectionBytes = Encoding.ASCII.GetBytes(sb.ToString());
        }
        else
        {
            injectionBytes = Array.Empty<byte>();
        }

        var headerEndMarker = Encoding.ASCII.GetBytes("\r\n\r\n");
        var buffer = new byte[BufferSize];
        var pendingData = Array.Empty<byte>();
        var parsingHeaders = true;
        long bodyBytesRemaining = 0;
        var requestCount = 0;
        var fallbackToRawCopy = false;

        try
        {
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                if (fallbackToRawCopy)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    await destination.FlushAsync(cancellationToken);
                    continue;
                }

                byte[] workingData;
                if (pendingData.Length > 0)
                {
                    workingData = new byte[pendingData.Length + bytesRead];
                    Buffer.BlockCopy(pendingData, 0, workingData, 0, pendingData.Length);
                    Buffer.BlockCopy(buffer, 0, workingData, pendingData.Length, bytesRead);
                    pendingData = Array.Empty<byte>();
                }
                else
                {
                    workingData = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, workingData, 0, bytesRead);
                }

                var offset = 0;

                while (offset < workingData.Length)
                {
                    if (parsingHeaders)
                    {
                        var markerIdx = FindSequence(workingData, headerEndMarker, offset);
                        if (markerIdx < 0)
                            break; // Need more data

                        var headerText = Encoding.ASCII.GetString(workingData, offset, markerIdx - offset);
                        requestCount++;

                        // === AUDIT: Log the request ===
                        LogAuditMessage(host, "REQUEST", requestCount, headerText, headersToInject);

                        if (injectionBytes.Length > 0)
                        {
                            var filtered = StripHeaders(headerText, headersToInject!.Keys);
                            await destination.WriteAsync(
                                Encoding.ASCII.GetBytes(filtered), cancellationToken);
                            await destination.WriteAsync(
                                Encoding.ASCII.GetBytes("\r\n"), cancellationToken);
                            await destination.WriteAsync(injectionBytes, cancellationToken);
                            await destination.WriteAsync(
                                Encoding.ASCII.GetBytes("\r\n"), cancellationToken);
                        }
                        else
                        {
                            await destination.WriteAsync(
                                workingData.AsMemory(offset, markerIdx - offset), cancellationToken);
                            await destination.WriteAsync(
                                Encoding.ASCII.GetBytes("\r\n\r\n"), cancellationToken);
                        }

                        offset = markerIdx + headerEndMarker.Length;

                        // Chunked transfer → fall back to raw copy
                        if (HasChunkedTransferEncoding(headerText))
                        {
                            _logger.LogDebug(
                                "Chunked transfer-encoding in request #{Num}, switching to raw copy for {Host}",
                                requestCount, host);
                            if (offset < workingData.Length)
                            {
                                await destination.WriteAsync(
                                    workingData.AsMemory(offset, workingData.Length - offset), cancellationToken);
                            }
                            await destination.FlushAsync(cancellationToken);
                            fallbackToRawCopy = true;
                            offset = workingData.Length;
                            break;
                        }

                        bodyBytesRemaining = ParseContentLength(headerText);
                        if (bodyBytesRemaining > 0)
                        {
                            parsingHeaders = false;
                        }

                        await destination.FlushAsync(cancellationToken);
                    }
                    else
                    {
                        // Forwarding body bytes (Content-Length delimited)
                        var available = workingData.Length - offset;
                        var toWrite = (int)Math.Min(available, bodyBytesRemaining);
                        await destination.WriteAsync(
                            workingData.AsMemory(offset, toWrite), cancellationToken);
                        await destination.FlushAsync(cancellationToken);
                        bodyBytesRemaining -= toWrite;
                        offset += toWrite;

                        if (bodyBytesRemaining <= 0)
                        {
                            parsingHeaders = true;
                        }
                    }
                }

                if (offset < workingData.Length && !fallbackToRawCopy)
                {
                    pendingData = new byte[workingData.Length - offset];
                    Buffer.BlockCopy(workingData, offset, pendingData, 0, pendingData.Length);
                }
            }

            if (pendingData.Length > 0)
            {
                await destination.WriteAsync(pendingData, cancellationToken);
                await destination.FlushAsync(cancellationToken);
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
        {
            _logger.LogDebug("client->upstream stream ended for {Host}: {Message}", host, ex.Message);
        }
    }

    /// <summary>
    /// Relays HTTP responses from upstream to client, parsing each response for audit logging.
    /// Handles Content-Length delimited bodies. Falls back to raw copy for chunked responses
    /// after logging the initial response headers.
    /// </summary>
    internal async Task RelayResponsesAsync(
        Stream source, Stream destination, string host, CancellationToken cancellationToken)
    {
        var headerEndMarker = Encoding.ASCII.GetBytes("\r\n\r\n");
        var buffer = new byte[BufferSize];
        var pendingData = Array.Empty<byte>();
        var parsingHeaders = true;
        long bodyBytesRemaining = 0;
        var responseCount = 0;
        var fallbackToRawCopy = false;

        try
        {
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                if (fallbackToRawCopy)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    await destination.FlushAsync(cancellationToken);
                    continue;
                }

                byte[] workingData;
                if (pendingData.Length > 0)
                {
                    workingData = new byte[pendingData.Length + bytesRead];
                    Buffer.BlockCopy(pendingData, 0, workingData, 0, pendingData.Length);
                    Buffer.BlockCopy(buffer, 0, workingData, pendingData.Length, bytesRead);
                    pendingData = Array.Empty<byte>();
                }
                else
                {
                    workingData = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, workingData, 0, bytesRead);
                }

                var offset = 0;

                while (offset < workingData.Length)
                {
                    if (parsingHeaders)
                    {
                        var markerIdx = FindSequence(workingData, headerEndMarker, offset);
                        if (markerIdx < 0)
                            break; // Need more data

                        var headerText = Encoding.ASCII.GetString(workingData, offset, markerIdx - offset);
                        responseCount++;

                        // === AUDIT: Log the response ===
                        LogAuditMessage(host, "RESPONSE", responseCount, headerText, null);

                        await destination.WriteAsync(
                            workingData.AsMemory(offset, markerIdx - offset + headerEndMarker.Length),
                            cancellationToken);

                        offset = markerIdx + headerEndMarker.Length;

                        // Chunked response → fall back to raw copy after logging headers
                        if (HasChunkedTransferEncoding(headerText))
                        {
                            if (offset < workingData.Length)
                            {
                                await destination.WriteAsync(
                                    workingData.AsMemory(offset, workingData.Length - offset), cancellationToken);
                            }
                            await destination.FlushAsync(cancellationToken);
                            fallbackToRawCopy = true;
                            offset = workingData.Length;
                            break;
                        }

                        bodyBytesRemaining = ParseContentLength(headerText);
                        if (bodyBytesRemaining > 0)
                        {
                            parsingHeaders = false;
                        }

                        await destination.FlushAsync(cancellationToken);
                    }
                    else
                    {
                        var available = workingData.Length - offset;
                        var toWrite = (int)Math.Min(available, bodyBytesRemaining);
                        await destination.WriteAsync(
                            workingData.AsMemory(offset, toWrite), cancellationToken);
                        await destination.FlushAsync(cancellationToken);
                        bodyBytesRemaining -= toWrite;
                        offset += toWrite;

                        if (bodyBytesRemaining <= 0)
                        {
                            parsingHeaders = true;
                        }
                    }
                }

                if (offset < workingData.Length && !fallbackToRawCopy)
                {
                    pendingData = new byte[workingData.Length - offset];
                    Buffer.BlockCopy(workingData, offset, pendingData, 0, pendingData.Length);
                }
            }

            if (pendingData.Length > 0)
            {
                await destination.WriteAsync(pendingData, cancellationToken);
                await destination.FlushAsync(cancellationToken);
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
        {
            _logger.LogDebug("upstream->client stream ended for {Host}: {Message}", host, ex.Message);
        }
    }

    // ─── Audit helpers ──────────────────────────────────────────────────

    private void LogAuditMessage(
        string host, string direction, int messageNum, string headerText,
        Dictionary<string, string>? injectedHeaders)
    {
        var lines = headerText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return;

        // First line is request-line or status-line
        _logger.LogInformation("[AUDIT] {Direction} #{Num} {Host} | {StatusLine}",
            direction, messageNum, host, lines[0]);

        for (var i = 1; i < lines.Length; i++)
        {
            var colonIdx = lines[i].IndexOf(':');
            if (colonIdx <= 0) continue;

            var name = lines[i][..colonIdx].Trim();
            var value = lines[i][(colonIdx + 1)..].Trim();
            _logger.LogInformation("[AUDIT]   {Name}: {Value}", name, RedactIfSensitive(name, value));
        }

        if (injectedHeaders is { Count: > 0 })
        {
            foreach (var (name, value) in injectedHeaders)
            {
                _logger.LogInformation("[AUDIT]   + INJECTED {Name}: {Value}",
                    name, RedactIfSensitive(name, value));
            }
        }
    }

    internal static string RedactIfSensitive(string headerName, string headerValue)
    {
        if (!SensitiveHeaders.Contains(headerName))
            return headerValue;

        // Preserve the auth scheme (e.g. "token", "Basic", "Bearer") but redact the credential
        var spaceIdx = headerValue.IndexOf(' ');
        if (spaceIdx > 0)
            return headerValue[..(spaceIdx + 1)] + "[REDACTED]";

        return "[REDACTED]";
    }

    // ─── Header manipulation ───────────────────────────────────────────

    internal static string StripHeaders(string headerText, IEnumerable<string> headerNames)
    {
        var namesToStrip = new HashSet<string>(headerNames, StringComparer.OrdinalIgnoreCase);
        var lines = headerText.Split("\r\n");
        var sb = new StringBuilder();

        foreach (var line in lines)
        {
            var colonIdx = line.IndexOf(':');
            if (colonIdx > 0)
            {
                var name = line[..colonIdx].Trim();
                if (namesToStrip.Contains(name))
                    continue;
            }

            if (sb.Length > 0)
                sb.Append("\r\n");
            sb.Append(line);
        }

        return sb.ToString();
    }

    // ─── Parsing helpers ────────────────────────────────────────────────

    internal static int FindSequence(byte[] haystack, byte[] needle, int startIndex = 0)
    {
        for (var i = startIndex; i <= haystack.Length - needle.Length; i++)
        {
            if (haystack.AsSpan(i, needle.Length).SequenceEqual(needle))
                return i;
        }
        return -1;
    }

    internal static long ParseContentLength(string headerText)
    {
        foreach (var line in headerText.Split("\r\n"))
        {
            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
            {
                var valueStr = line[(line.IndexOf(':') + 1)..].Trim();
                if (long.TryParse(valueStr, out var length))
                    return length;
            }
        }
        return 0;
    }

    internal static bool HasChunkedTransferEncoding(string headerText)
    {
        foreach (var line in headerText.Split("\r\n"))
        {
            if (line.StartsWith("Transfer-Encoding:", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("chunked", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
