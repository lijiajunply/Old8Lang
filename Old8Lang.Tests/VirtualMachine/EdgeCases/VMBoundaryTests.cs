using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.EdgeCases;

[Collection("Sequential")]
public class VMBoundaryTests
{
    private string ExecuteVMCode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            var vm = new Old8Lang.Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #region 数值边界测试

    [Fact]
    public void Boundary_IntZero_HandlesZeroValue()
    {
        var code = @"
            x <- 0
            result <- x * 10
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("0", output);
    }

    [Fact]
    public void Boundary_IntMax_HandlesMaxInt()
    {
        var code = @"
            maxInt <- 2147483647
            result <- maxInt + 0
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("2147483647", output);
    }

    [Fact]
    public void Boundary_IntMin_HandlesMinInt()
    {
        var code = @"
            minInt <- -2147483648
            result <- minInt + 0
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("-2147483648", output);
    }

    [Fact]
    public void Boundary_DoubleZero_HandlesDoubleZero()
    {
        var code = @"
            x <- 0.0
            result <- x * 3.14
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("0", output);
    }

    [Fact]
    public void Boundary_DoubleInfinity_HandlesInfinity()
    {
        var code = @"
            x <- 1.7976931348623157e+308
            result <- x > 0
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("true", output);
    }

    [Fact]
    public void Boundary_DoubleNaN_HandlesNaN()
    {
        var code = @"
            x <- 0.0 / 0.0
            result <- double.IsNaN(x)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("True", output);
    }

    [Fact]
    public void Boundary_BooleanTrue_HandlesTrue()
    {
        var code = @"
            flag <- true
            result <- flag == true
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("true", output);
    }

    [Fact]
    public void Boundary_BooleanFalse_HandlesFalse()
    {
        var code = @"
            flag <- false
            result <- flag == false
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("true", output);
    }

    #endregion

    #region 字符串边界测试

    [Fact]
    public void Boundary_EmptyString_HandlesEmptyString()
    {
        var code = @"
            emptyString <- """"
            result <- len(emptyString)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("0", output);
    }

    [Fact]
    public void Boundary_SingleCharString_HandlesSingleChar()
    {
        var code = @"
            singleChar <- ""A""
            result <- len(singleChar)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("1", output);
    }

    [Fact]
    public void Boundary_LongString_HandlesLongString()
    {
        var code = @"
            longString <- ""Hello, World! This is a longer string.""
            result <- len(longString)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("41", output);
    }

    [Fact]
    public void Boundary_StringConcat_HandlesConcat()
    {
        var code = @"
            str1 <- ""Hello""
            str2 <- ""World""
            result <- str1 + "" "" + str2
            PrintLine(result)
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("Hello World", output);
    }

    #endregion

    #region 集合边界测试

    [Fact]
    public void Boundary_EmptyArray_HandlesEmptyArray()
    {
        var code = @"
            emptyArray <- []
            result <- len(emptyArray)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("0", output);
    }

    [Fact]
    public void Boundary_SingleElementArray_HandlesSingleElement()
    {
        var code = @"
            singleArray <- [42]
            result <- len(singleArray)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("1", output);
    }

    [Fact]
    public void Boundary_EmptyList_HandlesEmptyList()
    {
        var code = @"
            emptyList <- {}
            result <- len(emptyList)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("0", output);
    }

    [Fact]
    public void Boundary_SingleElementList_HandlesSingleElement()
    {
        var code = @"
            singleList <- {1}
            result <- len(singleList)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("1", output);
    }

    [Fact]
    public void Boundary_EmptyDictionary_HandlesEmptyDictionary()
    {
        var code = @"
            emptyDict <- {}
            result <- len(emptyDict)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("0", output);
    }

    [Fact]
    public void Boundary_SingleElementDictionary_HandlesSingleElement()
    {
        var code = @"
            singleDict <- {""key"": ""value""}
            result <- len(singleDict)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("1", output);
    }

    #endregion

    #region 循环边界测试

    [Fact]
    public void Boundary_ZeroIterationLoop_HandlesZeroLoop()
    {
        var code = @"
            count <- 0
            for i <- 0, i < 0, i++ {
                count <- count + 1
            }
            PrintLine(count.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("0", output);
    }

    [Fact]
    public void Boundary_SingleIterationLoop_HandlesOneLoop()
    {
        var code = @"
            count <- 0
            for i <- 0, i < 1, i++ {
                count <- count + 1
            }
            PrintLine(count.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("1", output);
    }

    [Fact]
    public void Boundary_WhileLoopZeroIteration_HandlesZeroLoop()
    {
        var code = @"
            count <- 0
            while false {
                count <- count + 1
            }
            PrintLine(count.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("0", output);
    }

    [Fact]
    public void Boundary_WhileLoopBreak_HandlesBreak()
    {
        var code = @"
            count <- 0
            while true {
                count <- count + 1
                if count >= 3 {
                    break
                }
            }
            PrintLine(count.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("3", output);
    }

    #endregion

    #region 范围边界测试

    [Fact]
    public void Boundary_EmptyRange_HandlesEmptyRange()
    {
        var code = @"
            emptyRange <- [5~5]
            count <- 0
            for x <- emptyRange {
                count <- count + 1
            }
            PrintLine(count.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("0", output);
    }

    [Fact]
    public void Boundary_SingleElementRange_HandlesSingleElement()
    {
        var code = @"
            singleRange <- [5~6]
            count <- 0
            for x <- singleRange {
                count <- count + 1
            }
            PrintLine(count.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("1", output);
    }

    [Fact]
    public void Boundary_ReverseRange_HandlesReverse()
    {
        var code = @"
            reverseRange <- [10~5]
            count <- 0
            for x <- reverseRange {
                count <- count + 1
            }
            PrintLine(count.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("5", output);
    }

    #endregion

    #region 函数调用边界测试

    [Fact]
    public void Boundary_FunctionNoParameters_HandlesNoParams()
    {
        var code = @"
            func greet() -> string {
                return ""Hello""
            }
            result <- greet()
            PrintLine(result)
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("Hello", output);
    }

    [Fact]
    public void Boundary_FunctionManyParameters_HandlesManyParams()
    {
        var code = @"
            func sum(a:int, b:int, c:int, d:int, e:int) -> int {
                return a + b + c + d + e
            }
            result <- sum(1, 2, 3, 4, 5)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("15", output);
    }

    #endregion
}
