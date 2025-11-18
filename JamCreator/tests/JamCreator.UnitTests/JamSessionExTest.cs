using JamCreator.Shared.Extensions;
using JamCreator.Shared.Models;

namespace JamCreator.UnitTests;

public class JamSessionExtensionsTests
{
    [Fact]
    public void IsJoinable_PublicSession_ReturnsTrue()
    {
        var session = new JamSessionModel
        {
            IsPrivate = false,
            Password = null
        };

        var result = session.IsJoinable();

        Assert.True(result);
    }

    [Fact]
    public void IsJoinable_PrivateWithoutPassword_ReturnsFalse()
    {
        var session = new JamSessionModel
        {
            IsPrivate = true,
            Password = "" // empty / whitespace should block joining
        };

        var result = session.IsJoinable();

        Assert.False(result);
    }

    [Fact]
    public void IsJoinable_PrivateWithPassword_ReturnsTrue()
    {

        var session = new JamSessionModel
        {
            IsPrivate = true,
            Password = "secret"
        };

        var result = session.IsJoinable();

        Assert.True(result);
    }

    [Fact]
    public void IsJoinable_PrivateWithWhitespacePassword_ReturnsFalse()
    {
        // Arrange
        var session = new JamSessionModel
        {
            IsPrivate = true,
            Password = "   " // whitespace should be treated as empty
        };

        // Act
        var result = session.IsJoinable();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsJoinable_PublicWithPassword_ReturnsTrue()
    {
        // Arrange
        var session = new JamSessionModel
        {
            IsPrivate = false,
            Password = "secret"
        };

        // Act
        var result = session.IsJoinable();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsJoinable_DefaultSession_ReturnsTrue()
    {
        // Arrange
        var session = new JamSessionModel(); // uses default values

        // Act
        var result = session.IsJoinable();

        // Assert
        Assert.False(session.IsPrivate); // sanity check
        Assert.True(result);
    }



}
