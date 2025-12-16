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
            Password = "" 
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
        var session = new JamSessionModel
        {
            IsPrivate = true,
            Password = "   " 
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
        var session = new JamSessionModel();

        // Act
        var result = session.IsJoinable();

        // Assert
        Assert.False(session.IsPrivate); 
        Assert.True(result);
    }



}
