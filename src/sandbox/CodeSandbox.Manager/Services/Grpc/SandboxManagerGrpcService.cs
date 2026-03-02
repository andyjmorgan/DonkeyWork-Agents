using CodeSandbox.Manager.Contracts.Grpc;
using CodeSandbox.Manager.Services.Container;
using Grpc.Core;
using GrpcModels = CodeSandbox.Manager.Contracts.Grpc;
using Models = CodeSandbox.Manager.Models;

namespace CodeSandbox.Manager.Services.Grpc;

public class SandboxManagerGrpcService : SandboxManagerService.SandboxManagerServiceBase
{
    private readonly ISandboxService _sandboxService;
    private readonly ILogger<SandboxManagerGrpcService> _logger;

    public SandboxManagerGrpcService(ISandboxService sandboxService, ILogger<SandboxManagerGrpcService> logger)
    {
        _sandboxService = sandboxService;
        _logger = logger;
    }

    public override async Task CreateSandbox(
        GrpcModels.CreateSandboxRequest request,
        IServerStreamWriter<ContainerEvent> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC CreateSandbox: UserId={UserId}, ConversationId={ConversationId}",
            request.UserId, request.ConversationId);

        var serviceRequest = new Models.CreateSandboxRequest
        {
            UserId = request.UserId,
            ConversationId = request.ConversationId,
            EnvironmentVariables = request.EnvironmentVariables.Count > 0
                ? new Dictionary<string, string>(request.EnvironmentVariables)
                : null,
        };

        if (request.Resources != null)
        {
            serviceRequest.Resources = new Models.ResourceRequirements
            {
                Requests = request.Resources.Requests != null ? new Models.ResourceValues
                {
                    MemoryMi = request.Resources.Requests.HasMemoryMi ? request.Resources.Requests.MemoryMi : null,
                    CpuMillicores = request.Resources.Requests.HasCpuMillicores ? request.Resources.Requests.CpuMillicores : null,
                } : null,
                Limits = request.Resources.Limits != null ? new Models.ResourceValues
                {
                    MemoryMi = request.Resources.Limits.HasMemoryMi ? request.Resources.Limits.MemoryMi : null,
                    CpuMillicores = request.Resources.Limits.HasCpuMillicores ? request.Resources.Limits.CpuMillicores : null,
                } : null,
            };
        }

