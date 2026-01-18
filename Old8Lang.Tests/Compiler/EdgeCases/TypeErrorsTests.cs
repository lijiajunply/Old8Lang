using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.EdgeCases;

/// <summary>
/// 编译器模式下的边界和错误情况测试 - 类型错误测试
/// </summary>
public class TypeErrorsTests
{
    private readonly ITestOutputHelper _output;

    public TypeErrorsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void StringToIntConversion_CompilesAndExecutesCorrectly()
    {
        var code = @"
            result <- ""123"".ToInt()
            Assert.Equal(123, result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void IntToDoubleConversion_CompilesAndExecutesCorrectly()
    {
        var code = @"
            result <- 42.ToDouble()
            Assert.Equal(42.0, result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void DoubleToIntConversion_CompilesAndExecutesCorrectly()
    {
        var code = @"
            result <- 3.14.ToInt()
            Assert.Equal(3, result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MixedTypeOperation_CompilesAndExecutesCorrectly()
    {
        var code = @"
            result1 <- 10 + 3.5
            result2 <- 5.5 * 2
            result3 <- 100 / 3.0
            
            Assert.Equal(13.5, result1)
            Assert.Equal(11.0, result2)
            Assert.True(result3 > 33.33 && result3 < 33.34)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInAssignment_CompilesAndExecutesCorrectly()
    {
        var code = @"
            x : int <- 42
            x <- ""hello""
            
            Assert.Equal(""hello"", x)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInFunctionCall_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func process(value:int) -> string {
                return value.ToStr()
            }
            
            result <- process(""hello"")
            
            Assert.Equal(""hello"", result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInComparison_CompilesAndExecutesCorrectly()
    {
        var code = @"
            result <- 10 == ""10""
            
            Assert.Equal(false, result)
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
    public void NullTypeHandling_CompilesAndExecutesCorrectly()
    {
        var code = @"
            nullableValue <- null
            
            caughtError <- """"
            try {
                result <- nullableValue.ToInt()
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
    public void GenericTypeInference_CompilesAndExecutesCorrectly()
    {
        var code = @"
            list <- {1, 2, 3, 4, 5}
            
            sum <- 0
            i <- 0
            while i < list.Count() {
                sum <- sum + list[i]
                i <- i + 1
            }
            
            Assert.Equal(15, sum)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeCoercionInCollections_CompilesAndExecutesCorrectly()
    {
        var code = @"
            mixedList <- {1, ""hello"", 3.14, true}
            
            Assert.Equal(4, mixedList.Count())
            Assert.Equal(1, mixedList[0])
            Assert.Equal(""hello"", mixedList[1])
            Assert.Equal(3.14, mixedList[2])
            Assert.Equal(true, mixedList[3])
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInClassField_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class MyClass {
                public value : int
                
                func init(v:int) {
                    this.value <- v
                }
                
                func setValue(newValue:string) -> void {
                    this.value <- newValue.ToInt()
                }
            }
            
            instance <- MyClass(10)
            instance.setValue(""42"")
            
            Assert.Equal(42, instance.value)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInArray_CompilesAndExecutesCorrectly()
    {
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            arr[2] <- ""hello""
            
            Assert.Equal(""hello"", arr[2])
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInDictionary_CompilesAndExecutesCorrectly()
    {
        var code = @"
            dict <- {""key1"": 1, ""key2"": 2}
            dict[""key1""] <- ""hello""
            
            Assert.Equal(""hello"", dict[""key1""])
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInArithmetic_CompilesAndExecutesCorrectly()
    {
        var code = @"
            result <- ""hello"" * 3
            
            Assert.Equal(""hellohellohello"", result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInLogicalOperation_CompilesAndExecutesCorrectly()
    {
        var code = @"
            result <- 1 && 0
            
            Assert.Equal(true, result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInReturn_CompilesAndExecutesCorrectly()
    {
        var code = @"
            func getValue() -> int {
                return 42
            }
            
            result <- getValue()
            Assert.Equal(42, result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInConditional_CompilesAndExecutesCorrectly()
    {
        var code = @"
            x <- 10
            result <- if x > 5 { ""greater"" } else { ""less"" }
            
            Assert.Equal(""greater"", result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInLoop_CompilesAndExecutesCorrectly()
    {
        var code = @"
            sum <- 0
            for i <- 0 to 5 {
                sum <- sum + i
            }
            
            Assert.Equal(15, sum)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInLambda_CompilesAndExecutesCorrectly()
    {
        var code = @"
            transform <- (n:int) -> n * 2
            result <- transform(5)
            
            Assert.Equal(10, result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInSwitch_CompilesAndExecutesCorrectly()
    {
        var code = @"
            x <- 2
            result <- 0
            
            switch x {
                case 1 -> {
                    result <- 10
                }
                case 2 -> {
                    result <- 20
                }
                default -> {
                    result <- 0
                }
            }
            
            Assert.Equal(20, result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInMatch_CompilesAndExecutesCorrectly()
    {
        var code = @"
            value <- 5
            result <- match value {
                case 1 -> ""one""
                case 5 -> ""five""
                case _ -> ""other""
            }
            
            Assert.Equal(""five"", result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInStringTemplate_CompilesAndExecutesCorrectly()
    {
        var code = @"
            name <- ""Alice""
            age <- 30
            result <- $""Hello {name}, you are {age} years old""
            
            Assert.Equal(""Hello Alice, you are 30 years old"", result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInAsOperator_CompilesAndExecutesCorrectly()
    {
        var code = @"
            value <- ""hello""
            result <- value as string
            
            Assert.Equal(""hello"", result)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeMismatchInIsOperator_CompilesAndExecutesCorrectly()
    {
        var code = @"
            value <- ""hello""
            result1 <- value is string
            result2 <- value is int
            
            Assert.Equal(true, result1)
            Assert.Equal(false, result2)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
