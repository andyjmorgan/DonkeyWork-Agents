using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Credentials;

namespace DonkeyWork.Agents.Integration.Tests.Helpers;

public class TestDataSeeder
{
    private readonly AgentsDbContext _dbContext;

    public TestDataSeeder(AgentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    #region API Key Seeding

    public async Task<UserApiKeyEntity> SeedUserApiKeyAsync(
        Guid userId,
        string name,
        string? description = null)
    {
        var apiKey = new UserApiKeyEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = description ?? "Test API key",
            EncryptedKey = new byte[32]
        };

        _dbContext.UserApiKeys.Add(apiKey);
        await _dbContext.SaveChangesAsync();
        return apiKey;
    }

    #endregion

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
