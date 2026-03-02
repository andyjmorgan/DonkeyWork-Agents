using CodeSandbox.Manager.Contracts.Grpc;
using CodeSandbox.Manager.Services.Mcp;
using Grpc.Core;
using GrpcModels = CodeSandbox.Manager.Contracts.Grpc;
using Models = CodeSandbox.Manager.Models;

namespace CodeSandbox.Manager.Services.Grpc;

public class McpManagerGrpcService : McpManagerService.McpManagerServiceBase
{
    private readonly IMcpContainerService _mcpService;
    private readonly ILogger<McpManagerGrpcService> _logger;

    public McpManagerGrpcService(IMcpContainerService mcpService, ILogger<McpManagerGrpcService> logger)
    {
        _mcpService = mcpService;
        _logger = logger;
    }

    public override async Task CreateMcpServer(
        GrpcModels.CreateMcpServerRequest request,
        IServerStreamWriter<ContainerEvent> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC CreateMcpServer: UserId={UserId}, ConfigId={ConfigId}",
            request.UserId, request.McpServerConfigId);

        var serviceRequest = new Models.CreateMcpServerRequest
        {
            UserId = request.UserId,
            McpServerConfigId = request.McpServerConfigId,
            Command = request.HasCommand ? request.Command : null,
            Arguments = request.Arguments.ToArray(),
            PreExecScripts = request.PreExecScripts.ToArray(),
            TimeoutSeconds = request.TimeoutSeconds > 0 ? request.TimeoutSeconds : 30,
            EnvironmentVariables = request.EnvironmentVariables.Count > 0
                ? new Dictionary<string, string>(request.EnvironmentVariables)
                : null,
            WorkingDirectory = request.HasWorkingDirectory ? request.WorkingDirectory : null,
        };

        await foreach (var evt in _mcpService.CreateMcpServerAsync(serviceRequest, context.CancellationToken))
        {
            await responseStream.WriteAsync(MapContainerEvent(evt), context.CancellationToken);
        }
    }

    public override async Task<GrpcModels.McpServerInfo> FindMcpServer(
        FindMcpServerRequest request,
        ServerCallContext context)
    {
        var result = await _mcpService.FindMcpServerAsync(request.UserId, request.McpServerConfigId, context.CancellationToken);
        if (result is null)
            throw new RpcException(new Status(StatusCode.NotFound, "MCP server not found"));

        return MapMcpServerInfo(result);
    }

    public override async Task<ListMcpServersResponse> ListMcpServers(
        ListMcpServersRequest request,
        ServerCallContext context)
    {
        var servers = await _mcpService.ListMcpServersAsync(context.CancellationToken);
        var response = new ListMcpServersResponse();
        response.Servers.AddRange(servers.Select(MapMcpServerInfo));
        return response;
    }

    public override async Task<GrpcModels.McpServerInfo> GetMcpServer(
        GetMcpServerRequest request,
        ServerCallContext context)
    {
        var result = await _mcpService.GetMcpServerAsync(request.PodName, context.CancellationToken);
        if (result is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"MCP server '{request.PodName}' not found"));

        return MapMcpServerInfo(result);
    }

    public override async Task<GrpcModels.DeleteSandboxResponse> DeleteMcpServer(
        DeleteMcpServerRequest request,
        ServerCallContext context)
    {
        var result = await _mcpService.DeleteMcpServerAsync(request.PodName, context.CancellationToken);
        return new GrpcModels.DeleteSandboxResponse
        {
            Success = result.Success,
            Message = result.Message,
            PodName = result.PodName,
        };
    }

    public override async Task StartMcpProcess(
        StartMcpProcessRequest request,
        IServerStreamWriter<GrpcModels.McpStartEvent> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC StartMcpProcess: PodName={PodName}, Command={Command}",
            request.PodName, request.Command);

        var serviceRequest = new Models.McpStartRequest
        {
            Command = request.Command,
            Arguments = request.Arguments.ToArray(),
            PreExecScripts = request.PreExecScripts.ToArray(),
            TimeoutSeconds = request.TimeoutSeconds > 0 ? request.TimeoutSeconds : 30,
            WorkingDirectory = request.HasWorkingDirectory ? request.WorkingDirectory : null,
        };

        await foreach (var evt in _mcpService.StartMcpProcessAsync(request.PodName, serviceRequest, context.CancellationToken))
        {
            await responseStream.WriteAsync(new GrpcModels.McpStartEvent
            {
                EventType = evt.EventType,
                Message = evt.Message,
                Error = evt.Error ?? "",
            }, context.CancellationToken);
        }
    }

    public override async Task<GrpcModels.ProxyMcpResponse> ProxyMcpRequest(
        ProxyMcpRequestMessage request,
        ServerCallContext context)
    {
        var timeoutSeconds = request.TimeoutSeconds > 0 ? request.TimeoutSeconds : 30;
        var result = await _mcpService.ProxyMcpRequestAsync(
            request.PodName, request.Body, timeoutSeconds, context.CancellationToken);

        return new GrpcModels.ProxyMcpResponse
        {
            Body = result.Body,
            IsNotification = result.IsNotification,
        };
    }

    public override async Task<GrpcModels.McpStatusResponse> GetMcpStatus(
        GetMcpStatusRequest request,
        ServerCallContext context)
    {
        var result = await _mcpService.GetMcpStatusAsync(request.PodName, context.CancellationToken);
        return new GrpcModels.McpStatusResponse
        {
            State = result.State,
            Error = result.Error ?? "",
            StartedAt = result.StartedAt?.ToString("O") ?? "",
            LastRequestAt = result.LastRequestAt?.ToString("O") ?? "",
        };
    }

    public override async Task<GrpcModels.StopMcpProcessResponse> StopMcpProcess(
        StopMcpProcessRequest request,
        ServerCallContext context)
    {
        await _mcpService.StopMcpProcessAsync(request.PodName, context.CancellationToken);
        return new GrpcModels.StopMcpProcessResponse { Success = true };
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
