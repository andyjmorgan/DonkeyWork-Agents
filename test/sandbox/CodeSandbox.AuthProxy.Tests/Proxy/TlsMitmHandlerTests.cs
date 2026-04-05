using System.Text;
using CodeSandbox.AuthProxy.Proxy;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSandbox.AuthProxy.Tests.Proxy;

public class TlsMitmHandlerTests
{
    private readonly TlsMitmHandler _handler;

    public TlsMitmHandlerTests()
    {
        var caCert = CertificateGenerator.GenerateEphemeralCa();
        var certGen = new CertificateGenerator(caCert, Mock.Of<ILogger<CertificateGenerator>>());
        _handler = new TlsMitmHandler(certGen, Mock.Of<ILogger<TlsMitmHandler>>());
    }

    #region StripHeaders Tests

    [Fact]
    public void StripHeaders_RemovesMatchingHeader()
    {
        var headers = "GET / HTTP/1.1\r\nHost: example.com\r\nAuthorization: Bearer old-token";

        var result = TlsMitmHandler.StripHeaders(headers, ["Authorization"]);

        Assert.Equal("GET / HTTP/1.1\r\nHost: example.com", result);
    }

    [Fact]
    public void StripHeaders_CaseInsensitiveMatch()
    {
        var headers = "GET / HTTP/1.1\r\nhost: example.com\r\nauthorization: Bearer old-token";

        var result = TlsMitmHandler.StripHeaders(headers, ["Authorization"]);

        Assert.Equal("GET / HTTP/1.1\r\nhost: example.com", result);
    }

    [Fact]
    public void StripHeaders_RemovesMultipleMatchingHeaders()
    {
        var headers = "GET / HTTP/1.1\r\nHost: example.com\r\nAuthorization: Bearer old\r\nX-Api-Key: secret";

        var result = TlsMitmHandler.StripHeaders(headers, ["Authorization", "X-Api-Key"]);

        Assert.Equal("GET / HTTP/1.1\r\nHost: example.com", result);
    }

    [Fact]
    public void StripHeaders_PreservesRequestLine()
    {
        var headers = "POST /api/data HTTP/1.1\r\nAuthorization: Bearer token";

        var result = TlsMitmHandler.StripHeaders(headers, ["Authorization"]);

        Assert.Equal("POST /api/data HTTP/1.1", result);
    }

    [Fact]
    public void StripHeaders_NoMatchReturnsOriginal()
    {
        var headers = "GET / HTTP/1.1\r\nHost: example.com\r\nAccept: */*";

        var result = TlsMitmHandler.StripHeaders(headers, ["Authorization"]);

        Assert.Equal(headers, result);
    }

    [Fact]
    public void StripHeaders_EmptyNamesToStripReturnsOriginal()
    {
        var headers = "GET / HTTP/1.1\r\nHost: example.com";

        var result = TlsMitmHandler.StripHeaders(headers, []);

        Assert.Equal(headers, result);
    }

    [Fact]
    public void StripHeaders_PreservesHeadersWithColonsInValue()
    {
        var headers = "GET / HTTP/1.1\r\nHost: example.com:8080\r\nAuthorization: Bearer token";

        var result = TlsMitmHandler.StripHeaders(headers, ["Authorization"]);

        Assert.Equal("GET / HTTP/1.1\r\nHost: example.com:8080", result);
    }

    #endregion

    #region RedactIfSensitive Tests

    [Fact]
    public void RedactIfSensitive_NonSensitiveHeaderReturnsValue()
    {
        var result = TlsMitmHandler.RedactIfSensitive("Content-Type", "application/json");

        Assert.Equal("application/json", result);
    }

    [Fact]
    public void RedactIfSensitive_AuthorizationRedactsCredential()
    {
        var result = TlsMitmHandler.RedactIfSensitive("Authorization", "Bearer my-secret-token");

        Assert.Equal("Bearer [REDACTED]", result);
    }

    [Fact]
    public void RedactIfSensitive_AuthorizationWithoutSchemeFullyRedacts()
    {
        var result = TlsMitmHandler.RedactIfSensitive("Authorization", "my-api-key");

        Assert.Equal("[REDACTED]", result);
    }

