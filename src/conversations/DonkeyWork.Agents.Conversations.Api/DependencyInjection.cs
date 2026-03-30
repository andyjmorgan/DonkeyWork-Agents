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
        services.AddOptions<ImageUploadOptions>()
            .BindConfiguration("Conversations:ImageUpload")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ImageValidationOptions>()
            .Configure<Microsoft.Extensions.Options.IOptions<ImageUploadOptions>>((validationOptions, uploadOptions) =>
            {
                validationOptions.MaxFileSizeBytes = uploadOptions.Value.MaxFileSizeBytes;
                validationOptions.AllowedMimeTypes = uploadOptions.Value.AllowedMimeTypes;
            });

        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IConversationMetadataService, ConversationMetadataService>();
        services.AddScoped<IImageValidationService, ImageValidationService>();

        return services;
    }
}
