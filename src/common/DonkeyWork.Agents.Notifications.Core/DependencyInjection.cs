using DonkeyWork.Agents.Notifications.Contracts.Interfaces;
using DonkeyWork.Agents.Notifications.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Notifications.Core;

/// <summary>
/// Dependency injection extensions for the Notifications Core library.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the notification services and SignalR to the service collection.
    /// </summary>
    public static IServiceCollection AddNotificationsCore(this IServiceCollection services)
    {
        // Add SignalR for real-time notifications
        services.AddSignalR();

        // Add notification service
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
