using DonkeyWork.Agents.Identity.Contracts.Services;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Credentials.Api.Interceptors;

public class InternalGrpcInterceptor : Interceptor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InternalGrpcInterceptor> _logger;

    public InternalGrpcInterceptor(
        IServiceProvider serviceProvider,
        ILogger<InternalGrpcInterceptor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        SetIdentityFromMetadata(context);
        return await continuation(request, context);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        SetIdentityFromMetadata(context);
        await continuation(request, responseStream, context);
    }

    private void SetIdentityFromMetadata(ServerCallContext context)
    {
        var userIdHeader = context.RequestHeaders.GetValue("x-user-id");
        if (string.IsNullOrEmpty(userIdHeader) || !Guid.TryParse(userIdHeader, out var userId))
        {
            _logger.LogWarning("gRPC request missing or invalid x-user-id header");
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Missing or invalid x-user-id header"));
        }

        var identityContext = context.GetHttpContext().RequestServices.GetRequiredService<IIdentityContext>();
        identityContext.SetIdentity(userId);

        _logger.LogDebug("gRPC identity set for user {UserId}", userId);
    }
}
