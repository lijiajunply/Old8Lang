using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Collections.Advanced;

/// <summary>
/// 编译器模式下的高级集合功能测试 - 扩展范围
/// </summary>
public class ExtendedRangeTests
{
    private readonly ITestOutputHelper _output;

    public ExtendedRangeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void InclusiveRangeWithStep_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 包含范围，步长为2 [1..5]
            range <- [1..5:2]
            
            // 检查基本属性
            Assert.Equal(3, range.Length())
            Assert.Equal(1, range.Start())
            Assert.Equal(5, range.End())
            
            // 检查包含的方法
            Assert.True(range.Contains(1))
            Assert.True(range.Contains(3))
            Assert.True(range.Contains(5))
            Assert.False(range.Contains(2))
            Assert.False(range.Contains(4))
            
            // 获取所有值
            values <- range.ToArray()
            Assert.Equal([1, 3, 5], values)
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
    public void ExclusiveRangeWithStep_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 排除右端，步长为3 [1~5:3]
            range <- [1~5:3]
            
            // 检查基本属性
            Assert.Equal(2, range.Length())
            Assert.Equal(2, range.Start())  // 排除1，从2开始
            Assert.Equal(5, range.End())
            
            // 检查包含的方法
            Assert.False(range.Contains(1))  // 排除
            Assert.True(range.Contains(2))
            Assert.False(range.Contains(4))  // 2+3=5, 但5排除
            Assert.True(range.Contains(5))  // 最后一个元素
            
            // 获取所有值
            values <- range.ToArray()
            Assert.Equal([2, 5], values)
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
    public void CharacterRangeWithStep_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 字符范围，步长为1 ['a'..'f':1]
            range <- ['a'~'f':1]
            
            // 检查基本属性
            Assert.Equal(6, range.Length())
            Assert.Equal('a', range.Start())
            Assert.Equal('f', range.End())
            
            // 检查字符包含
            Assert.True(range.Contains('a'))
            Assert.True(range.Contains('d'))
            Assert.True(range.Contains('f'))
            Assert.False(range.Contains('g'))
            
            // 获取所有字符
            chars <- range.ToArray()
            Assert.Equal(['a', 'b', 'c', 'd', 'e', 'f'], chars)
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
    public void RangeWithNegativeStep_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 负步长范围 [10..1:-1]
            range <- [10..1:-1]
            
            // 检查基本属性
            Assert.Equal(10, range.Length())
            Assert.Equal(10, range.Start())
            Assert.Equal(1, range.End())
            
            // 检查前几个和最后几个元素
            Assert.True(range.Contains(10))
            Assert.True(range.Contains(5))
            Assert.True(range.Contains(1))
            
            // 获取所有值
            values <- range.ToArray()
            Assert.Equal([10, 9, 8, 7, 6, 5, 4, 3, 2, 1], values)
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
            // 空范围 [10..1] (默认正步长)
            emptyRange1 <- [10..1]
            
            // 另一个空范围 [1~1]
            emptyRange2 <- [1~1]
            
            Assert.Equal(0, emptyRange1.Length())
            Assert.Equal(0, emptyRange2.Length())
            
            // 空范围不应该包含任何值
            Assert.False(emptyRange1.Contains(5))
            Assert.False(emptyRange2.Contains(1))
            
            // 空范围的ToArray应该是空数组
            emptyValues1 <- emptyRange1.ToArray()
            emptyValues2 <- emptyRange2.ToArray()
            Assert.Equal([], emptyValues1)
            Assert.Equal([], emptyValues2)
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
    public void SingleElementRange_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 单元素范围 [5..5]
            singleRange <- [5..5]
            
            Assert.Equal(1, singleRange.Length())
            Assert.Equal(5, singleRange.Start())
            Assert.Equal(5, singleRange.End())
            Assert.True(singleRange.Contains(5))
            Assert.False(singleRange.Contains(4))
            
            values <- singleRange.ToArray()
            Assert.Equal([5], values)
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
    public void RangeWithLargeStep_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 大步长范围 [0..20:5]
            range <- [0..20:5]
            
            Assert.Equal(5, range.Length())
            Assert.Equal(0, range.Start())
            Assert.Equal(20, range.End())
            
            // 检查包含的值
            Assert.True(range.Contains(0))
            Assert.True(range.Contains(5))
            Assert.True(range.Contains(10))
            Assert.True(range.Contains(15))
            Assert.True(range.Contains(20))
            Assert.False(range.Contains(3))
            Assert.False(range.Contains(7))
            
            values <- range.ToArray()
            Assert.Equal([0, 5, 10, 15, 20], values)
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
            range <- [1..10]
            
            // First 和 Last 方法
            first <- range.First()
            last <- range.Last()
            
