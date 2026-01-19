using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.EdgeCases;

[Collection("Sequential")]
public class VMExtremeValuesTests
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

    #region 极大数值测试

    [Fact]
    public void ExtremeValues_LargeIntAddition_HandlesLargeAddition()
    {
        var code = @"
            a <- 999999999999999999
            b <- 1
            result <- a + b
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("1000000000000000000", output);
    }

    [Fact]
    public void ExtremeValues_LargeIntMultiplication_HandlesLargeMultiplication()
    {
        var code = @"
            a <- 1000000
            b <- 1000000
            result <- a * b
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("1000000000000", output);
    }

    [Fact]
    public void ExtremeValues_DoublePrecision_HandlesPrecision()
    {
        var code = @"
            a <- 3.141592653589793
            b <- 2.718281828459045
            result <- a + b
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("5.859874", output);
    }

    #endregion

    #region 深层嵌套测试

    [Fact]
    public void ExtremeValues_DeeplyNestedFunctionCalls_HandlesDeepNesting()
    {
        var code = @"
            func add1(x:int) -> int {
                return x + 1
            }

            func add2(x:int) -> int {
                return add1(add1(x))
            }

            func add4(x:int) -> int {
                return add2(add2(x))
            }

            func add8(x:int) -> int {
                return add4(add4(x))
            }

            result <- add8(0)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("8", output);
    }

    [Fact]
    public void ExtremeValues_DeeplyNestedClasses_HandlesDeepNesting()
    {
        var code = @"
            class Outer {
                public class Middle {
                    public class Inner {
                        public value:int
                    }
                }
            }

            outer <- Outer()
            middle <- Outer.Middle()
            inner <- Outer.Middle.Inner()
            inner.value <- 42
            PrintLine(inner.value.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("42", output);
    }

    [Fact]
    public void ExtremeValues_DeeplyNestedArrays_HandlesDeepNesting()
    {
        var code = @"
            deepArray <- [[[1]]]
            value <- deepArray[0][0][0]
            PrintLine(value.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("1", output);
    }

    [Fact]
    public void ExtremeValues_DeeplyNestedDictionaries_HandlesDeepNesting()
    {
        var code = @"
            deepDict <- {""level1"": {""level2"": {""level3"": 99}}}
            value <- deepDict[""level1""][""level2""][""level3""]
            PrintLine(value.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("99", output);
    }

    #endregion

    #region 大数据结构测试

    [Fact]
    public void ExtremeValues_LargeArray_HandlesLargeArray()
    {
        var code = @"
            largeArray <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
            result <- len(largeArray)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("10", output);
    }

    [Fact]
    public void ExtremeValues_LargeList_HandlesLargeList()
    {
        var code = @"
            largeList <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            result <- len(largeList)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("10", output);
    }

    [Fact]
    public void ExtremeValues_LargeDictionary_HandlesLargeDictionary()
    {
        var code = @"
            largeDict <- {""a"": 1, ""b"": 2, ""c"": 3, ""d"": 4, ""e"": 5}
            result <- len(largeDict)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("5", output);
    }

    [Fact]
    public void ExtremeValues_LargeString_HandlesLargeString()
    {
        var code = @"
            largeString <- ""Hello, World! Hello, World! Hello, World!""
            result <- len(largeString)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("41", output);
    }

    #endregion

    #region 复杂表达式测试

    [Fact]
    public void ExtremeValues_ComplexArithmetic_HandlesComplexExpression()
    {
        var code = @"
            result <- ((1 + 2) * (3 + 4)) / (5 - 2)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("7", output);
    }

    [Fact]
    public void ExtremeValues_ComplexBoolean_HandlesComplexExpression()
    {
        var code = @"
            a <- true
            b <- false
            c <- true
            result <- (a && b) || (c && !b)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("true", output);
    }

    [Fact]
    public void ExtremeValues_ComplexStringConcat_HandlesComplexConcat()
    {
        var code = @"
            a <- ""Hello""
            b <- "" ""
            c <- ""World""
            d <- ""!""
            result <- a + b + c + d
            PrintLine(result)
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("Hello World!", output);
    }

    [Fact]
    public void ExtremeValues_ComplexTernary_HandlesComplexTernary()
    {
        var code = @"
            x <- 5
            result <- (x > 10) ? ""large"" : ((x > 5) ? ""medium"" : ""small"")
            PrintLine(result)
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("small", output);
    }

    #endregion

    #region 极限循环测试

    [Fact]
    public void ExtremeValues_LargeLoopCount_HandlesLargeLoop()
    {
        var code = @"
            sum <- 0
            for i <- 0, i < 100, i++ {
                sum <- sum + i
            }
            PrintLine(sum.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("4950", output);
    }

    [Fact]
    public void ExtremeValues_NestedLoops_HandlesNestedLoops()
    {
        var code = @"
            count <- 0
            for i <- 0, i < 3, i++ {
                for j <- 0, j < 3, j++ {
                    count <- count + 1
                }
            }
            PrintLine(count.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("9", output);
    }

    [Fact]
    public void ExtremeValues_LoopWithBreak_HandlesBreak()
    {
        var code = @"
            sum <- 0
            for i <- 0, i < 100, i++ {
                sum <- sum + i
                if i >= 10 {
                    break
                }
            }
            PrintLine(sum.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("55", output);
    }

    #endregion

    #region 复杂数据操作测试

    [Fact]
    public void ExtremeValues_ComplexArrayOperations_HandlesComplexOps()
    {
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            sum <- 0
            for i <- 0, i < len(arr), i++ {
                sum <- sum + arr[i]
            }
            PrintLine(sum.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("15", output);
    }

    [Fact]
    public void ExtremeValues_ComplexDictionaryOperations_HandlesComplexOps()
    {
        var code = @"
            dict <- {""a"": 10, ""b"": 20, ""c"": 30}
            sum <- 0
            for key in dict {
                sum <- sum + dict[key]
            }
            PrintLine(sum.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("60", output);
    }

    [Fact]
    public void ExtremeValues_StringManipulation_HandlesComplexManipulation()
    {
        var code = @"
            str <- ""Hello, World!""
            upper <- str.ToUpper()
            PrintLine(upper)
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("HELLO, WORLD!", output);
    }

    #endregion

    #region 多变量复杂交互测试

    [Fact]
    public void ExtremeValues_MultipleVariables_HandlesInteraction()
    {
        var code = @"
            a <- 10
            b <- 20
            c <- 30
            d <- a + b
            e <- d * c
            f <- e / 10
            PrintLine(f.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("90", output);
    }

    [Fact]
    public void ExtremeValues_VariableReassignment_HandlesReassignment()
    {
        var code = @"
            x <- 10
            x <- x + 10
            x <- x * 2
            PrintLine(x.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("40", output);
    }

    #endregion

    #region 极端类型转换测试

    [Fact]
    public void ExtremeValues_IntToDoubleConversion_HandlesConversion()
    {
        var code = @"
            intValue <- 42
            doubleValue <- double(intValue)
            PrintLine(doubleValue.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("42", output);
    }

    [Fact]
    public void ExtremeValues_DoubleToIntConversion_HandlesConversion()
    {
        var code = @"
            doubleValue <- 42.7
            intValue <- int(doubleValue)
            PrintLine(intValue.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("42", output);
    }

    #endregion

    #region 复杂条件测试

    [Fact]
    public void ExtremeValues_ComplexIfElse_HandlesComplexConditions()
    {
        var code = @"
            x <- 15
            if x < 10 {
                PrintLine(""small"")
            } elif x < 20 {
                PrintLine(""medium"")
            } else {
                PrintLine(""large"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("medium", output);
    }

    [Fact]
    public void ExtremeValues_NestedIf_HandlesNestedConditions()
    {
        var code = @"
            x <- 10
            y <- 20
            if x > 5 {
                if y > 15 {
                    PrintLine(""both"")
                }
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("both", output);
    }

    #endregion
}
