using Xunit;

namespace DonkeyWork.Agents.Identity.Core.Tests;

using DonkeyWork.Agents.Identity.Core.Services;

public class IdentityContextTests
{
    [Fact]
    public void SetIdentity_WithValidData_SetsAllProperties()
    {
        // Arrange
        var context = new IdentityContext();
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var name = "Test User";
        var username = "testuser";

        // Act
        context.SetIdentity(userId, email, name, username);

        // Assert
        Assert.Equal(userId, context.UserId);
        Assert.Equal(email, context.Email);
        Assert.Equal(name, context.Name);
        Assert.Equal(username, context.Username);
        Assert.True(context.IsAuthenticated);
    }

    [Fact]
    public void SetIdentity_WithNullOptionalValues_SetsNullablePropertiesCorrectly()
    {
        // Arrange
        var context = new IdentityContext();
        var userId = Guid.NewGuid();

        // Act
        context.SetIdentity(userId, null, null, null);

        // Assert
        Assert.Equal(userId, context.UserId);
        Assert.Null(context.Email);
        Assert.Null(context.Name);
        Assert.Null(context.Username);
        Assert.True(context.IsAuthenticated);
    }

    [Fact]
    public void Clear_AfterSetIdentity_ResetsAllProperties()
    {
        // Arrange
        var context = new IdentityContext();
        context.SetIdentity(Guid.NewGuid(), "test@example.com", "Test", "test");

        // Act
        context.Clear();

        // Assert
        Assert.Equal(Guid.Empty, context.UserId);
        Assert.Null(context.Email);
        Assert.Null(context.Name);
        Assert.Null(context.Username);
        Assert.False(context.IsAuthenticated);
    }

    [Fact]
    public void NewInstance_IsNotAuthenticated()
    {
        // Arrange & Act
        var context = new IdentityContext();

        // Assert
        Assert.Equal(Guid.Empty, context.UserId);
        Assert.False(context.IsAuthenticated);
    }
}
