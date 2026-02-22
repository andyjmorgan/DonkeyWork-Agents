using DonkeyWork.Agents.Projects.Contracts.Services;
using DonkeyWork.Agents.Projects.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Projects.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddProjectsApi(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IMilestoneService, MilestoneService>();
        services.AddScoped<ITaskItemService, TaskItemService>();
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<IResearchService, ResearchService>();

        return services;
    }
}
