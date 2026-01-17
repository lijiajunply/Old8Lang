using Old8Lang.LangParser;

namespace Old8Lang.Tests.Parser;

/// <summary>
/// EscapeSequenceHelper 的单元测试
/// </summary>
public class EscapeSequenceHelperTests
{
    #region Unicode 转义序列测试

    [Fact]
    public void TryParseUnicodeEscape_ValidSequence_ReturnsTrue()
    {
        // Arrange
        const string input = "\\u4E2D";
        const int startIndex = 0;

        // Act
        var result = EscapeSequenceHelper.TryParseUnicodeEscape(input, startIndex, out var ch, out var advance);

        // Assert
        Assert.True(result);
        Assert.Equal('中', ch);
        Assert.Equal(4, advance);
    }

    [Fact]
    public void TryParseUnicodeEscape_MultipleSequences_ReturnsCorrect()
    {
        // Arrange
        const string input = "\\u4E2D\\u6587";

        // Act
        var result1 = EscapeSequenceHelper.TryParseUnicodeEscape(input, 0, out var ch1, out var advance1);
        var result2 = EscapeSequenceHelper.TryParseUnicodeEscape(input, 6, out var ch2, out var advance2);

        // Assert
        Assert.True(result1);
        Assert.Equal('中', ch1);
        Assert.Equal(4, advance1);

        Assert.True(result2);
        Assert.Equal('文', ch2);
        Assert.Equal(4, advance2);
    }

    [Fact]
    public void TryParseUnicodeEscape_InsufficientLength_ReturnsFalse()
    {
        // Arrange
        const string input = "\\u4E"; // 只有 2 位十六进制
        const int startIndex = 0;

        // Act
        var result = EscapeSequenceHelper.TryParseUnicodeEscape(input, startIndex, out var ch, out var advance);

        // Assert
        Assert.False(result);
        Assert.Equal('\0', ch);
        Assert.Equal(0, advance);
    }

    [Fact]
    public void TryParseUnicodeEscape_InvalidHex_ReturnsFalse()
    {
        // Arrange
        const string input = "\\uGGGG"; // 无效的十六进制
        const int startIndex = 0;

        // Act
        var result = EscapeSequenceHelper.TryParseUnicodeEscape(input, startIndex, out var ch, out var advance);

        // Assert
        Assert.False(result);
        Assert.Equal('\0', ch);
        Assert.Equal(0, advance);
    }

    [Fact]
    public void TryParseUnicodeEscapeFromContent_ValidContent_ReturnsTrue()
    {
        // Arrange
        const string content = "\\u4E2D";

        // Act
        var result = EscapeSequenceHelper.TryParseUnicodeEscapeFromContent(content, out var ch);

        // Assert
        Assert.True(result);
        Assert.Equal('中', ch);
    }

    [Fact]
    public void TryParseUnicodeEscapeFromContent_WrongLength_ReturnsFalse()
    {
        // Arrange
        const string content = "\\u4E2"; // 只有 3 位十六进制

        // Act
        var result = EscapeSequenceHelper.TryParseUnicodeEscapeFromContent(content, out var ch);

        // Assert
        Assert.False(result);
        Assert.Equal('\0', ch);
    }

    [Fact]
    public void TryParseUnicodeEscapeFromContent_NotStartWithBackslashU_ReturnsFalse()
    {
        // Arrange
        const string content = "u4E2D"; // 没有反斜杠

        // Act
        var result = EscapeSequenceHelper.TryParseUnicodeEscapeFromContent(content, out var ch);

        // Assert
        Assert.False(result);
        Assert.Equal('\0', ch);
    }

    #endregion

    #region 十六进制转义序列测试

    [Fact]
    public void TryParseHexEscape_ValidSequence_ReturnsTrue()
    {
        // Arrange
        const string input = "\\x41";
        const int startIndex = 0;

        // Act
        var result = EscapeSequenceHelper.TryParseHexEscape(input, startIndex, out var ch, out var advance);

        // Assert
        Assert.True(result);
        Assert.Equal('A', ch);
        Assert.Equal(2, advance);
    }

    [Fact]
    public void TryParseHexEscape_MultipleSequences_ReturnsCorrect()
    {
        // Arrange
        const string input = "\\x41\\x42\\x43";

        // Act
        var result1 = EscapeSequenceHelper.TryParseHexEscape(input, 0, out var ch1, out var advance1);
        var result2 = EscapeSequenceHelper.TryParseHexEscape(input, 4, out var ch2, out var advance2);
        var result3 = EscapeSequenceHelper.TryParseHexEscape(input, 8, out var ch3, out var advance3);

        // Assert
        Assert.True(result1);
        Assert.Equal('A', ch1);
        Assert.Equal(2, advance1);

        Assert.True(result2);
        Assert.Equal('B', ch2);
        Assert.Equal(2, advance2);

        Assert.True(result3);
        Assert.Equal('C', ch3);
        Assert.Equal(2, advance3);
    }

