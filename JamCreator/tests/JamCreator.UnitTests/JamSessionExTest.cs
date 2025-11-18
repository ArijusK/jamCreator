using JamCreator.Shared.Extensions;
using JamCreator.Shared.Models;

namespace JamCreator.UnitTests;

public class JamSessionExtensionsTests
{
    [Fact]
    public void IsJoinable_PublicSession_ReturnsTrue()
    {
        // Arrange
        var session = new JamSessionModel
        {
            IsPrivate = false,
            Password = null
        };

        // Act
        var result = session.IsJoinable();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsJoinable_PrivateWithoutPassword_ReturnsFalse()
    {
        // Arrange
        var session = new JamSessionModel
        {
            IsPrivate = true,
            Password = "" // empty / whitespace should block joining
        };

        // Act
        var result = session.IsJoinable();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsJoinable_PrivateWithPassword_ReturnsTrue()
    {
        // Arrange
        var session = new JamSessionModel
        {
            IsPrivate = true,
            Password = "secret"
        };

        // Act
        var result = session.IsJoinable();

        // Assert
        Assert.True(result);
    }
} /* com */
