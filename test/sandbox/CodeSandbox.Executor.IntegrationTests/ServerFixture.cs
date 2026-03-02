using System.Diagnostics;
using System.Net;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Grpc.Net.Client;
using Xunit;

namespace CodeSandbox.Executor.IntegrationTests;

public class ServerFixture : IAsyncLifetime
{
    private IContainer? _container;
    private const int ServerPort = 8666;

    public string ServerUrl { get; private set; } = string.Empty;
    public GrpcChannel GrpcChannel { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (!await ImageExistsAsync("codesandbox-executor:test"))
        {
            var solutionDir = GetSolutionDirectory();
            var sandboxDir = new CommonDirectoryPath(Path.Combine(solutionDir.DirectoryPath, "src", "sandbox"));
            var imageFuture = new ImageFromDockerfileBuilder()
                .WithDockerfileDirectory(sandboxDir, string.Empty)
                .WithDockerfile("CodeSandbox.Executor/Dockerfile")
                .WithName("codesandbox-executor:test")
                .WithCleanUp(true)
                .Build();

            await imageFuture.CreateAsync();
        }

        _container = new ContainerBuilder()
            .WithImage("codesandbox-executor:test")
            .WithPortBinding(ServerPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(ServerPort))
            .Build();

        await _container.StartAsync();

        var mappedPort = _container.GetMappedPublicPort(ServerPort);
        ServerUrl = $"http://localhost:{mappedPort}";
        GrpcChannel = GrpcChannel.ForAddress(ServerUrl);

        await Task.Delay(2000);
        await WaitForServerHealthAsync();
    }

    private async Task WaitForServerHealthAsync()
    {
        var maxRetries = 30;
        var retryDelay = TimeSpan.FromSeconds(1);

        using var handler = new SocketsHttpHandler();
        using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                // Executor uses HTTP/2-only Kestrel, so health check must use HTTP/2
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"http://localhost:{_container!.GetMappedPublicPort(ServerPort)}/healthz")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                };

                var response = await httpClient.SendAsync(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch
            {
                // Server not ready yet
            }

            if (i < maxRetries - 1)
            {
                await Task.Delay(retryDelay);
            }
        }
    }

    public async Task DisposeAsync()
    {
        GrpcChannel?.Dispose();

        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    private static async Task<bool> ImageExistsAsync(string imageName)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"image inspect {imageName}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static CommonDirectoryPath GetSolutionDirectory()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory != null)
        {
            if (directory.GetFiles("*.sln").Length > 0 ||
                directory.GetFiles("*.slnx").Length > 0)
            {
                return new CommonDirectoryPath(directory.FullName);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find solution directory (no .sln or .slnx file found)");
    }
}
