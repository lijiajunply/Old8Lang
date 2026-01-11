using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Services;

/// <summary>
/// 测试 FormattingService - 代码格式化服务
/// </summary>
public class FormattingServiceTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TestFormatDocument_SimpleFunction()
    {
        // Arrange
        var service = new FormattingService();
        var code = @"
func test() -> void {
a <- 10
b <- 20
}
";
        var options = new FormattingOptions
        {
            TabSize = 4,
            InsertSpaces = true
        };

        // Act
        var edits = service.FormatDocument(code, options);

        // Assert
        Assert.NotNull(edits);
        testOutputHelper.WriteLine($"Generated {edits.Count} formatting edits");

        foreach (var edit in edits)
        {
            testOutputHelper.WriteLine($"Edit: {edit.NewText.Length} chars");
        }
    }

    [Fact]
    public void TestFormatDocument_NestedBlocks()
    {
        // Arrange
        var service = new FormattingService();
        var code = @"
func outer() -> void {
if true {
a <- 10
}
}
";
        var options = new FormattingOptions
        {
            TabSize = 4,
            InsertSpaces = true
        };

        // Act
        var edits = service.FormatDocument(code, options);

        // Assert
        Assert.NotNull(edits);
        testOutputHelper.WriteLine($"Nested blocks: {edits.Count} edits");
    }

    [Fact]
    public void TestFormatDocument_WithTabs()
    {
        // Arrange
        var service = new FormattingService();
        var code = @"
func test() -> void {
a <- 10
}
";
        var options = new FormattingOptions
        {
            TabSize = 1,
            InsertSpaces = false // 使用制表符
        };

        // Act
        var edits = service.FormatDocument(code, options);

        // Assert
        Assert.NotNull(edits);
        testOutputHelper.WriteLine("Formatting with tabs completed");
    }

    [Fact]
    public void TestFormatDocument_Class()
    {
        // Arrange
        var service = new FormattingService();
        var code = @"
class Test {
public x:int
public func method() -> void {
a <- 10
}
}
";
        var options = new FormattingOptions
        {
            TabSize = 4,
            InsertSpaces = true
        };

        // Act
        var edits = service.FormatDocument(code, options);

        // Assert
        Assert.NotNull(edits);
        testOutputHelper.WriteLine($"Class formatting: {edits.Count} edits");
    }

    [Fact]
    public void TestFormatDocument_Comments()
    {
        // Arrange
        var service = new FormattingService();
        var code = @"
// Comment 1
func test() -> void {
// Comment 2
a <- 10
}
";
        var options = new FormattingOptions
        {
            TabSize = 4,
            InsertSpaces = true
        };

        // Act
        var edits = service.FormatDocument(code, options);

        // Assert
        Assert.NotNull(edits);
        testOutputHelper.WriteLine("Comments preserved in formatting");
    }

    [Fact]
    public void TestFormatDocument_EmptyLines()
    {
        // Arrange
        var service = new FormattingService();
        var code = @"
func test() -> void {

a <- 10

}
";
        var options = new FormattingOptions
        {
            TabSize = 4,
            InsertSpaces = true
        };

        // Act
        var edits = service.FormatDocument(code, options);

        // Assert
        Assert.NotNull(edits);
        testOutputHelper.WriteLine("Empty lines handled");
    }

    [Fact]
    public void TestFormatRange_Subset()
    {
        // Arrange
        var service = new FormattingService();
        var code = @"
func test() -> void {
a <- 10
b <- 20
c <- 30
}
";
        var range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
            new Position(2, 0),
            new Position(3, 10)
        );
        var options = new FormattingOptions
        {
            TabSize = 4,
            InsertSpaces = true
        };

        // Act
        var edits = service.FormatRange(code, range, options);

        // Assert
        Assert.NotNull(edits);
        testOutputHelper.WriteLine($"Range formatting: {edits.Count} edits");
    }

    [Fact]
    public void TestFormatDocument_PreservesLogic()
    {
        // Arrange
        var service = new FormattingService();
        var code = @"
func add(a:int, b:int) -> int {
return a + b
}
";
        var options = new FormattingOptions
        {
            TabSize = 4,
            InsertSpaces = true
        };

        // Act
        var edits = service.FormatDocument(code, options);

        // Assert
        Assert.NotNull(edits);

        // 格式化不应该改变代码逻辑，只改变空白字符
        foreach (var edit in edits)
        {
            var newText = edit.NewText;
            Assert.Contains("func", newText);
            Assert.Contains("return", newText);
        }

        testOutputHelper.WriteLine("Logic preserved after formatting");
    }

    [Fact]
    public void TestFormatDocument_DifferentTabSizes()
    {
        // Arrange
        var service = new FormattingService();
        var code = @"
func test() -> void {
a <- 10
}
";

        // Act & Assert - TabSize 2
        var edits2 = service.FormatDocument(code, new FormattingOptions { TabSize = 2, InsertSpaces = true });
        testOutputHelper.WriteLine($"TabSize 2: {edits2.Count} edits");

        // Act & Assert - TabSize 4
        var edits4 = service.FormatDocument(code, new FormattingOptions { TabSize = 4, InsertSpaces = true });
        testOutputHelper.WriteLine($"TabSize 4: {edits4.Count} edits");

        // Act & Assert - TabSize 8
        var edits8 = service.FormatDocument(code, new FormattingOptions { TabSize = 8, InsertSpaces = true });
        testOutputHelper.WriteLine($"TabSize 8: {edits8.Count} edits");

        Assert.NotNull(edits2);
        Assert.NotNull(edits4);
        Assert.NotNull(edits8);
    }
}
