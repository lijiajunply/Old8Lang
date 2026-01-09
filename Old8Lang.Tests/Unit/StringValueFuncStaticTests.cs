using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ValueFunctions;
using Xunit;

namespace Old8Lang.Tests.Unit;

/// <summary>
/// StringValueFuncStatic 的单元测试，重点测试重构后的验证功能
/// </summary>
public class StringValueFuncStaticTests
{
    #region StartsWith 测试

    [Fact]
    public void StartsWith_ValidPrefix_ReturnsTrue()
    {
        // Arrange
        var str = new StringLangValue("Hello World");
        var prefix = new StringLangValue("Hello");

        // Act
        var result = str.StartsWith(prefix);

        // Assert
        Assert.True(result.Value);
    }

    [Fact]
    public void StartsWith_InvalidPrefix_ReturnsFalse()
    {
        // Arrange
        var str = new StringLangValue("Hello World");
        var prefix = new StringLangValue("World");

        // Act
        var result = str.StartsWith(prefix);

        // Assert
        Assert.False(result.Value);
    }

    [Fact]
    public void StartsWith_NullOrEmptyString_ThrowsException()
    {
        // Arrange
        var str = new StringLangValue("");
        var prefix = new StringLangValue("Hello");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => str.StartsWith(prefix));
    }

    [Fact]
    public void StartsWith_NullOrEmptyPrefix_ThrowsException()
    {
        // Arrange
        var str = new StringLangValue("Hello World");
        var prefix = new StringLangValue("");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => str.StartsWith(prefix));
    }

    #endregion

    #region EndsWith 测试

    [Fact]
    public void EndsWith_ValidSuffix_ReturnsTrue()
    {
        // Arrange
        var str = new StringLangValue("Hello World");
        var suffix = new StringLangValue("World");

        // Act
        var result = str.EndsWith(suffix);

        // Assert
        Assert.True(result.Value);
    }

    [Fact]
    public void EndsWith_InvalidSuffix_ReturnsFalse()
    {
        // Arrange
        var str = new StringLangValue("Hello World");
        var suffix = new StringLangValue("Hello");

        // Act
        var result = str.EndsWith(suffix);

        // Assert
        Assert.False(result.Value);
    }

    [Fact]
    public void EndsWith_NullOrEmptyString_ThrowsException()
    {
        // Arrange
        var str = new StringLangValue("");
        var suffix = new StringLangValue("World");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => str.EndsWith(suffix));
    }

    [Fact]
    public void EndsWith_NullOrEmptySuffix_ThrowsException()
    {
        // Arrange
        var str = new StringLangValue("Hello World");
        var suffix = new StringLangValue("");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => str.EndsWith(suffix));
    }

    #endregion

    #region IndexOf 测试

    [Fact]
    public void IndexOf_SubstringExists_ReturnsIndex()
    {
        // Arrange
        var str = new StringLangValue("Hello World");
        var substring = new StringLangValue("World");

        // Act
        var result = str.IndexOf(substring);

        // Assert
        Assert.Equal(6, result.Value);
    }

    [Fact]
    public void IndexOf_SubstringNotExists_ReturnsNegativeOne()
    {
        // Arrange
        var str = new StringLangValue("Hello World");
        var substring = new StringLangValue("Foo");

        // Act
        var result = str.IndexOf(substring);

        // Assert
        Assert.Equal(-1, result.Value);
    }

    [Fact]
    public void IndexOf_NullOrEmptyString_ThrowsException()
    {
        // Arrange
        var str = new StringLangValue("");
        var substring = new StringLangValue("World");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => str.IndexOf(substring));
    }

    [Fact]
    public void IndexOf_NullOrEmptySubstring_ThrowsException()
    {
        // Arrange
        var str = new StringLangValue("Hello World");
        var substring = new StringLangValue("");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => str.IndexOf(substring));
    }

    #endregion

    #region Repeat 测试

    [Fact]
    public void Repeat_PositiveCount_ReturnsRepeatedString()
    {
        // Arrange
        var input = new StringLangValue("AB");
        var count = new IntLangValue(3);

        // Act
        var result = input.Repeat(count);

        // Assert
        Assert.Equal("ABABAB", result);
    }

    [Fact]
    public void Repeat_ZeroCount_ReturnsEmptyString()
    {
        // Arrange
        var input = new StringLangValue("AB");
        var count = new IntLangValue(0);

        // Act
        var result = input.Repeat(count);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Repeat_NegativeCount_ThrowsException()
    {
        // Arrange
        var input = new StringLangValue("AB");
        var count = new IntLangValue(-1);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => input.Repeat(count));
    }

    [Fact]
    public void Repeat_NullOrEmptyString_ThrowsException()
    {
        // Arrange
        var input = new StringLangValue("");
        var count = new IntLangValue(3);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => input.Repeat(count));
    }

    #endregion

    #region TrimStart 测试

    [Fact]
    public void TrimStart_StringWithLeadingWhitespace_RemovesWhitespace()
    {
        // Arrange
        var input = new StringLangValue("  Hello");

        // Act
        var result = input.TrimStart();

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void TrimStart_StringWithoutLeadingWhitespace_ReturnsOriginal()
    {
        // Arrange
        var input = new StringLangValue("Hello");

        // Act
        var result = input.TrimStart();

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void TrimStart_NullOrEmptyString_ThrowsException()
    {
        // Arrange
        var input = new StringLangValue("");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => input.TrimStart());
    }

    #endregion

    #region TrimEnd 测试

    [Fact]
    public void TrimEnd_StringWithTrailingWhitespace_RemovesWhitespace()
    {
        // Arrange
        var input = new StringLangValue("Hello  ");

        // Act
        var result = input.TrimEnd();

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void TrimEnd_StringWithoutTrailingWhitespace_ReturnsOriginal()
    {
        // Arrange
        var input = new StringLangValue("Hello");

        // Act
        var result = input.TrimEnd();

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void TrimEnd_NullOrEmptyString_ThrowsException()
    {
        // Arrange
        var input = new StringLangValue("");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => input.TrimEnd());
    }

    #endregion

    #region ToBase64 测试

    [Fact]
    public void ToBase64_ValidString_ReturnsBase64()
    {
        // Arrange
        var input = new StringLangValue("Hello");

        // Act
        var result = input.ToBase64();

        // Assert
        Assert.Equal("SGVsbG8=", result);
    }

    [Fact]
    public void ToBase64_ChineseCharacters_ReturnsBase64()
    {
        // Arrange
        var input = new StringLangValue("中文");

        // Act
        var result = input.ToBase64();

        // Assert
        Assert.NotEmpty(result);
        // 验证可以解码回来
        var decoded = new StringLangValue(result).FromBase64();
        Assert.Equal("中文", decoded);
    }

    [Fact]
    public void ToBase64_NullOrEmptyString_ThrowsException()
    {
        // Arrange
        var input = new StringLangValue("");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => input.ToBase64());
    }

    #endregion

    #region FromBase64 测试

    [Fact]
    public void FromBase64_ValidBase64_ReturnsDecodedString()
    {
        // Arrange
        var input = new StringLangValue("SGVsbG8=");

        // Act
        var result = input.FromBase64();

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void FromBase64_InvalidBase64_ThrowsException()
    {
        // Arrange
        var input = new StringLangValue("InvalidBase64!!!");

        // Act & Assert
        var ex = Assert.Throws<EncodingException>(() => input.FromBase64());
        Assert.Contains("Base64解码失败", ex.Message);
    }

    [Fact]
    public void FromBase64_NullOrEmptyString_ThrowsException()
    {
        // Arrange
        var input = new StringLangValue("");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => input.FromBase64());
    }

    #endregion
}
