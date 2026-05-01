using System.Diagnostics;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace CodeSandbox.McpServer.IntegrationTests;

public class ServerFixture : IAsyncLifetime
{
    private const string BaseImageName = "codesandbox-executor-base:latest";
    private const string ImageName = "codesandbox-executor:test";
    private const int ServerPort = 8666;

    private IContainer? _container;

    public string ServerUrl { get; private set; } = string.Empty;
    public HttpClient HttpClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var solutionDir = GetSolutionDirectory();
        var executorDir = Path.Combine(solutionDir, "src", "sandbox", "CodeSandbox.Executor");
        var sandboxDir = Path.Combine(solutionDir, "src", "sandbox");

        if (!await DockerImageExistsAsync(BaseImageName))
        {
            await BuildDockerImageAsync(
                Path.Combine(executorDir, "Dockerfile.base"),
                executorDir,
                BaseImageName);
        }

        if (!await DockerImageExistsAsync(ImageName))
        {
            await BuildDockerImageAsync(
                Path.Combine(executorDir, "Dockerfile"),
                sandboxDir,
                ImageName);
        }

        _container = new ContainerBuilder()
            .WithImage(ImageName)
            .WithPortBinding(ServerPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(ServerPort))
            .Build();

        await _container.StartAsync();

        var mappedPort = _container.GetMappedPublicPort(ServerPort);
        ServerUrl = $"http://localhost:{mappedPort}";
        HttpClient = new HttpClient { BaseAddress = new Uri(ServerUrl), Timeout = TimeSpan.FromMinutes(2) };

        await Task.Delay(2000);
        await WaitForServerHealthAsync();
    }

    private static async Task<bool> DockerImageExistsAsync(string imageName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"image inspect {imageName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process == null) return false;
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task BuildDockerImageAsync(string dockerfilePath, string contextDir, string imageName)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            ArgumentList =
            {
                "build",
                "--platform", "linux/amd64",
                "-f", dockerfilePath,
                "-t", imageName,
                contextDir,
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start docker build for {imageName}");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"docker build failed for {imageName} (exit {process.ExitCode}):\n{stderr}\n{stdout}");
        }
    }

    private async Task WaitForServerHealthAsync()
    {
        var maxRetries = 30;
        var retryDelay = TimeSpan.FromSeconds(1);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await HttpClient.GetAsync("/healthz");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
            }

            if (i < maxRetries - 1)
            {
                await Task.Delay(retryDelay);
            }
        }
    }

    public async Task DisposeAsync()
    {
        HttpClient?.Dispose();

        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    private static string GetSolutionDirectory()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory != null)
        {
            if (directory.GetFiles("*.sln").Length > 0 ||
                directory.GetFiles("*.slnx").Length > 0)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find solution directory (no .sln or .slnx file found)");
    }
}
