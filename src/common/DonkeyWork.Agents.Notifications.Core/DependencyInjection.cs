using DonkeyWork.Agents.Notifications.Contracts.Interfaces;
using DonkeyWork.Agents.Notifications.Core.Hubs;
using DonkeyWork.Agents.Notifications.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Notifications.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsCore(this IServiceCollection services)
    {
        services.AddSignalR();

        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }

    public static WebApplication MapNotifications(this WebApplication app, string path = "/hubs/notifications")
    {
        app.MapHub<NotificationHub>(path);
        return app;
    }
}
