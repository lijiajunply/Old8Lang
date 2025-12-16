using Old8LangLib;

namespace Old8Lang.Tests.Library;

public class RegexLibTest
{
    [Fact]
    public void RegexIsMatch_ValidPattern_ReturnsTrue()
    {
        // Arrange
        string input = "test@example.com";
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        
        // Act
        var result = RegexLib.RegexIsMatch(input, pattern);
        
        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RegexIsMatch_InvalidPattern_ReturnsFalse()
    {
        // Arrange
        string input = "invalid-email";
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        
        // Act
        var result = RegexLib.RegexIsMatch(input, pattern);
        
        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RegexReplace_MatchFound_ReturnsReplacedString()
    {
        // Arrange
        string input = "Hello, World!";
        string pattern = "World";
        string replacement = "Old8Lang";
        
        // Act
        var result = RegexLib.RegexReplace(input, pattern, replacement);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello, Old8Lang!", result);
    }
}