        await foreach (var evt in _sandboxService.CreateSandboxAsync(serviceRequest, context.CancellationToken))
        {
            await responseStream.WriteAsync(MapContainerEvent(evt), context.CancellationToken);
        }
    }

    public override async Task<GrpcModels.SandboxInfo> FindSandbox(
        FindSandboxRequest request,
        ServerCallContext context)
    {
        var result = await _sandboxService.FindSandboxAsync(request.UserId, request.ConversationId, context.CancellationToken);
        if (result is null)
            throw new RpcException(new Status(StatusCode.NotFound, "Sandbox not found"));

        return MapSandboxInfo(result);
    }

    public override async Task<ListSandboxesResponse> ListSandboxes(
        ListSandboxesRequest request,
        ServerCallContext context)
    {
        var sandboxes = await _sandboxService.ListContainersAsync(context.CancellationToken);
        var response = new ListSandboxesResponse();
        response.Sandboxes.AddRange(sandboxes.Select(MapSandboxInfo));
        return response;
    }

    public override async Task<GrpcModels.SandboxInfo> GetSandbox(
        GetSandboxRequest request,
        ServerCallContext context)
    {
        var result = await _sandboxService.GetContainerAsync(request.PodName, context.CancellationToken);
        if (result is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Sandbox '{request.PodName}' not found"));

        return MapSandboxInfo(result);
    }

    public override async Task<GrpcModels.DeleteSandboxResponse> DeleteSandbox(
        DeleteSandboxRequest request,
        ServerCallContext context)
    {
        var result = await _sandboxService.DeleteContainerAsync(request.PodName, context.CancellationToken);
        return new GrpcModels.DeleteSandboxResponse
        {
            Success = result.Success,
            Message = result.Message,
            PodName = result.PodName,
        };
    }

    public override async Task<GrpcModels.DeleteAllSandboxesResponse> DeleteAllSandboxes(
        DeleteAllSandboxesRequest request,
        ServerCallContext context)
    {
        var result = await _sandboxService.DeleteAllContainersAsync(context.CancellationToken);
        var response = new GrpcModels.DeleteAllSandboxesResponse
        {
            DeletedCount = result.DeletedCount,
            FailedCount = result.FailedCount,
        };
        response.DeletedPods.AddRange(result.DeletedPods);
        response.FailedPods.AddRange(result.FailedPods);
        return response;
    }

    public override async Task ExecuteCommand(
        ExecuteCommandRequest request,
        IServerStreamWriter<GrpcModels.ExecuteEvent> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC ExecuteCommand: SandboxId={SandboxId}, Command={Command}",
            request.SandboxId, request.Command);

        // Update last activity
        _ = _sandboxService.UpdateLastActivityAsync(request.SandboxId, context.CancellationToken);

        var podIp = await _sandboxService.GetPodIpAsync(request.SandboxId, context.CancellationToken);

        using var channel = global::Grpc.Net.Client.GrpcChannel.ForAddress($"http://{podIp}:8666");
        var client = new CodeSandbox.Contracts.Grpc.Executor.ExecutorService.ExecutorServiceClient(channel);

        var grpcRequest = new CodeSandbox.Contracts.Grpc.Executor.ExecuteRequest
        {
            Command = request.Command,
            TimeoutSeconds = request.TimeoutSeconds,
        };

        using var call = client.Execute(grpcRequest, cancellationToken: context.CancellationToken);
        await foreach (var evt in call.ResponseStream.ReadAllAsync(context.CancellationToken))
        {
            var managerEvent = new GrpcModels.ExecuteEvent
            {
                EventType = evt.EventType,
                Data = evt.Data,
                Pid = evt.Pid,
            };
            if (evt.HasExitCode)
                managerEvent.ExitCode = evt.ExitCode;
            if (evt.HasStream)
                managerEvent.Stream = evt.Stream;

            await responseStream.WriteAsync(managerEvent, context.CancellationToken);
        }
    }

    private static ContainerEvent MapContainerEvent(Models.ContainerCreationEvent evt)
    {
        var grpcEvent = new ContainerEvent
        {
            EventType = evt.EventType,
            PodName = evt.PodName,
        };

        switch (evt)
        {
            case Models.ContainerCreatedEvent created:
                grpcEvent.Phase = created.Phase;
                grpcEvent.Message = $"Container created in phase: {created.Phase}";
                break;
            case Models.ContainerWaitingEvent waiting:
                grpcEvent.AttemptNumber = waiting.AttemptNumber;
                grpcEvent.Phase = waiting.Phase;
                grpcEvent.Message = waiting.Message;
                break;
            case Models.ContainerReadyEvent ready:
                grpcEvent.ElapsedSeconds = ready.ElapsedSeconds;
                grpcEvent.Message = "Container ready";
                if (ready.ContainerInfo != null)
                    grpcEvent.SandboxInfo = MapSandboxInfo(ready.ContainerInfo);
                break;
            case Models.ContainerFailedEvent failed:
                grpcEvent.Reason = failed.Reason;
                grpcEvent.Message = failed.Reason;
                break;
            case Models.McpServerStartingEvent starting:
                grpcEvent.Message = starting.Message;
                break;
            case Models.McpServerStartedEvent started:
                grpcEvent.ElapsedSeconds = started.ElapsedSeconds;
                grpcEvent.Message = "MCP server started";
                if (started.ServerInfo != null)
                    grpcEvent.McpServerInfo = MapMcpServerInfo(started.ServerInfo);
                break;
            case Models.McpServerStartFailedEvent startFailed:
                grpcEvent.Reason = startFailed.Reason;
                grpcEvent.Message = startFailed.Reason;
                break;
        }

        return grpcEvent;
    }

    private static GrpcModels.SandboxInfo MapSandboxInfo(Models.SandboxInfo info)
    {
        var grpc = new GrpcModels.SandboxInfo
        {
            Name = info.Name,
            Phase = info.Phase,
            IsReady = info.IsReady,
        };
        if (info.CreatedAt.HasValue) grpc.CreatedAt = info.CreatedAt.Value.ToString("O");
        if (info.LastActivity.HasValue) grpc.LastActivity = info.LastActivity.Value.ToString("O");
        if (info.NodeName != null) grpc.NodeName = info.NodeName;
        if (info.PodIP != null) grpc.PodIp = info.PodIP;
        if (info.Labels != null) grpc.Labels.Add(info.Labels);
        if (info.Image != null) grpc.Image = info.Image;
        return grpc;
    }

    private static GrpcModels.McpServerInfo MapMcpServerInfo(Models.McpServerInfo info)
    {
        var grpc = new GrpcModels.McpServerInfo
        {
            Name = info.Name,
            Phase = info.Phase,
            IsReady = info.IsReady,
            McpStatus = info.McpStatus.ToString(),
        };
        if (info.CreatedAt.HasValue) grpc.CreatedAt = info.CreatedAt.Value.ToString("O");
        if (info.LastActivity.HasValue) grpc.LastActivity = info.LastActivity.Value.ToString("O");
        if (info.NodeName != null) grpc.NodeName = info.NodeName;
        if (info.PodIP != null) grpc.PodIp = info.PodIP;
        if (info.Labels != null) grpc.Labels.Add(info.Labels);
        if (info.Image != null) grpc.Image = info.Image;
        if (info.LaunchCommand != null) grpc.LaunchCommand = info.LaunchCommand;
        return grpc;
    }
}
