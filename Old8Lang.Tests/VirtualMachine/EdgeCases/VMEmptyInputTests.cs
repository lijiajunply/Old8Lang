using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.EdgeCases;

[Collection("Sequential")]
public class VMEmptyInputTests
{
    private void AssertVMExecutionSucceeds(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        var vm = new Old8Lang.Bytecode.VirtualMachine(bytecodeFile);
        var exception = Record.Exception(() => vm.Execute());
        Assert.Null(exception);
    }

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

    #region 空代码测试

    [Fact]
    public void EmptyInput_EmptyCode_HandlesEmptyCode()
    {
        var code = @"";
        AssertVMExecutionSucceeds(code);
    }

    [Fact]
    public void EmptyInput_WhitespaceOnly_HandlesWhitespace()
    {
        var code = @"   
   

        ";
        AssertVMExecutionSucceeds(code);
    }

    [Fact]
    public void EmptyInput_CommentsOnly_HandlesComments()
    {
        var code = @"
            // This is a comment
            // Another comment
            // Final comment
        ";
        AssertVMExecutionSucceeds(code);
    }

    #endregion

    #region 空语句测试

    [Fact]
    public void EmptyInput_EmptyStatement_HandlesEmptyStatement()
    {
        var code = @"

        ";
        AssertVMExecutionSucceeds(code);
    }

    [Fact]
    public void EmptyInput_MultipleEmptyStatements_HandlesMultiple()
    {
        var code = @"


        ";
        AssertVMExecutionSucceeds(code);
    }

    #endregion

    #region 空数据结构测试

    [Fact]
    public void EmptyInput_EmptyStringLiteral_HandlesEmptyString()
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
    public void EmptyInput_EmptyArrayLiteral_HandlesEmptyArray()
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
    public void EmptyInput_EmptyListLiteral_HandlesEmptyList()
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
    public void EmptyInput_EmptyDictionaryLiteral_HandlesEmptyDictionary()
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
    public void EmptyInput_EmptyTuple_HandlesEmptyTuple()
    {
        var code = @"
            emptyTuple <- tuple()
            result <- len(emptyTuple)
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("0", output);
    }

    #endregion

    #region 空控制结构测试

    [Fact]
    public void EmptyInput_EmptyIfStatement_HandlesEmptyIf()
    {
        var code = @"
            x <- 10
            if x > 0 {
            }
            PrintLine(""OK"")
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("OK", output);
    }

    [Fact]
    public void EmptyInput_EmptyForLoop_HandlesEmptyFor()
    {
        var code = @"
            for i <- 0, i < 0, i++ {
            }
            PrintLine(""OK"")
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("OK", output);
    }

    [Fact]
    public void EmptyInput_EmptyWhileLoop_HandlesEmptyWhile()
    {
        var code = @"
            while false {
            }
            PrintLine(""OK"")
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("OK", output);
    }

    [Fact]
    public void EmptyInput_EmptyForInLoop_HandlesEmptyForIn()
    {
        var code = @"
            emptyArray <- []
            for x in emptyArray {
            }
            PrintLine(""OK"")
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("OK", output);
    }

    [Fact]
    public void EmptyInput_EmptyTryCatch_HandlesEmptyTryCatch()
    {
        var code = @"
            try {
            } catch {
            }
            PrintLine(""OK"")
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("OK", output);
    }

    #endregion

    #region 空函数测试

    [Fact]
    public void EmptyInput_EmptyFunction_HandlesEmptyFunction()
    {
        var code = @"
            func emptyFunc() -> void {
            }
            emptyFunc()
            PrintLine(""OK"")
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("OK", output);
    }

    [Fact]
    public void EmptyInput_EmptyAsyncFunction_HandlesEmptyAsyncFunction()
    {
        var code = @"
            async func emptyAsyncFunc() -> void {
            }
            task <- emptyAsyncFunc()
            await task
            PrintLine(""OK"")
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("OK", output);
    }

    [Fact]
    public void EmptyInput_EmptyLambda_HandlesEmptyLambda()
    {
        var code = @"
            emptyLambda <- () -> {}
            emptyLambda()
            PrintLine(""OK"")
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("OK", output);
    }

    #endregion

    #region 空类测试

    [Fact]
    public void EmptyInput_EmptyClass_HandlesEmptyClass()
    {
        var code = @"
            class EmptyClass {
            }
            obj <- EmptyClass()
            PrintLine(""OK"")
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("OK", output);
    }

    [Fact]
    public void EmptyInput_ClassWithEmptyMethod_HandlesEmptyMethod()
    {
        var code = @"
            class ClassWithEmptyMethod {
                public func emptyMethod() -> void {
                }
            }
            obj <- ClassWithEmptyMethod()
            obj.emptyMethod()
            PrintLine(""OK"")
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("OK", output);
    }

    #endregion

    #region 空作用域测试

    [Fact]
    public void EmptyInput_EmptyBlock_HandlesEmptyBlock()
    {
        var code = @"
            {
            }
            PrintLine(""OK"")
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("OK", output);
    }

    [Fact]
    public void EmptyInput_NestedEmptyBlocks_HandlesNestedBlocks()
    {
        var code = @"
            {
                {
                    {
                    }
                }
            }
            PrintLine(""OK"")
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("OK", output);
    }

    #endregion

    #region 空表达式测试

    [Fact]
    public void EmptyInput_TernaryWithEmptyBranches_HandlesEmptyTernary()
    {
        var code = @"
            x <- true
            result <- x ? 1 : 0
            PrintLine(result.ToStr())
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("1", output);
    }

    #endregion
}
