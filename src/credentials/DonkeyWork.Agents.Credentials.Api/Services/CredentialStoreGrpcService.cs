using CodeSandbox.Contracts.Grpc.Credentials;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Credentials.Api.Services;

public class CredentialStoreGrpcService : CredentialStoreService.CredentialStoreServiceBase
{
    private readonly ISandboxCredentialMappingService _mappingService;
    private readonly ILogger<CredentialStoreGrpcService> _logger;

    public CredentialStoreGrpcService(
        ISandboxCredentialMappingService mappingService,
        ILogger<CredentialStoreGrpcService> logger)
    {
        _mappingService = mappingService;
        _logger = logger;
    }

    public override async Task<GetDomainCredentialsResponse> GetDomainCredentials(
        GetDomainCredentialsRequest request,
        ServerCallContext context)
    {
        _logger.LogDebug("GetDomainCredentials request for domain: {Domain}", request.BaseDomain);

        var resolved = await _mappingService.ResolveForDomainAsync(request.BaseDomain, context.CancellationToken);

        if (resolved is null)
        {
            _logger.LogDebug("No credentials found for domain: {Domain}", request.BaseDomain);
            return new GetDomainCredentialsResponse { Found = false };
        }

        var response = new GetDomainCredentialsResponse { Found = true };
        response.Headers.Add(resolved.Headers);

        _logger.LogDebug("Resolved {Count} header(s) for domain: {Domain}", resolved.Headers.Count, request.BaseDomain);

        return response;
    }
}
