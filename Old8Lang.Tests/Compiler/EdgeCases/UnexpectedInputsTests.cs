using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.EdgeCases;

/// <summary>
/// 编译器模式下的边界和错误情况测试 - 意外输入测试
/// </summary>
public class UnexpectedInputsTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void NegativeIndex_CompilesAndExecutesCorrectly()
    {
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            
            caughtError <- """"
            try {
                item <- arr[-1]
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void OutOfBoundsIndex_CompilesAndExecutesCorrectly()
    {
        var code = @"
            arr <- [1, 2, 3]
            
            caughtError <- """"
            try {
                item <- arr[10]
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void DivisionByZero_CompilesAndExecutesCorrectly()
    {
        var code = @"
            caughtError <- """"
            try {
                result <- 10 / 0
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ModuloByZero_CompilesAndExecutesCorrectly()
    {
        var code = @"
            caughtError <- """"
            try {
                result <- 10 % 0
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void NegativeSquareRoot_CompilesAndExecutesCorrectly()
    {
        var code = @"
            caughtError <- """"
            try {
                result <- (-1.0).Sqrt()
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AccessingNonExistentVariable_CompilesAndExecutesCorrectly()
    {
        var code = @"
            caughtError <- """"
            try {
                value <- undefinedVariable
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void CallingUndefinedFunction_CompilesAndExecutesCorrectly()
    {
        var code = @"
            caughtError <- """"
            try {
                result <- undefinedFunction()
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AccessingNonExistentDictionaryKey_CompilesAndExecutesCorrectly()
    {
        var code = @"
            dict <- {""key1"": ""value1"", ""key2"": ""value2""}
            
            caughtError <- """"
            try {
                value <- dict[""nonexistent""]
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InvalidStringIndex_CompilesAndExecutesCorrectly()
    {
        var code = @"
            str <- ""hello""
            
            caughtError <- """"
            try {
                char <- str[100]
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void NullReference_CompilesAndExecutesCorrectly()
    {
        var code = @"
            nullableValue <- null
            
            caughtError <- """"
            try {
                result <- nullableValue.ToStr()
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ConvertingInvalidType_CompilesAndExecutesCorrectly()
    {
        var code = @"
            caughtError <- """"
            try {
                result <- ""not a number"".ToInt()
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InvalidMethodCall_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class MyClass {
                public value <- 0
                
                func init(v:int) {
                    this.value <- v
                }
            }
            
            instance <- MyClass(10)
            
            caughtError <- """"
            try {
                result <- instance.nonExistentMethod()
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InvalidRange_CompilesAndExecutesCorrectly()
    {
        var code = @"
            caughtError <- """"
            try {
                range <- 10 to 5
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void StackOverflow_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func infiniteRecursion() -> int {
                return infiniteRecursion()
            }
            
            caughtError <- """"
            try {
                result <- infiniteRecursion()
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MemoryAllocation_CompilesAndExecutesCorrectly()
    {
        var code = @"
            largeList <- {}
            i <- 0
            while i < 10000 {
                largeList.Add(i)
                i <- i + 1
            }
            
            Assert.Equal(10000, largeList.Count())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InvalidSlice_CompilesAndExecutesCorrectly()
    {
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            
            caughtError <- """"
            try {
                slice <- arr[10:20]
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InvalidTypeConversion_CompilesAndExecutesCorrectly()
    {
        var code = @"
            caughtError <- """"
            try {
                result <- true.ToDouble()
            } catch (e) {
                caughtError <- e
            }
            
            result <- result
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AccessingPrivateMember_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class MyClass {
                private privateField <- 0
                
                func init() {
                    this.privateField <- 10
                }
            }
            
            instance <- MyClass()
            
            caughtError <- """"
            try {
                value <- instance.privateField
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InvalidRegularExpression_CompilesAndExecutesCorrectly()
    {
        var code = @"
            caughtError <- """"
            try {
                result <- ""test"".Match(""["")
            } catch (e) {
                caughtError <- e
            }
            
            Assert.True(caughtError != """")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ReassigningConstant_CompilesAndExecutesCorrectly()
    {
        var code = @"
            x <- 10
            x <- 20
            
            Assert.Equal(20, x)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AccessingUninitializedField_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class MyClass {
                public uninitializedField : int
                
                func init() {
                }
            }
            
            instance <- MyClass()
            value <- instance.uninitializedField
            
            Assert.Equal(0, value)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InvalidArithmeticOperation_CompilesAndExecutesCorrectly()
    {
        var code = @"
            caughtError <- """"
            try {
                result <- ""hello"" + 10
            } catch (e) {
                caughtError <- e
            }
            
            result <- result
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
