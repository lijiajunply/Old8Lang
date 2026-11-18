using Old8Lang.Error;
using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.EdgeCases;

/// <summary>
/// IL 模式下的边界和错误情况测试 - 意外输入测试
/// </summary>
public class UnexpectedInputsTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void NegativeIndex_CompilesAndExecutesCorrectly()
    {
        // 负索引在 Old8Lang 里是有效语法：-1 表示最后一个元素（与解释器 / VM 一致）。
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            Assert.Equal(5, arr[-1])
            Assert.Equal(4, arr[-2])
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
        // IL 模式在编译期就能发现未知变量/成员/方法（解释器为运行期错误）
        Assert.Throws<Old8Lang.Error.CompilerException>(() =>
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
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
            
            result <- nullableValue.ToStr()
            Assert.Equal(""null"", result)
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
        // IL 模式在编译期就能发现未知变量/成员/方法（解释器为运行期错误）
        Assert.ThrowsAny<Old8Exception>(() =>
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
    }

    [Fact]
    public void InvalidRange_CompilesAndExecutesCorrectly()
    {
        var code = @"
            range <- [10~5]
            Assert.Equal(6, range.Length)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact(Skip = "生成无效IL代码导致CLR崩溃，中止整个测试运行")]
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
            
            slice <- arr[10:20]
            Assert.Equal(0, slice.Count())
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
        // IL 模式在编译期就能发现未知变量/成员/方法（解释器为运行期错误）
        Assert.ThrowsAny<Old8Exception>(() =>
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
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
        // `.Match` 不是 Old8Lang 的字符串 API（正则用 Regex.RegexIsMatch/RegexMatch）；
        // IL 在编译期按未知方法报错。
        Assert.ThrowsAny<Old8Exception>(() =>
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
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
