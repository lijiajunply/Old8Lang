using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Expressions;

/// <summary>
/// 虚拟机比较和逻辑表达式测试
/// 测试虚拟机执行比较运算和逻辑运算的正确性
/// </summary>
[Collection("Sequential")]
public class VMComparisonLogicalTests
{
    /// <summary>
    /// 执行虚拟机代码并捕获控制台输出
    /// </summary>
    private string ExecuteVMCode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // 编译为字节码
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        // 捕获控制台输出
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // 执行字节码
            var vm = new Bytecode.VM.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #region 比较运算测试

    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(5, 10, false)]
    [InlineData(5, 5, false)]
    public void GreaterThan_ExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a} > {b}
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(expected.ToString().ToLower(), output);
    }

    [Theory]
    [InlineData(10, 5, false)]
    [InlineData(5, 10, true)]
    [InlineData(5, 5, false)]
    public void LessThan_ExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a} < {b}
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(expected.ToString().ToLower(), output);
    }

    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(5, 10, false)]
    [InlineData(5, 5, true)]
    public void GreaterThanOrEqual_ExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a} >= {b}
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(expected.ToString().ToLower(), output);
    }

    [Theory]
    [InlineData(10, 5, false)]
    [InlineData(5, 10, true)]
    [InlineData(5, 5, true)]
    public void LessThanOrEqual_ExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a} <= {b}
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(expected.ToString().ToLower(), output);
    }

    [Theory]
    [InlineData(5, 5, true)]
    [InlineData(5, 10, false)]
    [InlineData(0, 0, true)]
    public void Equality_ExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a} == {b}
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(expected.ToString().ToLower(), output);
    }

    [Theory]
    [InlineData(5, 5, false)]
    [InlineData(5, 10, true)]
    [InlineData(0, 1, true)]
    public void NotEqual_ExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a} != {b}
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(expected.ToString().ToLower(), output);
    }

    [Fact]
    public void StringComparison_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            str1 <- ""hello""
            str2 <- ""hello""
            str3 <- ""world""
            equal <- str1 == str2
            notEqual <- str1 != str3
            PrintLine(equal.ToStr())
            PrintLine(notEqual.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("true", lines[0]);
        Assert.Equal("true", lines[1]);
    }

    [Fact]
    public void BooleanComparison_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            bool1 <- true
            bool2 <- false
            bool3 <- true
            equal <- bool1 == bool3
            notEqual <- bool1 != bool2
            PrintLine(equal.ToStr())
            PrintLine(notEqual.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("true", lines[0]);
        Assert.Equal("true", lines[1]);
    }

    #endregion

    #region 逻辑运算测试

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void LogicalAnd_ExecutesCorrectly(bool a, bool b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a.ToString().ToLower()} && {b.ToString().ToLower()}
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(expected.ToString().ToLower(), output);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void LogicalOr_ExecutesCorrectly(bool a, bool b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a.ToString().ToLower()} || {b.ToString().ToLower()}
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(expected.ToString().ToLower(), output);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void LogicalNot_ExecutesCorrectly(bool value, bool expected)
    {
        // Arrange
        var code = $@"
            result <- !{value.ToString().ToLower()}
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(expected.ToString().ToLower(), output);
    }

    [Fact]
    public void ComplexLogicalExpression_ExecutesCorrectly()
    {
        // Arrange - (true && false) || (true && true) = false || true = true
        var code = @"
            result <- (true && false) || (true && true)
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("true", output);
    }

    [Fact]
    public void LogicalWithComparison_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 5
            c <- 15
            result <- (a > b) && (c > a)
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("true", output); // (10 > 5) && (15 > 10) = true && true = true
    }

    [Fact]
    public void LogicalShortCircuit_And_ExecutesCorrectly()
    {
        // Arrange - 测试短路求值：false && (任何表达式) 应该直接返回 false
        var code = @"
            a <- 5
            b <- 10
            result <- (a > b) && (b > 0)
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("false", output); // (5 > 10) && (10 > 0) = false && true = false
    }

    [Fact]
    public void LogicalShortCircuit_Or_ExecutesCorrectly()
    {
        // Arrange - 测试短路求值：true || (任何表达式) 应该直接返回 true
        var code = @"
            a <- 10
            b <- 5
            result <- (a > b) || (b > a)
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("true", output); // (10 > 5) || (5 > 10) = true || false = true
    }

    #endregion

    #region 混合表达式测试

    [Fact]
    public void ComparisonAndLogicalMixed_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            y <- 5
            z <- 15
            result1 <- (x > y) && (z > x)
            result2 <- (x < y) || (z > y)
            result3 <- !(x == y)
            PrintLine(result1.ToStr())
            PrintLine(result2.ToStr())
            PrintLine(result3.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("true", lines[0]);  // (10 > 5) && (15 > 10) = true && true = true
        Assert.Equal("true", lines[1]);  // (10 < 5) || (15 > 5) = false || true = true
        Assert.Equal("true", lines[2]);  // !(10 == 5) = !false = true
    }

    [Fact]
    public void NestedLogicalExpressions_ExecuteCorrectly()
    {
        // Arrange
        var code = @"
            a <- 1
            b <- 2
            c <- 3
            d <- 4
            result <- ((a < b) && (c < d)) || ((a > b) && (c > d))
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        // ((1 < 2) && (3 < 4)) || ((1 > 2) && (3 > 4))
        // = (true && true) || (false && false)
        // = true || false = true
        Assert.Equal("true", output);
    }

    [Fact]
    public void ComparisonWithArithmetic_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 5
            result1 <- (a + b) > (a - b)
            result2 <- (a * b) == (b * a)
            PrintLine(result1.ToStr())
            PrintLine(result2.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("true", lines[0]);  // (10 + 5) > (10 - 5) = 15 > 5 = true
        Assert.Equal("true", lines[1]);  // (10 * 5) == (5 * 10) = 50 == 50 = true
    }

    #endregion
}