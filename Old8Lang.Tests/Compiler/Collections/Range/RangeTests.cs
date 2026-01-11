using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Collections.Range;

/// <summary>
/// 编译器模式下的范围表达式测试
/// </summary>
public class RangeTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void BasicInclusiveRange_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 基本包含范围 [start~end]
            range1 <- [1~5]
            range2 <- [10~20]

            // 测试基本功能 - 使用数组属性
            Assert.Equal(5, range1.Length)
            Assert.Equal(11, range2.Length)

            // 测试起始值 - 使用数组索引
            Assert.Equal(1, range1[0])
            Assert.Equal(10, range2[0])

            // 测试结束值 - 使用数组最后一个元素
            Assert.Equal(5, range1[4])
            Assert.Equal(20, range2[10])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void BasicExclusiveRange_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 基本范围 [start~end] - 包含两端
            range1 <- [1~5]
            range2 <- [10~20]

            // 测试基本功能 - 使用数组属性
            Assert.Equal(5, range1.Length)
            Assert.Equal(11, range2.Length)

            // 测试起始值 - 使用数组索引
            Assert.Equal(1, range1[0])
            Assert.Equal(10, range2[0])

            // 测试结束值 - 使用数组最后一个元素
            Assert.Equal(5, range1[4])
            Assert.Equal(20, range2[10])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void RangeWithLeftInclusion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 左包含右排除 [start~<end]
            range1 <- [1~<5]
            range2 <- [10~<20]

            // Test left inclusion, right exclusion
            // [1~<5] = [1,2,3,4], length is 4
            Assert.Equal(4, range1.Length)
            Assert.Equal(10, range2.Length)

            // Test start value (included)
            Assert.Equal(1, range1[0])
            Assert.Equal(10, range2[0])

            // Test last value (4 for range1, 19 for range2)
            Assert.Equal(4, range1[3])
            Assert.Equal(19, range2[9])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void RangeWithRightInclusion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 左排除右包含 [start>~end]
            range1 <- [1>~5]
            range2 <- [10>~20]

            // Test left exclusion, right inclusion
            // [1>~5] = [2,3,4,5], length is 4
            Assert.Equal(4, range1.Length)
            Assert.Equal(10, range2.Length)

            // Test start value (excluded, so first element is 2)
            Assert.Equal(2, range1[0])
            Assert.Equal(11, range2[0])

            // Test end value (included)
            Assert.Equal(5, range1[3])
            Assert.Equal(20, range2[9])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void RangeWithBothExclusions_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // Both ends excluded
            range1 <- [1>~<5]
            range2 <- [1>~<5]

            // Test both ends exclusion
            // [1>~<5] = [2,3,4], length is 3
            Assert.Equal(3, range1.Length)
            Assert.Equal(2, range1[0])
            Assert.Equal(3, range1[1])
            Assert.Equal(4, range1[2])

            // Verify 1 and 5 are not included
            Assert.False(range1[0] == 1)
            Assert.False(range1[2] == 5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact(Skip = "Range with step is not yet implemented")]
    public void RangesWithDifferentStep_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            range_1 <- [1~10:2]
            range_2 <- [1~10:2]

            // Test different step sizes
            sum_1 <- 0
            sum_2 <- 0
            i <- 1
            while i < 10 {
                sum_1 <- sum_1 + range_1[i]
                i <- i + 1
            }
            i <- 1
            while i < 10 {
                sum_2 <- sum_2 + range_2[i]
                i <- i + 1
            }

            Assert.Equal(12, sum_1)
            Assert.Equal(12, sum_2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact(Skip = "Character ranges are not yet supported in compiler mode")]
    public void CharacterRange_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // Character range test
            text_range <- ['A'~'Z']

            // Test character range
            Assert.Equal(26, text_range.Length)
            Assert.Equal('A', text_range[0])
            Assert.Equal('Z', text_range[25])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact(Skip = "Unicode character ranges are not yet supported in compiler mode")]
    public void UnicodeCharacterRange_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // Unicode character range test
            greek_range <- ['α'~'ω']

            // Test Unicode character range
            Assert.True(greek_range.Length > 0)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void RangeEdgeCases_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // Test single value range
            single_range <- [5~5]
            Assert.Equal(1, single_range.Length)
            Assert.Equal(5, single_range[0])

            // Test single value exclusive range (both ends excluded, should be empty)
            single_exclusive <- [5>~<5]
            Assert.Equal(0, single_exclusive.Length)

            // Test reversed range (should be descending)
            reversed_range <- [10~1]
            Assert.Equal(10, reversed_range.Length)
            Assert.Equal(10, reversed_range[0])
            Assert.Equal(1, reversed_range[9])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void RangeOperations_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            range_a <- [1~5]
            range_b <- [3~8]

            // Test basic array operations
            Assert.Equal(5, range_a.Length)
            Assert.Equal(6, range_b.Length)

            // Test array elements
            Assert.Equal(1, range_a[0])
            Assert.Equal(5, range_a[4])

            Assert.Equal(3, range_b[0])
            Assert.Equal(8, range_b[5])

            // Test the first element
            Assert.Equal(1, range_a[0])
            Assert.Equal(3, range_b[0])

            // Test the last element
            Assert.Equal(5, range_a[4])
            Assert.Equal(8, range_b[5])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}