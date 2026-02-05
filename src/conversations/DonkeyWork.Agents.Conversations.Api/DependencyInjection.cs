using DonkeyWork.Agents.Conversations.Api.Options;
using DonkeyWork.Agents.Conversations.Contracts.Services;
using DonkeyWork.Agents.Conversations.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Conversations.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddConversationsApi(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.AddOptions<ImageUploadOptions>()
            .BindConfiguration("Conversations:ImageUpload")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Map ImageUploadOptions to ImageValidationOptions (Core uses its own options class)
        services.AddOptions<ImageValidationOptions>()
            .Configure<Microsoft.Extensions.Options.IOptions<ImageUploadOptions>>((validationOptions, uploadOptions) =>
            {
                validationOptions.MaxFileSizeBytes = uploadOptions.Value.MaxFileSizeBytes;
                validationOptions.AllowedMimeTypes = uploadOptions.Value.AllowedMimeTypes;
            });

        // Register services
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IImageValidationService, ImageValidationService>();

        return services;
    }
}
