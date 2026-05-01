using System.Net;
using System.Text;
using System.Text.Json;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Executors;
using DonkeyWork.Agents.Orchestrations.Core.Services;
using Microsoft.Extensions.Logging;
using OrchestrationExecutionContext = DonkeyWork.Agents.Orchestrations.Core.Execution.ExecutionContext;

namespace DonkeyWork.Agents.Orchestrations.Core.Tests.Executors;

public class GeminiTextToSpeechNodeExecutorTests
{
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestCredentialId = Guid.NewGuid();

    private static readonly byte[] MinimalPcm = new byte[44];

    private static string BuildGeminiResponse()
    {
        var audioBase64 = Convert.ToBase64String(MinimalPcm);
        return $$"""
        {
          "candidates": [
            {
              "content": {
                "parts": [
                  {
                    "inlineData": {
                      "mimeType": "audio/pcm",
                      "data": "{{audioBase64}}"
                    }
                  }
                ],
                "role": "model"
              },
              "finishReason": "STOP"
            }
          ]
        }
        """;
    }

    private static (GeminiTextToSpeechNodeExecutor Executor, List<string> CapturedRequestBodies) BuildExecutor()
    {
        var capturedBodies = new List<string>();

        var fakeHandler = new FakeHttpMessageHandler(capturedBodies, BuildGeminiResponse);

        var httpClient = new HttpClient(fakeHandler)
        {
            Timeout = TimeSpan.FromSeconds(30),
        };

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var context = new OrchestrationExecutionContext();
        context.Hydrate(
            Guid.NewGuid(),
            TestUserId,
            ExecutionInterface.Direct,
            JsonDocument.Parse("{}").RootElement,
            JsonDocument.Parse("{\"type\":\"object\"}"));

        var credentialServiceMock = new Mock<IExternalApiKeyService>();
        credentialServiceMock
            .Setup(s => s.GetByIdAsync(TestUserId, TestCredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalApiKey
            {
                Id = TestCredentialId,
                UserId = TestUserId,
                Provider = ExternalApiKeyProvider.Google,
                Name = "test-key",
                Fields = new Dictionary<CredentialFieldType, string>
                {
                    [CredentialFieldType.ApiKey] = "fake-api-key",
                },
                CreatedAt = DateTimeOffset.UtcNow,
            });

        var templateRendererMock = new Mock<ITemplateRenderer>();
        templateRendererMock
            .Setup(r => r.RenderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string t, CancellationToken _) => t);

        var chunkerMock = new Mock<ITtsChunker>();
        chunkerMock
            .Setup(c => c.Chunk(It.IsAny<string>(), It.IsAny<ChunkerOptions>()))
            .Returns((string text, ChunkerOptions _) => new List<string> { text });

        var streamWriterMock = new Mock<IExecutionStreamWriter>();

        var executor = new GeminiTextToSpeechNodeExecutor(
            streamWriterMock.Object,
            context,
            credentialServiceMock.Object,
            templateRendererMock.Object,
            chunkerMock.Object,
            httpClientFactoryMock.Object,
            new Mock<ILogger<GeminiTextToSpeechNodeExecutor>>().Object);

        return (executor, capturedBodies);
    }

    #region Prompt Wrapping Tests

    [Fact]
    public async Task ExecuteAsync_NoInstructions_WrapsTextWithTtsInstruction()
    {
        var (executor, capturedBodies) = BuildExecutor();

        var config = new GeminiTextToSpeechNodeConfiguration
        {
            Name = "tts",
            Model = "gemini-2.5-flash-preview-tts",
            Voice = "Kore",
            Text = "Hello world",
            CredentialId = TestCredentialId,
            ResponseFormat = "pcm",
        };

        await executor.ExecuteAsync(Guid.NewGuid(), config, CancellationToken.None);

        Assert.Single(capturedBodies);
        Assert.Contains("Read aloud the following text exactly as written", capturedBodies[0]);
        Assert.Contains("Hello world", capturedBodies[0]);
    }

    [Fact]
    public async Task ExecuteAsync_WithInstructions_PrependsTtsWrapperAfterInstructions()
    {
        var (executor, capturedBodies) = BuildExecutor();

        var config = new GeminiTextToSpeechNodeConfiguration
        {
            Name = "tts",
            Model = "gemini-2.5-flash-preview-tts",
            Voice = "Kore",
            Text = "Hello world",
            Instructions = "Speak cheerfully.",
            CredentialId = TestCredentialId,
            ResponseFormat = "pcm",
        };

        await executor.ExecuteAsync(Guid.NewGuid(), config, CancellationToken.None);

        Assert.Single(capturedBodies);
        Assert.Contains("Speak cheerfully.", capturedBodies[0]);
        Assert.Contains("Read aloud the following text exactly as written", capturedBodies[0]);
        Assert.Contains("Hello world", capturedBodies[0]);
    }

    [Fact]
    public async Task ExecuteAsync_QuestionLikeInput_PromptAlwaysContainsTtsInstruction()
    {
        var (executor, capturedBodies) = BuildExecutor();

        var config = new GeminiTextToSpeechNodeConfiguration
        {
            Name = "tts",
            Model = "gemini-2.5-flash-preview-tts",
            Voice = "Kore",
            Text = "What is the capital of France?",
            CredentialId = TestCredentialId,
            ResponseFormat = "pcm",
        };

        await executor.ExecuteAsync(Guid.NewGuid(), config, CancellationToken.None);

        Assert.Single(capturedBodies);
        Assert.Contains("Read aloud the following text exactly as written", capturedBodies[0]);
    }

    #endregion

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<string> _capturedBodies;
        private readonly Func<string> _responseBodyFactory;

        public FakeHttpMessageHandler(List<string> capturedBodies, Func<string> responseBodyFactory)
        {
            _capturedBodies = capturedBodies;
            _responseBodyFactory = responseBodyFactory;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content != null)
            {
                var body = await request.Content.ReadAsStringAsync(cancellationToken);
                _capturedBodies.Add(body);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBodyFactory(), Encoding.UTF8, "application/json"),
            };
        }
    }
}
