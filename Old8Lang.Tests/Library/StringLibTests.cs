using Old8LangLib;

namespace Old8Lang.Tests.Library;

/// <summary>
/// 字符串处理模块测试
/// </summary>
public class StringLibTests
{
    [Fact]
    public void RegexIsMatch_ValidPattern_ReturnsTrue()
    {
        // Arrange
        string input = "test@example.com";
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        
        // Act
        var result = StringLib.RegexIsMatch(input, pattern);
        
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
        var result = StringLib.RegexIsMatch(input, pattern);
        
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
        var result = StringLib.RegexReplace(input, pattern, replacement);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello, Old8Lang!", result);
    }

    [Fact]
    public void ToBase64_String_ReturnsBase64String()
    {
        // Arrange
        string input = "test string";
        
        // Act
        var result = StringLib.ToBase64(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("dGVzdCBzdHJpbmc=", result);
    }

    [Fact]
    public void FromBase64_Base64String_ReturnsOriginalString()
    {
        // Arrange
        string input = "dGVzdCBzdHJpbmc=";
        
        // Act
        var result = StringLib.FromBase64(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test string", result);
    }

    [Fact]
    public void ToUpper_String_ReturnsUppercase()
    {
        // Arrange
        string input = "hello world";
        
        // Act
        var result = StringLib.ToUpper(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("HELLO WORLD", result);
    }

    [Fact]
    public void ToLower_String_ReturnsLowercase()
    {
        // Arrange
        string input = "HELLO WORLD";
        
        // Act
        var result = StringLib.ToLower(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void Trim_String_ReturnsTrimmedString()
    {
        // Arrange
        string input = "  hello world  ";
        
        // Act
        var result = StringLib.Trim(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void StartsWith_Match_ReturnsTrue()
    {
        // Arrange
        string input = "hello world";
        string prefix = "hello";
        
        // Act
        var result = StringLib.StartsWith(input, prefix);
        
        // Assert
        Assert.True(result);
    }

    [Fact]
    public void EndsWith_Match_ReturnsTrue()
    {
        // Arrange
        string input = "hello world";
        string suffix = "world";
        
        // Act
        var result = StringLib.EndsWith(input, suffix);
        
        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Split_String_ReturnsArray()
    {
        // Arrange
        string input = "hello,world,test";
        string separator = ",";
        
        // Act
        var result = StringLib.Split(input, separator);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal("hello", result[0]);
        Assert.Equal("world", result[1]);
        Assert.Equal("test", result[2]);
    }

    [Fact]
    public void Join_Array_ReturnsString()
    {
        // Arrange
        string[] input = { "hello", "world", "test" };
        string separator = ",";
        
        // Act
        var result = StringLib.Join(input, separator);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("hello,world,test", result);
    }
}