    [Fact]
    public void RedactIfSensitive_CaseInsensitiveHeaderName()
    {
        var result = TlsMitmHandler.RedactIfSensitive("authorization", "Bearer token");

        Assert.Equal("Bearer [REDACTED]", result);
    }

    [Theory]
    [InlineData("Cookie")]
    [InlineData("Set-Cookie")]
    [InlineData("X-Api-Key")]
    [InlineData("X-Auth-Token")]
    [InlineData("X-Access-Token")]
    [InlineData("Proxy-Authorization")]
    public void RedactIfSensitive_AllSensitiveHeadersAreRedacted(string headerName)
    {
        var result = TlsMitmHandler.RedactIfSensitive(headerName, "secret-value");

        Assert.Equal("[REDACTED]", result);
    }

    #endregion

    #region ParseContentLength Tests

    [Fact]
    public void ParseContentLength_ValidHeader_ReturnsLength()
    {
        var headers = "HTTP/1.1 200 OK\r\nContent-Length: 1234";

        var result = TlsMitmHandler.ParseContentLength(headers);

        Assert.Equal(1234, result);
    }

    [Fact]
    public void ParseContentLength_NoHeader_ReturnsZero()
    {
        var headers = "HTTP/1.1 200 OK\r\nContent-Type: text/html";

        var result = TlsMitmHandler.ParseContentLength(headers);

        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseContentLength_CaseInsensitive()
    {
        var headers = "HTTP/1.1 200 OK\r\ncontent-length: 42";

        var result = TlsMitmHandler.ParseContentLength(headers);

        Assert.Equal(42, result);
    }

    [Fact]
    public void ParseContentLength_InvalidValue_ReturnsZero()
    {
        var headers = "HTTP/1.1 200 OK\r\nContent-Length: not-a-number";

        var result = TlsMitmHandler.ParseContentLength(headers);

        Assert.Equal(0, result);
    }

    #endregion

    #region HasChunkedTransferEncoding Tests

    [Fact]
    public void HasChunkedTransferEncoding_Present_ReturnsTrue()
    {
        var headers = "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked";

        Assert.True(TlsMitmHandler.HasChunkedTransferEncoding(headers));
    }

    [Fact]
    public void HasChunkedTransferEncoding_Absent_ReturnsFalse()
    {
        var headers = "HTTP/1.1 200 OK\r\nContent-Length: 100";

        Assert.False(TlsMitmHandler.HasChunkedTransferEncoding(headers));
    }

    [Fact]
    public void HasChunkedTransferEncoding_CaseInsensitive()
    {
        var headers = "HTTP/1.1 200 OK\r\ntransfer-encoding: Chunked";

        Assert.True(TlsMitmHandler.HasChunkedTransferEncoding(headers));
    }

    #endregion

    #region FindSequence Tests

    [Fact]
    public void FindSequence_FoundAtStart_ReturnsZero()
    {
        var haystack = Encoding.ASCII.GetBytes("hello world");
        var needle = Encoding.ASCII.GetBytes("hello");

        Assert.Equal(0, TlsMitmHandler.FindSequence(haystack, needle));
    }

    [Fact]
    public void FindSequence_FoundInMiddle_ReturnsIndex()
    {
        var haystack = Encoding.ASCII.GetBytes("hello world");
        var needle = Encoding.ASCII.GetBytes("world");

        Assert.Equal(6, TlsMitmHandler.FindSequence(haystack, needle));
    }

    [Fact]
    public void FindSequence_NotFound_ReturnsNegativeOne()
    {
        var haystack = Encoding.ASCII.GetBytes("hello world");
        var needle = Encoding.ASCII.GetBytes("xyz");

        Assert.Equal(-1, TlsMitmHandler.FindSequence(haystack, needle));
    }

    [Fact]
    public void FindSequence_WithStartIndex_SkipsPriorOccurrences()
    {
        var haystack = Encoding.ASCII.GetBytes("abcabc");
        var needle = Encoding.ASCII.GetBytes("abc");

        Assert.Equal(3, TlsMitmHandler.FindSequence(haystack, needle, 1));
    }

    #endregion

    #region RelayRequestsAsync Tests

    [Fact]
    public async Task RelayRequestsAsync_InjectsHeadersAndReplacesExisting()
    {
        var request = "GET / HTTP/1.1\r\nHost: example.com\r\nAuthorization: Bearer old-token\r\n\r\n";
        var source = new MemoryStream(Encoding.ASCII.GetBytes(request));
        var destination = new MemoryStream();
        var headersToInject = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer new-token"
        };

        await _handler.RelayRequestsAsync(source, destination, "example.com", headersToInject, CancellationToken.None);

        var result = Encoding.ASCII.GetString(destination.ToArray());

        Assert.Contains("Authorization: Bearer new-token", result);
        Assert.DoesNotContain("Bearer old-token", result);
        Assert.Contains("Host: example.com", result);
    }