    [Fact]
    public void TryParseHexEscape_InsufficientLength_ReturnsFalse()
    {
        // Arrange
        const string input = "\\x4"; // 只有 1 位十六进制
        const int startIndex = 0;

        // Act
        var result = EscapeSequenceHelper.TryParseHexEscape(input, startIndex, out var ch, out var advance);

        // Assert
        Assert.False(result);
        Assert.Equal('\0', ch);
        Assert.Equal(0, advance);
    }

    [Fact]
    public void TryParseHexEscape_InvalidHex_ReturnsFalse()
    {
        // Arrange
        const string input = "\\xGG"; // 无效的十六进制
        const int startIndex = 0;

        // Act
        var result = EscapeSequenceHelper.TryParseHexEscape(input, startIndex, out var ch, out var advance);

        // Assert
        Assert.False(result);
        Assert.Equal('\0', ch);
        Assert.Equal(0, advance);
    }

    [Fact]
    public void TryParseHexEscapeFromContent_ValidContent_ReturnsTrue()
    {
        // Arrange
        const string content = "\\x41";

        // Act
        var result = EscapeSequenceHelper.TryParseHexEscapeFromContent(content, out var ch);

        // Assert
        Assert.True(result);
        Assert.Equal('A', ch);
    }

    [Fact]
    public void TryParseHexEscapeFromContent_LongerContent_ReturnsTrue()
    {
        // Arrange
        const string content = "\\x7A"; // 'z'

        // Act
        var result = EscapeSequenceHelper.TryParseHexEscapeFromContent(content, out var ch);

        // Assert
        Assert.True(result);
        Assert.Equal('z', ch);
    }

    [Fact]
    public void TryParseHexEscapeFromContent_NotStartWithBackslashX_ReturnsFalse()
    {
        // Arrange
        const string content = "x41"; // 没有反斜杠

        // Act
        var result = EscapeSequenceHelper.TryParseHexEscapeFromContent(content, out var ch);

        // Assert
        Assert.False(result);
        Assert.Equal('\0', ch);
    }

    #endregion

    #region 边界和特殊情况测试

    [Fact]
    public void TryParseUnicodeEscape_NullTerminator_ReturnsTrue()
    {
        // Arrange
        const string input = "\\u0000";
        const int startIndex = 0;

        // Act
        var result = EscapeSequenceHelper.TryParseUnicodeEscape(input, startIndex, out var ch, out var advance);

        // Assert
        Assert.True(result);
        Assert.Equal('\0', ch);
        Assert.Equal(4, advance);
    }

    [Fact]
    public void TryParseHexEscape_NullTerminator_ReturnsTrue()
    {
        // Arrange
        const string input = "\\x00";
        const int startIndex = 0;

        // Act
        var result = EscapeSequenceHelper.TryParseHexEscape(input, startIndex, out var ch, out var advance);

        // Assert
        Assert.True(result);
        Assert.Equal('\0', ch);
        Assert.Equal(2, advance);
    }

    [Fact]
    public void TryParseUnicodeEscape_MaxUnicodeValue_ReturnsTrue()
    {
        // Arrange
        const string input = "\\uFFFF";
        const int startIndex = 0;

        // Act
        var result = EscapeSequenceHelper.TryParseUnicodeEscape(input, startIndex, out var ch, out var advance);

        // Assert
        Assert.True(result);
        Assert.Equal('\uFFFF', ch);
        Assert.Equal(4, advance);
    }

    [Fact]
    public void TryParseHexEscape_LowercaseHex_ReturnsTrue()
    {
        // Arrange
        const string input = "\\x61"; // 'a'
        const int startIndex = 0;

        // Act
        var result = EscapeSequenceHelper.TryParseHexEscape(input, startIndex, out var ch, out var advance);

        // Assert
        Assert.True(result);
        Assert.Equal('a', ch);
        Assert.Equal(2, advance);
    }

    [Fact]
    public void TryParseUnicodeEscape_MixedCaseHex_ReturnsTrue()
    {
        // Arrange
        const string input = "\\u4e2D"; // 混合大小写
        const int startIndex = 0;

        // Act
        var result = EscapeSequenceHelper.TryParseUnicodeEscape(input, startIndex, out var ch, out var advance);

        // Assert
        Assert.True(result);
        Assert.Equal('中', ch);
        Assert.Equal(4, advance);
    }

    #endregion
}
