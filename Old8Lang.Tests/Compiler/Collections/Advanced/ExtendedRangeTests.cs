using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Collections.Advanced;

/// <summary>
/// 编译器模式下的高级集合功能测试 - 扩展范围
/// 注意：当前 Range 实现在编译器模式下会直接转换为数组，不是专门的 Range 对象
/// </summary>
public class ExtendedRangeTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void InclusiveRange_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 包含范围 [1~5]
            range <- [1~5]

            // 验证数组内容
            Assert.Equal(5, range.Length)
            Assert.Equal(1, range[0])
            Assert.Equal(2, range[1])
            Assert.Equal(3, range[2])
            Assert.Equal(4, range[3])
            Assert.Equal(5, range[4])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ExclusiveEndRange_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 排除右端 [1~<5]
            range <- [1~<5]

            // 验证数组内容: [1, 2, 3, 4]
            Assert.Equal(4, range.Length)
            Assert.Equal(1, range[0])
            Assert.Equal(2, range[1])
            Assert.Equal(3, range[2])
            Assert.Equal(4, range[3])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ExclusiveStartRange_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 排除左端 [1>~5]
            range <- [1>~5]

            // 验证数组内容: [2, 3, 4, 5]
            Assert.Equal(4, range.Length)
            Assert.Equal(2, range[0])
            Assert.Equal(3, range[1])
            Assert.Equal(4, range[2])
            Assert.Equal(5, range[3])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ExclusiveBothRange_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 排除两端 [1>~<5]
            range <- [1>~<5]

            // 验证数组内容: [2, 3, 4]
            Assert.Equal(3, range.Length)
            Assert.Equal(2, range[0])
            Assert.Equal(3, range[1])
            Assert.Equal(4, range[2])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void SingleElementInclusive_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 单元素范围 [3~3]
            range <- [3~3]

            Assert.Equal(1, range.Length)
            Assert.Equal(3, range[0])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void NegativeNumbers_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 负数范围 [-2>~<2]
            range <- [-2>~<2]

            // 验证数组内容: [-1, 0, 1]
            Assert.Equal(3, range.Length)
            Assert.Equal(-1, range[0])
            Assert.Equal(0, range[1])
            Assert.Equal(1, range[2])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void VariablesInRange_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            start <- 10
            end <- 15
            result1 <- [start~<end]
            result2 <- [start>~end]

            // result1: [10, 11, 12, 13, 14]
            Assert.Equal(5, result1.Length)
            Assert.Equal(10, result1[0])
            Assert.Equal(14, result1[4])

            // result2: [11, 12, 13, 14, 15]
            Assert.Equal(5, result2.Length)
            Assert.Equal(11, result2[0])
            Assert.Equal(15, result2[4])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void RangeIteration_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 范围迭代测试
            range1 <- [1~5]
            range2 <- [10~12]

            // 计算 range1 的总和
            sum1 <- 0
            i <- 0
            while i < range1.Length {
                sum1 <- sum1 + range1[i]
                i <- i + 1
            }

            // 计算 range2 的总和
            sum2 <- 0
            i <- 0
            while i < range2.Length {
                sum2 <- sum2 + range2[i]
                i <- i + 1
            }

            // 1+2+3+4+5 = 15
            Assert.Equal(15, sum1)
            // 10+11+12 = 33
            Assert.Equal(33, sum2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void RangeWithConditionalFiltering_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 范围与条件过滤结合
            range <- [1~20]

            // 手动过滤偶数和奇数
            evens <- {}
            odds <- {}

            i <- 0
            while i < range.Length {
                value <- range[i]
                if value % 2 == 0 {
                    evens.Add(value)
                } else {
                    odds.Add(value)
                }
                i <- i + 1
            }

            Assert.Equal(10, evens.Count())
            Assert.Equal(10, odds.Count())

            // 检查一些具体的值
            Assert.True(evens.Contains(2))
            Assert.True(evens.Contains(20))
            Assert.True(odds.Contains(1))
            Assert.True(odds.Contains(19))
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void LargeRange_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 性能测试：创建一个较大的范围
            largeRange <- [1~1000]

            // 基本属性检查
            Assert.Equal(1000, largeRange.Length)
            Assert.Equal(1, largeRange[0])
            Assert.Equal(1000, largeRange[999])

            // 部分求和测试（前100个元素）
            partialSum <- 0
            i <- 0
            while i < 100 {
                partialSum <- partialSum + largeRange[i]
                i <- i + 1
            }
            // 1+2+...+100 = 5050
            Assert.Equal(5050, partialSum)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyRange_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 降序范围 [10~1]
            emptyRange1 <- [10~1]

            // 这是降序范围，应该包含: 10, 9, 8, 7, 6, 5, 4, 3, 2, 1
            Assert.Equal(10, emptyRange1.Length)
            Assert.Equal(10, emptyRange1[0])
            Assert.Equal(1, emptyRange1[9])

            // 排除两端 [1>~<1]
            // 1 (不包含) ~ 1 (不包含) => 2 ~ 0 (降序) => [2, 1, 0]
            emptyRange2 <- [1>~<1]

            Assert.Equal(3, emptyRange2.Length)
            Assert.Equal(2, emptyRange2[0])
            Assert.Equal(1, emptyRange2[1])
            Assert.Equal(0, emptyRange2[2])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void RangeBoundaryOperations_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 边界测试范围
            range <- [1~10]

            // 边界索引访问
            firstByIndex <- range[0]
            lastByIndex <- range[9]

            Assert.Equal(1, firstByIndex)
            Assert.Equal(10, lastByIndex)

            Assert.Equal(10, range.Length)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void RangeWithMixedExclusions_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 左包含右排除 [1~<5]
            range1 <- [1~<5]
            // 左排除右包含 [1>~5]
            range2 <- [1>~5]

            // range1: [1, 2, 3, 4]
            Assert.Equal(4, range1.Length)
            Assert.Equal(1, range1[0])
            Assert.Equal(4, range1[3])

            // range2: [2, 3, 4, 5]
            Assert.Equal(4, range2.Length)
            Assert.Equal(2, range2[0])
            Assert.Equal(5, range2[3])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