    [Fact]
    public async Task RelayRequestsAsync_InjectsNewHeaderWhenNotPresent()
    {
        var request = "GET / HTTP/1.1\r\nHost: example.com\r\n\r\n";
        var source = new MemoryStream(Encoding.ASCII.GetBytes(request));
        var destination = new MemoryStream();
        var headersToInject = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer injected-token"
        };

        await _handler.RelayRequestsAsync(source, destination, "example.com", headersToInject, CancellationToken.None);

        var result = Encoding.ASCII.GetString(destination.ToArray());

        Assert.Contains("Authorization: Bearer injected-token", result);
        Assert.Contains("Host: example.com", result);
    }

    [Fact]
    public async Task RelayRequestsAsync_NoInjection_PreservesOriginalHeaders()
    {
        var request = "GET / HTTP/1.1\r\nHost: example.com\r\nAuthorization: Bearer keep-me\r\n\r\n";
        var source = new MemoryStream(Encoding.ASCII.GetBytes(request));
        var destination = new MemoryStream();

        await _handler.RelayRequestsAsync(source, destination, "example.com", null, CancellationToken.None);

        var result = Encoding.ASCII.GetString(destination.ToArray());

        Assert.Contains("Authorization: Bearer keep-me", result);
        Assert.Contains("Host: example.com", result);
    }

    [Fact]
    public async Task RelayRequestsAsync_ReplacesMultipleHeaders()
    {
        var request = "GET / HTTP/1.1\r\nHost: example.com\r\nAuthorization: Bearer old\r\nX-Api-Key: old-key\r\n\r\n";
        var source = new MemoryStream(Encoding.ASCII.GetBytes(request));
        var destination = new MemoryStream();
        var headersToInject = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer new",
            ["X-Api-Key"] = "new-key"
        };

        await _handler.RelayRequestsAsync(source, destination, "example.com", headersToInject, CancellationToken.None);

        var result = Encoding.ASCII.GetString(destination.ToArray());

        Assert.Contains("Authorization: Bearer new", result);
        Assert.Contains("X-Api-Key: new-key", result);
        Assert.DoesNotContain("Bearer old", result);
        Assert.DoesNotContain("old-key", result);
    }

    [Fact]
    public async Task RelayRequestsAsync_ForwardsBodyCorrectly()
    {
        var body = "{\"data\":\"test\"}";
        var request = $"POST /api HTTP/1.1\r\nHost: example.com\r\nContent-Length: {body.Length}\r\n\r\n{body}";
        var source = new MemoryStream(Encoding.ASCII.GetBytes(request));
        var destination = new MemoryStream();

        await _handler.RelayRequestsAsync(source, destination, "example.com", null, CancellationToken.None);

        var result = Encoding.ASCII.GetString(destination.ToArray());

        Assert.Contains(body, result);
    }

    [Fact]
    public async Task RelayRequestsAsync_BodyWithInjection_PreservesBodyAndReplacesHeaders()
    {
        var body = "{\"key\":\"value\"}";
        var request = $"POST /api HTTP/1.1\r\nHost: example.com\r\nAuthorization: Bearer old\r\nContent-Length: {body.Length}\r\n\r\n{body}";
        var source = new MemoryStream(Encoding.ASCII.GetBytes(request));
        var destination = new MemoryStream();
        var headersToInject = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer new"
        };

        await _handler.RelayRequestsAsync(source, destination, "example.com", headersToInject, CancellationToken.None);

        var result = Encoding.ASCII.GetString(destination.ToArray());

        Assert.Contains("Authorization: Bearer new", result);
        Assert.DoesNotContain("Bearer old", result);
        Assert.Contains(body, result);
    }

    [Fact]
    public async Task RelayRequestsAsync_MultipleSequentialRequests_AllGetInjected()
    {
        var req1 = "GET /first HTTP/1.1\r\nHost: example.com\r\nAuthorization: Bearer old1\r\n\r\n";
        var req2 = "GET /second HTTP/1.1\r\nHost: example.com\r\nAuthorization: Bearer old2\r\n\r\n";
        var source = new MemoryStream(Encoding.ASCII.GetBytes(req1 + req2));
        var destination = new MemoryStream();
        var headersToInject = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer injected"
        };

        await _handler.RelayRequestsAsync(source, destination, "example.com", headersToInject, CancellationToken.None);

        var result = Encoding.ASCII.GetString(destination.ToArray());

        Assert.DoesNotContain("Bearer old1", result);
        Assert.DoesNotContain("Bearer old2", result);
        Assert.Contains("/first", result);
        Assert.Contains("/second", result);

        var count = 0;
        var idx = 0;
        while ((idx = result.IndexOf("Authorization: Bearer injected", idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += 1;
        }
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task RelayRequestsAsync_ChunkedTransferEncoding_FallsBackToRawCopy()
    {
        var request = "POST /stream HTTP/1.1\r\nHost: example.com\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello\r\n0\r\n\r\n";
        var source = new MemoryStream(Encoding.ASCII.GetBytes(request));
        var destination = new MemoryStream();

        await _handler.RelayRequestsAsync(source, destination, "example.com", null, CancellationToken.None);

        var result = Encoding.ASCII.GetString(destination.ToArray());

        Assert.Contains("Transfer-Encoding: chunked", result);
        Assert.Contains("hello", result);
    }

    [Fact]
    public async Task RelayRequestsAsync_EmptyDictionary_TreatedAsNoInjection()
    {
        var request = "GET / HTTP/1.1\r\nHost: example.com\r\nAuthorization: Bearer keep\r\n\r\n";
        var source = new MemoryStream(Encoding.ASCII.GetBytes(request));
        var destination = new MemoryStream();

        await _handler.RelayRequestsAsync(source, destination, "example.com", new Dictionary<string, string>(), CancellationToken.None);

        var result = Encoding.ASCII.GetString(destination.ToArray());

        Assert.Contains("Authorization: Bearer keep", result);
    }

    #endregion

    #region RelayResponsesAsync Tests

    [Fact]
    public async Task RelayResponsesAsync_ForwardsResponseHeaders()
    {
        var response = "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: 2\r\n\r\n{}";
        var source = new MemoryStream(Encoding.ASCII.GetBytes(response));
        var destination = new MemoryStream();

        await _handler.RelayResponsesAsync(source, destination, "example.com", CancellationToken.None);

        var result = Encoding.ASCII.GetString(destination.ToArray());

        Assert.Contains("HTTP/1.1 200 OK", result);
        Assert.Contains("Content-Type: application/json", result);
        Assert.Contains("{}", result);
    }

    [Fact]
    public async Task RelayResponsesAsync_ChunkedResponse_FallsBackToRawCopy()
    {
        var response = "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n4\r\ntest\r\n0\r\n\r\n";
        var source = new MemoryStream(Encoding.ASCII.GetBytes(response));
        var destination = new MemoryStream();

        await _handler.RelayResponsesAsync(source, destination, "example.com", CancellationToken.None);

        var result = Encoding.ASCII.GetString(destination.ToArray());

        Assert.Contains("HTTP/1.1 200 OK", result);
        Assert.Contains("test", result);
    }

    [Fact]
    public async Task RelayResponsesAsync_MultipleResponses_AllForwarded()
    {
        var resp1 = "HTTP/1.1 200 OK\r\nContent-Length: 5\r\n\r\nhello";
        var resp2 = "HTTP/1.1 404 Not Found\r\nContent-Length: 9\r\n\r\nnot found";
        var source = new MemoryStream(Encoding.ASCII.GetBytes(resp1 + resp2));
        var destination = new MemoryStream();

        await _handler.RelayResponsesAsync(source, destination, "example.com", CancellationToken.None);

        var result = Encoding.ASCII.GetString(destination.ToArray());

        Assert.Contains("200 OK", result);
        Assert.Contains("404 Not Found", result);
        Assert.Contains("hello", result);
        Assert.Contains("not found", result);
    }

    #endregion
}
