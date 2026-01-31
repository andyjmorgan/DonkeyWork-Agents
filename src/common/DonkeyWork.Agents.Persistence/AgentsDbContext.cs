using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence.Entities;
using DonkeyWork.Agents.Persistence.Entities.Agents;
using DonkeyWork.Agents.Persistence.Entities.Credentials;
using DonkeyWork.Agents.Persistence.Entities.Projects;
using DonkeyWork.Agents.Persistence.Entities.Storage;
using Microsoft.EntityFrameworkCore;

namespace DonkeyWork.Agents.Persistence;

public class AgentsDbContext : DbContext
{
    private readonly IIdentityContext? _identityContext;

    public AgentsDbContext(DbContextOptions<AgentsDbContext> options)
        : base(options)
    {
    }

    public AgentsDbContext(DbContextOptions<AgentsDbContext> options, IIdentityContext identityContext)
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

    // Storage module
    public DbSet<StoredFileEntity> StoredFiles => Set<StoredFileEntity>();
    public DbSet<FileShareEntity> FileShares => Set<FileShareEntity>();

    // Agents module
    public DbSet<AgentEntity> Agents => Set<AgentEntity>();
    public DbSet<AgentVersionEntity> AgentVersions => Set<AgentVersionEntity>();
    public DbSet<AgentVersionCredentialMappingEntity> AgentVersionCredentialMappings => Set<AgentVersionCredentialMappingEntity>();
    public DbSet<AgentExecutionEntity> AgentExecutions => Set<AgentExecutionEntity>();
    public DbSet<AgentNodeExecutionEntity> AgentNodeExecutions => Set<AgentNodeExecutionEntity>();
    public DbSet<AgentExecutionLogEntity> AgentExecutionLogs => Set<AgentExecutionLogEntity>();

    // Projects module
    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<MilestoneEntity> Milestones => Set<MilestoneEntity>();
    public DbSet<TodoEntity> Todos => Set<TodoEntity>();
    public DbSet<NoteEntity> Notes => Set<NoteEntity>();
    public DbSet<ProjectTagEntity> ProjectTags => Set<ProjectTagEntity>();
    public DbSet<MilestoneTagEntity> MilestoneTags => Set<MilestoneTagEntity>();
    public DbSet<TodoTagEntity> TodoTags => Set<TodoTagEntity>();
    public DbSet<NoteTagEntity> NoteTags => Set<NoteTagEntity>();
    public DbSet<ProjectFileReferenceEntity> ProjectFileReferences => Set<ProjectFileReferenceEntity>();
    public DbSet<MilestoneFileReferenceEntity> MilestoneFileReferences => Set<MilestoneFileReferenceEntity>();

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