            Assert.Equal(1, first)
            Assert.Equal(10, last)
            
            // 边界索引访问
            firstByIndex <- range[0]
            lastByIndex <- range[9]
            
            Assert.Equal(1, firstByIndex)
            Assert.Equal(10, lastByIndex)
            
            // 越出边界的索引应该返回合适的值或处理
            // 这取决于具体实现
            allValues <- range.ToArray()
            Assert.Equal(10, allValues.Length)
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
    public void RangeWithFloatingPoint_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 浮点数范围 [0.0..1.0:0.25]
            range <- [0.0..1.0:0.25]
            
            // 检查基本属性
            Assert.True(range.Length() > 0)
            Assert.Equal(0.0, range.Start())
            Assert.Equal(1.0, range.End())
            
            // 检查包含的方法（浮点数比较）
            Assert.True(range.Contains(0.0))
            Assert.True(range.Contains(0.5))
            Assert.True(range.Contains(1.0))
            Assert.False(range.Contains(0.1))  // 不在序列中的值
            
            // 获取所有值
            values <- range.ToArray()
            // 应该包含: 0.0, 0.25, 0.5, 0.75, 1.0
            Assert.True(values.Length >= 4)  // 至少包含一些值
            Assert.Equal(0.0, values[0])
            Assert.Equal(1.0, values[values.Length - 1])
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
            // 左包含右排除 [1..<5]
            range1 <- [1..<5]
            // 左排除右包含 [1>~5]
            range2 <- [1>~5]
            
            // range1: [1, 2, 3, 4]
            Assert.Equal(4, range1.Length())
            Assert.True(range1.Contains(1))
            Assert.False(range1.Contains(5))
            
            // range2: [2, 3, 4, 5]
            Assert.Equal(4, range2.Length())
            Assert.False(range2.Contains(1))
            Assert.True(range2.Contains(5))
            
            values1 <- range1.ToArray()
            values2 <- range2.ToArray()
            
            Assert.Equal([1, 2, 3, 4], values1)
            Assert.Equal([2, 3, 4, 5], values2)
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
    public void ComplexRangeIteration_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 复杂范围操作
            range1 <- [1..5]
            range2 <- [10..20:5]
            range3 <- ['a'..'e']
            
            // 收集所有值
            allNumbers <- {}
            allChars <- {}
            
            // 迭代第一个范围
            i <- 0
            while i < range1.Length() {
                allNumbers.Add(range1[i])
                i <- i + 1
            }
            
            // 迭代第二个范围
            i <- 0
            while i < range2.Length() {
                allNumbers.Add(range2[i])
                i <- i + 1
            }
            
            // 迭代第三个范围
            i <- 0
            while i < range3.Length() {
                allChars.Add(range3[i])
                i <- i + 1
            }
            
            // 验证结果
            Assert.Equal(5 + 3 + 5, allNumbers.Count())  // range1(5) + range2(3) = 8
            Assert.Equal(5, allChars.Count())
            
            // 验证范围1的值
            Assert.True(allNumbers.Contains(1))
            Assert.True(allNumbers.Contains(5))
            Assert.True(allNumbers.Contains(10))
            Assert.True(allNumbers.Contains(20))
            
            // 验证字符
            Assert.Equal(['a', 'b', 'c', 'd', 'e'], allChars)
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
            range <- [1..20]
            
            // 手动过滤偶数
            evens <- {}
            odds <- {}
            
            i <- 0
            while i < range.Length() {
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
            
            // 计算总和
            evenSum <- 0
            oddSum <- 0
            i <- 0
            while i < evens.Count() {
                evenSum <- evenSum + evens[i]
                i <- i + 1
            }
            i <- 0
            while i < odds.Count() {
                oddSum <- oddSum + odds[i]
                i <- i + 1
            }
            
            // 2+4+...+20 = 110, 1+3+...+19 = 100
            Assert.Equal(110, evenSum)
            Assert.Equal(100, oddSum)
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
    public void RangePerformance_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 性能测试：创建一个较大的范围
            largeRange <- [1..1000]
            
            // 基本属性检查
            Assert.Equal(1000, largeRange.Length())
            Assert.Equal(1, largeRange.Start())
            Assert.Equal(1000, largeRange.End())
            
            // 边界值检查
            Assert.True(largeRange.Contains(1))
            Assert.True(largeRange.Contains(500))
            Assert.True(largeRange.Contains(1000))
            Assert.False(largeRange.Contains(0))
            Assert.False(largeRange.Contains(1001))
            
            // 检查首尾元素
            first <- largeRange.First()
            last <- largeRange.Last()
            Assert.Equal(1, first)
            Assert.Equal(1000, last)
            
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
}