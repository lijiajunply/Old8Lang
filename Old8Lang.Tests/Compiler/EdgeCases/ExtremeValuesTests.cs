using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.EdgeCases;

/// <summary>
/// 编译器模式下的边界和错误情况测试 - 极值测试
/// </summary>
public class ExtremeValuesTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void MaximumIntegerValue_CompilesAndExecutesCorrectly()
    {
        var code = @"
            maxInt <- 2147483647
            Assert.Equal(2147483647, maxInt)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MinimumIntegerValue_CompilesAndExecutesCorrectly()
    {
        var code = @"
            minInt <- -2147483648
            Assert.Equal(-2147483648, minInt)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MaximumDoubleValue_CompilesAndExecutesCorrectly()
    {
        var code = @"
            maxDouble <- 1.7976931348623157e308
            Assert.True(maxDouble > 1.7e308)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MinimumDoubleValue_CompilesAndExecutesCorrectly()
    {
        var code = @"
            minDouble <- -1.7976931348623157e308
            Assert.True(minDouble < -1.7e308)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ZeroValue_CompilesAndExecutesCorrectly()
    {
        var code = @"
            intZero <- 0
            doubleZero <- 0.0
            boolFalse <- false
            
            Assert.Equal(0, intZero)
            Assert.Equal(0.0, doubleZero)
            Assert.Equal(false, boolFalse)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ExtremeStringLength_CompilesAndExecutesCorrectly()
    {
        var code = @"
            longStr <- ""a"" * 1000
            Assert.Equal(1000, longStr.Length())
            Assert.Equal(""a"", longStr[0])
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void LargeArraySize_CompilesAndExecutesCorrectly()
    {
        var code = @"
            largeArray <- []
            i <- 0
            while i < 1000 {
                largeArray.Add(i)
                i <- i + 1
            }
            
            Assert.Equal(1000, largeArray.Length)
            Assert.Equal(0, largeArray[0])
            Assert.Equal(999, largeArray[999])
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void LargeListSize_CompilesAndExecutesCorrectly()
    {
        var code = @"
            largeList <- {}
            i <- 0
            while i < 1000 {
                largeList.Add(i)
                i <- i + 1
            }
            
            Assert.Equal(1000, largeList.Count())
            Assert.Equal(0, largeList[0])
            Assert.Equal(999, largeList[999])
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void DeepRecursion_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func factorial(n:int) -> int {
                if n <= 1 {
                    return 1
                }
                return n * factorial(n - 1)
            }
            
            result <- factorial(10)
            Assert.Equal(3628800, result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ExtremeLoopIterations_CompilesAndExecutesCorrectly()
    {
        var code = @"
            sum <- 0
            i <- 0
            while i < 10000 {
                sum <- sum + i
                i <- i + 1
            }
            
            expected <- 10000 * 9999 / 2
            Assert.Equal(expected, sum)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void VeryLargeNumber_CompilesAndExecutesCorrectly()
    {
        var code = @"
            bigNumber <- 1000000000000000000
            Assert.Equal(1000000000000000000, bigNumber)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void VerySmallNumber_CompilesAndExecutesCorrectly()
    {
        var code = @"
            smallNumber <- 0.0000000001
            Assert.True(smallNumber < 0.000001)
            Assert.True(smallNumber > 0)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MaximumCharValue_CompilesAndExecutesCorrectly()
    {
        var code = @"
            maxChar <- 'z'
            Assert.Equal('z', maxChar)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void BooleanTrueFalse_CompilesAndExecutesCorrectly()
    {
        var code = @"
            trueValue <- true
            falseValue <- false
            
            Assert.Equal(true, trueValue)
            Assert.Equal(false, falseValue)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ScientificNotationExtreme_CompilesAndExecutesCorrectly()
    {
        var code = @"
            huge <- 1e100
            tiny <- 1e-100
            
            Assert.True(huge > 1e99)
            Assert.True(tiny < 1e-99)
            Assert.True(tiny > 0)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void OverflowAddition_CompilesAndExecutesCorrectly()
    {
        var code = @"
            maxInt <- 2147483647
            result <- maxInt + 1
            
            result <- result
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void UnderflowSubtraction_CompilesAndExecutesCorrectly()
    {
        var code = @"
            minInt <- -2147483648
            result <- minInt - 1
            
            result <- result
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ExtremePower_CompilesAndExecutesCorrectly()
    {
        var code = @"
            result <- 2.0.Pow(10.0)
            Assert.Equal(1024.0, result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void DivisionByExtremeValues_CompilesAndExecutesCorrectly()
    {
        var code = @"
            result1 <- 1.0 / 1000000000.0
            result2 <- 1000000000.0 / 0.000000001
            
            Assert.True(result1 > 0)
            Assert.True(result2 > 0)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
