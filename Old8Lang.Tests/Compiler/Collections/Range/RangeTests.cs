using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Collections.Range;

/// <summary>
/// 编译器模式下的范围表达式测试
/// </summary>
public class RangeTests
{
    private readonly ITestOutputHelper _output;

    public RangeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BasicInclusiveRange_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 基本包含范围 [start..end]
            range1 <- [1..5]
            range2 <- [10..20]
            
            // 测试基本功能
            Assert.Equal(5, range1.Length())
            Assert.Equal(11, range2.Length())
            
            // 测试起始值
            Assert.Equal(1, range1.Start())
            Assert.Equal(10, range2.Start())
            
            // 测试结束值
            Assert.Equal(5, range1.End())
            Assert.Equal(20, range2.End())
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
            // 基本排除范围 [start~end]
            range1 <- [1~5]
            range2 <- [10~20]
            
            // 测试基本功能
            Assert.Equal(4, range1.Length())
            Assert.Equal(10, range2.Length())
            
            // 测试起始值
            Assert.Equal(2, range1.Start())
            Assert.Equal(11, range2.Start())
            
            // 测试结束值
            Assert.Equal(5, range1.End())
            Assert.Equal(20, range2.End())
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
            // 左包含右排除 [start..end~
            range1 <- [1..5~
            range2 <- [10..20~
            
            // Test left inclusion, right exclusion
            Assert.Equal(5, range1.Length())
            Assert.Equal(11, range2.Length())
            
            // Test start value (included)
            Assert.Equal(1, range1.Start())
            Assert.Equal(10, range2.Start())
            
            // Test end value (excluded)
            Assert.Equal(6, range1.End())
            Assert.Equal(21, range2.End())
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
            // 左排除右包含 [start~..end]
            range1 <- [1~..5]
            range2 <- [10~..20]
            
            // Test left exclusion, right inclusion
            Assert.Equal(5, range1.Length())
            Assert.Equal(11, range2.Length())
            
            // Test start value (excluded)
            Assert.Equal(2, range1.Start())
            Assert.Equal(11, range2.Start())
            
            // Test end value (included)
            Assert.Equal(5, range1.End())
            Assert.Equal(20, range2.End())
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
            range1 <- [1~5]
            range2 <- [1~5]
            
            // Test both ends exclusion
            Assert.Equal([2, 3, 4, 5], range1)
            Assert.False(range1.Contains(1))
            Assert.False(range2.Contains(1))
            Assert.False(range1.Contains(5))
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

    [Fact]
    public void CharacterRange_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // Character range test
            text_range <- ['A'..'Z']
            
            // Test character range
            Assert.Equal(26, text_range.Length())
            Assert.Equal('A', text_range.Start())
            Assert.Equal('Z', text_range.End())
            
            // Test Contains method for characters
            Assert.True(text_range.Contains('A'))
            Assert.True(text_range.Contains('M'))
            Assert.True(text_range.Contains('Z'))
            Assert.False(text_range.Contains('a'))
            Assert.False(text_range.Contains('@'))
            
            // Test ToArray conversion
            char_array <- text_range.ToArray()
            Assert.Equal(26, char_array.Length)
            Assert.Equal('A', char_array[0])
            Assert.Equal('Z', char_array[25])
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
    public void UnicodeCharacterRange_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // Unicode character range test
            greek_range <- ['α'..'ω']
            
            // Test Unicode character range
            Assert.True(greek_range.Length() > 0)
            Assert.Equal('α', greek_range.Start())
            Assert.Equal('ω', greek_range.End())
            
            // Test Contains method for Unicode characters
            Assert.True(greek_range.Contains('α'))
            Assert.True(greek_range.Contains('π'))
            Assert.True(greek_range.Contains('ω'))
            Assert.False(greek_range.Contains('a'))
            Assert.False(greek_range.Contains('Ω'))
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
            single_range <- [5..5]
            Assert.Equal(1, single_range.Length())
            Assert.Equal(5, single_range.Start())
            Assert.Equal(5, single_range.End())
            Assert.True(single_range.Contains(5))
            Assert.False(single_range.Contains(4))
            Assert.False(single_range.Contains(6))
            
            // Test single value exclusive range (should be empty)
            single_exclusive <- [5~5]
            Assert.Equal(1, single_exclusive.Length())
            Assert.Equal(6, single_exclusive.Start())
            Assert.Equal(5, single_exclusive.End())
            
            // Test reversed range (empty by default)
            reversed_range <- [10..1]
            Assert.Equal(0, reversed_range.Length())
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
            range_a <- [1..5]
            range_b <- [3..8]
            
            // Test Contains method with multiple elements
            Assert.True(range_a.Contains(3))
            Assert.True(range_a.Contains(5))
            Assert.False(range_a.Contains(6))
            Assert.False(range_a.Contains(0))
            
            Assert.True(range_b.Contains(3))
            Assert.True(range_b.Contains(8))
            Assert.False(range_b.Contains(2))
            Assert.False(range_b.Contains(9))
            
            // Test GetRangeValues method
            values_a <- range_a.ToArray()
            Assert.Equal(5, values_a.Length)
            Assert.Equal(1, values_a[0])
            Assert.Equal(5, values_a[4])
            
            values_b <- range_b.ToArray()
            Assert.Equal(6, values_b.Length)
            Assert.Equal(3, values_b[0])
            Assert.Equal(8, values_b[5])
            
            // Test the first element
            Assert.Equal(1, range_a.First())
            Assert.Equal(3, range_b.First())
            
            // Test the last element
            Assert.Equal(5, range_a.Last())
            Assert.Equal(8, range_b.Last())
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