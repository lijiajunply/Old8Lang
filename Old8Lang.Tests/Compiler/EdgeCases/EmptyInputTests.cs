using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.EdgeCases;

/// <summary>
/// 编译器模式下的边界和错误情况测试 - 空输入测试
/// </summary>
public class EmptyInputTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void EmptyString_CompilesAndExecutesCorrectly()
    {
        var code = @"
            emptyStr <- """"
            Assert.Equal("""", emptyStr)
            Assert.Equal(0, emptyStr.Length())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyList_CompilesAndExecutesCorrectly()
    {
        var code = @"
            emptyList <- {}
            Assert.Equal(0, emptyList.Count())
            Assert.True(emptyList.Count() == 0)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyArray_CompilesAndExecutesCorrectly()
    {
        var code = @"
            emptyArray <- []
            Assert.Equal(0, emptyArray.Length)
            Assert.True(emptyArray.Length == 0)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyDictionary_CompilesAndExecutesCorrectly()
    {
        var code = @"
            emptyDict <- {}
            Assert.Equal(0, emptyDict.Count())
            Assert.True(emptyDict.Count() == 0)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyCodeBlock_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func doNothing() -> void {
            }
            
            doNothing()
            Assert.True(true)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyLoop_CompilesAndExecutesCorrectly()
    {
        var code = @"
            i <- 0
            while i < 0 {
                i <- i + 1
            }
            
            Assert.Equal(0, i)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyIfStatement_CompilesAndExecutesCorrectly()
    {
        var code = @"
            x <- 0
            if false {
                x <- 1
            }
            
            Assert.Equal(0, x)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyClass_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class EmptyClass {
            }
            
            instance <- EmptyClass()
            Assert.NotNull(instance)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyTryCatch_CompilesAndExecutesCorrectly()
    {
        var code = @"
            try {
            } catch (e) {
            }
            
            Assert.True(true)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptySwitchStatement_CompilesAndExecutesCorrectly()
    {
        var code = @"
            x <- 5
            result <- 0
            
            switch x {
                case 1 -> {
                    result <- 1
                }
                case 2 -> {
                    result <- 2
                }
            }
            
            Assert.Equal(0, result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyForLoop_CompilesAndExecutesCorrectly()
    {
        var code = @"
            sum <- 0
            for i <- 0 to -1 {
                sum <- sum + i
            }
            
            Assert.Equal(0, sum)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyForInLoop_CompilesAndExecutesCorrectly()
    {
        var code = @"
            emptyList <- {}
            count <- 0
            for item in emptyList {
                count <- count + 1
            }
            
            Assert.Equal(0, count)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyLambda_CompilesAndExecutesCorrectly()
    {
        var code = @"
            emptyLambda <- func() -> void {
            }
            
            emptyLambda()
            Assert.True(true)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyAssignment_CompilesAndExecutesCorrectly()
    {
        var code = @"
            x <- 0
            Assert.Equal(0, x)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyExpression_CompilesAndExecutesCorrectly()
    {
        var code = @"
            result <- (())
            Assert.NotNull(result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyStringInTemplate_CompilesAndExecutesCorrectly()
    {
        var code = @"
            empty <- """"
            template <- $""Hello{empty}World""
            
            Assert.Equal(""HelloWorld"", template)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyFinallyBlock_CompilesAndExecutesCorrectly()
    {
        var code = @"
            try {
                x <- 1
            } catch (e) {
                x <- 2
            } finally {
            }
            
            Assert.Equal(1, x)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyDefaultCase_CompilesAndExecutesCorrectly()
    {
        var code = @"
            x <- 10
            result <- ""unknown""
            
            switch x {
                case 1 -> {
                    result <- ""one""
                }
                default -> {
                }
            }
            
            Assert.Equal(""unknown"", result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyMatchExpression_CompilesAndExecutesCorrectly()
    {
        var code = @"
            value <- 5
            result <- match value {
                case 1 -> ""one""
                case 2 -> ""two""
                case _ -> ""other""
            }
            
            Assert.Equal(""other"", result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyConstructor_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class SimpleClass {
                func init() {
                }
            }
            
            instance <- SimpleClass()
            Assert.NotNull(instance)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
