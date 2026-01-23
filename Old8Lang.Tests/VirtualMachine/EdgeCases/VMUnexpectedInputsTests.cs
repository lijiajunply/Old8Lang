using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.VirtualMachine.EdgeCases;

[Collection("Sequential")]
public class VMUnexpectedInputsTests(ITestOutputHelper output)
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
            var vm = new Bytecode.VM.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private void AssertVMThrows(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        var vm = new Bytecode.VM.VirtualMachine(bytecodeFile);
        Assert.ThrowsAny<System.Exception>(() => vm.Execute());
    }

    #region 未定义变量访问测试

    [Fact]
    public void UnexpectedInputs_UndefinedVariable_ThrowsException()
    {
        var code = @"
            PrintLine(undefinedVar.ToStr())
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    [Fact]
    public void UnexpectedInputs_UndefinedVariableInExpression_ThrowsException()
    {
        var code = @"
            result <- x + 10
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 类型不匹配操作测试

    [Fact]
    public void UnexpectedInputs_BoolPlusInt_ThrowsException()
    {
        var code = @"
            x <- true
            y <- 10
            result <- x + y
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    [Fact]
    public void UnexpectedInputs_StringComparisonWithInt_ThrowsException()
    {
        var code = @"
            x <- ""hello""
            y <- 10
            result <- x > y
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 索引越界测试

    [Fact]
    public void UnexpectedInputs_ArrayIndexOutOfBounds_ThrowsException()
    {
        var code = @"
            arr <- [1, 2, 3]
            result <- arr[10]
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    [Fact]
    public void UnexpectedInputs_ArrayNegativeIndex_ThrowsException()
    {
        var code = @"
            arr <- [1, 2, 3]
            result <- arr[-1]
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    [Fact]
    public void UnexpectedInputs_ListIndexOutOfBounds_ThrowsException()
    {
        var code = @"
            list <- {1, 2, 3}
            result <- list[10]
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    [Fact]
    public void UnexpectedInputs_DictionaryKeyNotFound_ThrowsException()
    {
        var code = @"
            dict <- {""a"": 1, ""b"": 2}
            result <- dict[""c""]
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 除零错误测试

    [Fact]
    public void UnexpectedInputs_DivideByZero_ThrowsException()
    {
        var code = @"
            result <- 10 / 0
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    [Fact]
    public void UnexpectedInputs_ModuloByZero_ThrowsException()
    {
        var code = @"
            result <- 10 % 0
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 无效函数调用测试

    [Fact]
    public void UnexpectedInputs_UndefinedFunction_ThrowsException()
    {
        var code = @"
            result <- undefinedFunc()
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    [Fact]
    public void UnexpectedInputs_WrongParameterCount_ThrowsException()
    {
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }
            result <- add(1)
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 无效类型转换测试

    [Fact]
    public void UnexpectedInputs_InvalidStringToInt_ThrowsException()
    {
        var code = @"
            str <- ""not a number""
            result <- int(str)
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    [Fact]
    public void UnexpectedInputs_InvalidStringToDouble_ThrowsException()
    {
        var code = @"
            str <- ""not a number""
            result <- double(str)
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 无效操作测试

    [Fact]
    public void UnexpectedInputs_NullReference_ThrowsException()
    {
        var code = @"
            x <- null
            result <- x.ToStr()
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    [Fact]
    public void UnexpectedInputs_InvalidRange_ThrowsException()
    {
        var code = @"
            range <- 10..5
            for x <- range {
                PrintLine(x.ToStr())
            }
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 无效类操作测试

    [Fact]
    public void UnexpectedInputs_UndefinedClass_ThrowsException()
    {
        var code = @"
            obj <- UndefinedClass()
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    [Fact]
    public void UnexpectedInputs_UndefinedMethod_ThrowsException()
    {
        var code = @"
            class MyClass {
                public func existingMethod() -> void {
                    PrintLine(""exists"")
                }
            }
            obj <- MyClass()
            obj.undefinedMethod()
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    [Fact]
    public void UnexpectedInputs_PrivateMethodAccess_ThrowsException()
    {
        var code = @"
            class MyClass {
                private func privateMethod() -> void {
                    PrintLine(""private"")
                }
            }
            obj <- MyClass()
            obj.privateMethod()
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 无效赋值测试

    [Fact]
    public void UnexpectedInputs_AssignToConstant_ThrowsException()
    {
        var code = @"
            x:const <- 10
            x <- 20
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 无效控制流测试

    [Fact]
    public void UnexpectedInputs_BreakOutsideLoop_ThrowsException()
    {
        var code = @"
            break
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    [Fact]
    public void UnexpectedInputs_ContinueOutsideLoop_ThrowsException()
    {
        var code = @"
            continue
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 无效导入测试

    [Fact]
    public void UnexpectedInputs_ImportNonExistentModule_ThrowsException()
    {
        var code = @"
            import ""nonexistent_module""
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 无效异步操作测试

    [Fact]
    public void UnexpectedInputs_AwaitNonTask_ThrowsException()
    {
        var code = @"
            x <- 10
            result <- await x
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 无效范围操作测试

    [Fact]
    public void UnexpectedInputs_SliceOutOfBounds_HandlesGracefully()
    {
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            slice <- arr[0:10]
            PrintLine(len(slice).ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("5", output);
    }

    #endregion

    #region 无效键操作测试

    [Fact]
    public void UnexpectedInputs_DictKeyNonExistent_ThrowsException()
    {
        var code = @"
            dict <- {""a"": 1, ""b"": 2}
            result <- dict[""nonexistent""]
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 无效集合操作测试

    [Fact]
    public void UnexpectedInputs_RemoveFromEmptyCollection_ThrowsException()
    {
        var code = @"
            list <- {}
            list.Remove(1)
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 无效类型断言测试

    [Fact]
    public void UnexpectedInputs_InvalidTypeAssertion_ThrowsException()
    {
        var code = @"
            x <- ""hello""
            y <- x as int
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 无效匹配表达式测试

    [Fact]
    public void UnexpectedInputs_MatchNoMatchingCase_ThrowsException()
    {
        var code = @"
            x <- 5
            result <- match x {
                case 1 -> ""one""
                case 2 -> ""two""
            }
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 无效选择语句测试

    [Fact]
    public void UnexpectedInputs_SelectNoReadyChannel_ThrowsException()
    {
        var code = @"
            ch <- ChannelCreate()
            select {
                case val from ch -> {
                    PrintLine(val.ToStr())
                }
            }
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 无效生成器操作测试

    [Fact]
    public void UnexpectedInputs_InvalidYield_ThrowsException()
    {
        var code = @"
            func generator() -> {
                yield 10
                yield 20
            }
            gen <- generator()
            gen.MoveNext()
            gen.MoveNext()
            gen.MoveNext()
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion

    #region 无效字符串操作测试

    [Fact]
    public void UnexpectedInputs_SubstringOutOfBounds_ThrowsException()
    {
        var code = @"
            str <- ""hello""
            result <- str.Substring(10)
        ";

        var exception = Assert.ThrowsAny<System.Exception>(() => ExecuteVMCode(code));
        output.WriteLine($"Expected exception: {exception.Message}");
    }

    #endregion
}
