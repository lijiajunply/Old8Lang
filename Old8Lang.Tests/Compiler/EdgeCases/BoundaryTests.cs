using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.EdgeCases;

/// <summary>
/// 编译器模式下的边界和错误情况测试
/// </summary>
public class BoundaryTests
{
    private readonly ITestOutputHelper _output;

    public BoundaryTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void EmptyInput_CompilesAndExecutesCorrectly()
    {
        // Arrange - 完全空的输入
        var code = @"";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 空输入应该能正常编译和执行
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void WhitespaceOnlyInput_CompilesAndExecutesCorrectly()
    {
        // Arrange - 只有空白字符的输入
        var code = @"
   
   
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
    public void CommentsOnlyInput_CompilesAndExecutesCorrectly()
    {
        // Arrange - 只有注释的输入
        var code = @"
            // This is a comment
            // Another comment
            // Final comment
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
    public void MaximumIntegerValue_CompilesAndExecutesCorrectly()
    {
        // Arrange - 测试极大整数值
        var code = @"
            max_int <- 9223372036854775807  // 接近 int64 最大值
            large_num <- 999999999999999999
            
            result1 <- max_int > 0
            result2 <- large_num > 0
            
            Assert.Equal(true, result1)
            Assert.Equal(true, result2)
            Assert.Equal(9223372036854775807, max_int)
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
    public void MinimumIntegerValue_CompilesAndExecutesCorrectly()
    {
        // Arrange - 测试极小整数值
        var code = @"
            min_int <- -9223372036854775808  // int64 最小值
            large_negative <- -999999999999999999
            
            result1 <- min_int < 0
            result2 <- large_negative < 0
            
            Assert.Equal(true, result1)
            Assert.Equal(true, result2)
            Assert.Equal(-9223372036854775808, min_int)
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
    public void MaximumDoubleValue_CompilesAndExecutesCorrectly()
    {
        // Arrange - 测试极大浮点数值
        var code = @"
            max_double <- 1.7976931348623157E308  // 接近 double 最大值
            min_double <- 2.2250738585072014E-308  // 接近 double 最小正值
            
            result1 <- max_double > 0
            result2 <- min_double > 0
            result3 <- max_double > min_double
            
            Assert.Equal(true, result1)
            Assert.Equal(true, result2)
            Assert.Equal(true, result3)
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
    public void DeepNesting_CompilesAndExecutesCorrectly()
    {
        // Arrange - 测试深度嵌套
        var code = @"
            result <- 0
            try {
                try {
                    try {
                        try {
                            try {
                                result <- result + 1
                                if true { throw ""deep error"" }
                            } catch (e) {
                                result <- result + 10
                            }
                        } catch (e) {
                            result <- result + 100
                        }
                    } catch (e) {
                        result <- result + 1000
                    }
                } catch (e) {
                    result <- result + 10000
                }
            } catch (e) {
                result <- result + 100000
            }
            Assert.Equal(100011, result)  // 1 + 10 + 100 + 1000 + 100000
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
    public void LargeArray_CompilesAndExecutesCorrectly()
    {
        // Arrange - 测试大数组
        var code = @"
            // 创建一个较大的数组
            large_array <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
                          11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                          21, 22, 23, 24, 25, 26, 27, 28, 29, 30]
            
            length <- large_array.Length
            first <- large_array[0]
            last <- large_array[length - 1]
            
            Assert.Equal(30, length)
            Assert.Equal(1, first)
            Assert.Equal(30, last)
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
    public void LargeList_CompilesAndExecutesCorrectly()
    {
        // Arrange - 测试大列表
        var code = @"
            // 创建一个较大的列表
            large_list <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
                         11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                         21, 22, 23, 24, 25, 26, 27, 28, 29, 30}
            
            count <- large_list.Count()
            sum <- 0
            i <- 0
            while i < count {
                sum <- sum + large_list[i]
                i <- i + 1
            }
            
            Assert.Equal(30, count)
            Assert.Equal(465, sum)  // sum of 1 to 30
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
    public void ZeroDivision_CompilesAndExecutesCorrectly()
    {
        // Arrange - 测试零除法
        var code = @"
            result1 <- 0
            result2 <- 0
            result3 <- 0
            
            try {
                result1 <- 10 / 0
            } catch (e) {
                result1 <- -1
            }
            
            try {
                result2 <- 10.0 / 0.0
            } catch (e) {
                result2 <- -2
            }
            
            try {
                result3 <- 10 % 0
            } catch (e) {
                result3 <- -3
            }
            
            Assert.Equal(-1, result1)
            Assert.Equal(-2, result2)
            Assert.Equal(-3, result3)
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
    public void ComplexBooleanExpression_CompilesAndExecutesCorrectly()
    {
        // Arrange - 测试复杂布尔表达式
        var code = @"
            a <- true
            b <- false
            c <- true
            d <- false
            
            // 复杂的逻辑表达式
            result1 <- a and b or c and d
            result2 <- (a or b) and (c or d)
            result3 <- a xor b xor c xor d
            result4 <- not (a and b) or not (c and d)
            
            Assert.Equal(true, result1)   // true and false or true and false = false or true = true
            Assert.Equal(true, result2)   // (true or false) and (true or false) = true and true = true
            Assert.Equal(true, result3)   // true xor false xor true xor false = true xor true xor false = false xor false = true
            Assert.Equal(true, result4)   // not(false) or not(false) = true or true = true
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
    public void DeepRecursion_CompilesAndExecutesCorrectly()
    {
        // Arrange - 测试深度递归（受限于实际实现）
        var code = @"
            func recursiveSum(n:int) -> int {
                if n <= 0 {
                    return 0
                }
                return n + recursiveSum(n - 1)
            }
            
            // 测试适度递归深度
            result <- recursiveSum(10)
            Assert.Equal(55, result)  // sum of 1 to 10
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
    public void NullOperations_CompilesAndExecutesCorrectly()
    {
        // Arrange - 测试 null 操作
        var code = @"
            null_var <- null
            result1 <- null_var == null
            result2 <- null_var != null
            result3 <- null_var == 0
            result4 <- null_var == """"
            
            Assert.Equal(true, result1)
            Assert.Equal(false, result2)
            Assert.Equal(false, result3)
            Assert.Equal(false, result4)
            
            // 测试 null 的字符串转换
            str_result <- null_var.ToStr()
            Assert.Equal(""null"", str_result)
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
    public void StringBoundaryOperations_CompilesAndExecutesCorrectly()
    {
        // Arrange - 测试字符串边界操作
        var code = @"
            empty_string <- """"
            single_char <- ""a""
            long_string <- ""This is a very long string that contains many words and characters""
            
            result1 <- empty_string.Length()
            result2 <- single_char.Length()
            result3 <- long_string.Length()
            result4 <- empty_string == """"
            result5 <- single_char != """"
            
            Assert.Equal(0, result1)
            Assert.Equal(1, result2)
            Assert.True(result3 > 0)
            Assert.Equal(true, result4)
            Assert.Equal(true, result5)
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