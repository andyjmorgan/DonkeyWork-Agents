using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence.Entities;
using DonkeyWork.Agents.Persistence.Entities.Conversations;
using DonkeyWork.Agents.Persistence.Entities.Credentials;
using DonkeyWork.Agents.Persistence.Entities.Mcp;
using DonkeyWork.Agents.Persistence.Entities.Orchestrations;
using DonkeyWork.Agents.Persistence.Entities.Projects;
using DonkeyWork.Agents.Persistence.Entities.Research;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DonkeyWork.Agents.Persistence;

public class AgentsDbContext : DbContext, IDataProtectionKeyContext
{
    private readonly IIdentityContext? _identityContext;

    public AgentsDbContext(DbContextOptions<AgentsDbContext> options, IIdentityContext? identityContext = null)
        : base(options)
    {
        _identityContext = identityContext;
    }

    /// <summary>
    /// Current user ID for query filtering. Returns empty GUID if not authenticated.
    /// </summary>
    public Guid CurrentUserId => _identityContext?.UserId ?? Guid.Empty;

    // Credentials module
    public DbSet<ExternalApiKeyEntity> ExternalApiKeys => Set<ExternalApiKeyEntity>();
    public DbSet<OAuthTokenEntity> OAuthTokens => Set<OAuthTokenEntity>();
    public DbSet<UserApiKeyEntity> UserApiKeys => Set<UserApiKeyEntity>();
    public DbSet<OAuthProviderConfigEntity> OAuthProviderConfigs => Set<OAuthProviderConfigEntity>();
    public DbSet<OAuthStateEntity> OAuthStates => Set<OAuthStateEntity>();
    public DbSet<SandboxCredentialMappingEntity> SandboxCredentialMappings => Set<SandboxCredentialMappingEntity>();

    // Orchestrations module
    public DbSet<OrchestrationEntity> Orchestrations => Set<OrchestrationEntity>();
    public DbSet<OrchestrationVersionEntity> OrchestrationVersions => Set<OrchestrationVersionEntity>();
    public DbSet<OrchestrationVersionCredentialMappingEntity> OrchestrationVersionCredentialMappings => Set<OrchestrationVersionCredentialMappingEntity>();
    public DbSet<OrchestrationExecutionEntity> OrchestrationExecutions => Set<OrchestrationExecutionEntity>();
    public DbSet<OrchestrationNodeExecutionEntity> OrchestrationNodeExecutions => Set<OrchestrationNodeExecutionEntity>();
    public DbSet<OrchestrationExecutionLogEntity> OrchestrationExecutionLogs => Set<OrchestrationExecutionLogEntity>();

    // Projects module
    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<MilestoneEntity> Milestones => Set<MilestoneEntity>();
    public DbSet<TaskItemEntity> TaskItems => Set<TaskItemEntity>();
    public DbSet<NoteEntity> Notes => Set<NoteEntity>();
    public DbSet<ProjectTagEntity> ProjectTags => Set<ProjectTagEntity>();
    public DbSet<MilestoneTagEntity> MilestoneTags => Set<MilestoneTagEntity>();
    public DbSet<TaskItemTagEntity> TaskItemTags => Set<TaskItemTagEntity>();
    public DbSet<NoteTagEntity> NoteTags => Set<NoteTagEntity>();
    public DbSet<ProjectFileReferenceEntity> ProjectFileReferences => Set<ProjectFileReferenceEntity>();
    public DbSet<MilestoneFileReferenceEntity> MilestoneFileReferences => Set<MilestoneFileReferenceEntity>();

    // Research module
    public DbSet<ResearchEntity> Research => Set<ResearchEntity>();
    public DbSet<ResearchTagEntity> ResearchTags => Set<ResearchTagEntity>();

    // Conversations module
    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();
    public DbSet<ConversationMessageEntity> ConversationMessages => Set<ConversationMessageEntity>();

    // MCP module (system-level logging, no user scoping)
    public DbSet<McpToolInvocationLogEntity> McpToolInvocationLogs => Set<McpToolInvocationLogEntity>();

    // MCP server configurations module
    public DbSet<McpServerConfigurationEntity> McpServerConfigurations => Set<McpServerConfigurationEntity>();
    public DbSet<McpStdioConfigurationEntity> McpStdioConfigurations => Set<McpStdioConfigurationEntity>();
    public DbSet<McpHttpConfigurationEntity> McpHttpConfigurations => Set<McpHttpConfigurationEntity>();
    public DbSet<McpHttpOAuthConfigurationEntity> McpHttpOAuthConfigurations => Set<McpHttpOAuthConfigurationEntity>();
    public DbSet<McpHttpHeaderConfigurationEntity> McpHttpHeaderConfigurations => Set<McpHttpHeaderConfigurationEntity>();
    public DbSet<McpStdioEnvironmentVariableEntity> McpStdioEnvironmentVariables => Set<McpStdioEnvironmentVariableEntity>();

    // Data Protection keys (system-level, no user scoping)
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AgentsDbContext).Assembly);

        // Apply global query filter for user isolation on all entities inheriting from BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AgentsDbContext)
                    .GetMethod(nameof(ApplyUserFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(this, [modelBuilder]);
            }
        }
    }

    private void ApplyUserFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : BaseEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.UserId == CurrentUserId);
    }
}
