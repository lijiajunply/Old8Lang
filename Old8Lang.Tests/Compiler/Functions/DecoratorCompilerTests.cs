using Old8Lang.Error;
using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Functions;

/// <summary>
/// 装饰器编译器模式测试
/// </summary>
[Collection("Sequential")]
public class DecoratorCompilerTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region 基础功能测试

    [Fact]
    public void Compiler_SimpleDecorator_CompilesAndRuns()
    {
        // Arrange
        var code = @"
            func identity(f:object) -> object {
                return f
            }

            @identity
            func test(x:int) -> int {
                return x * 2
            }

            result:int <- test(5)
            Assert.Equal(10, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_DecoratorModifiesResult_CompilesAndRuns()
    {
        // Arrange
        var code = @"
            func double(f:object) -> object {
                wrapper <- (x:int) -> {
                    result:int <- f(x)
                    return result * 2
                }
                return wrapper
            }

            @double
            func getValue(x:int) -> int {
                return x
            }

            result:int <- getValue(5)
            Assert.Equal(10, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_DecoratorWithArguments_CompilesAndRuns()
    {
        // Arrange
        var code = @"
            func multiply(factor:int) -> object {
                return (f:object) -> {
                    wrapper <- (x:int) -> {
                        result:int <- f(x)
                        return result * factor
                    }
                    return wrapper
                }
            }

            @multiply(factor: 3)
            func getValue(x:int) -> int {
                return x
            }

            result:int <- getValue(5)
            Assert.Equal(15, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_MultipleDecorators_AppliesInCorrectOrder()
    {
        // Arrange
        var code = @"
            func addOne(f:object) -> object {
                wrapper <- (x:int) -> {
                    result:int <- f(x)
                    return result + 1
                }
                return wrapper
            }

            func double(f:object) -> object {
                wrapper <- (x:int) -> {
                    result:int <- f(x)
                    return result * 2
                }
                return wrapper
            }

            @double
            @addOne
            func getValue(x:int) -> int {
                return x
            }

            result:int <- getValue(5)
            // 应用顺序：getValue(5) -> 5, addOne -> 6, double -> 12
            Assert.Equal(12, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 高级功能测试

    [Fact]
    public void Compiler_DecoratorWithMultipleParameters_CompilesAndRuns()
    {
        // Arrange
        var code = @"
            func log(f:object) -> object {
                wrapper <- (a:int, b:int) -> {
                    result:int <- f(a, b)
                    return result
                }
                return wrapper
            }

            @log
            func add(a:int, b:int) -> int {
                return a + b
            }

            result:int <- add(3, 5)
            Assert.Equal(8, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_DecoratorWithNoParameters_CompilesAndRuns()
    {
        // Arrange
        var code = @"
            func decorator(f:object) -> object {
                wrapper <- () -> {
                    result:int <- f()
                    return result + 10
                }
                return wrapper
            }

            @decorator
            func getValue() -> int {
                return 32
            }

            result:int <- getValue()
            Assert.Equal(42, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_DecoratorReturningDifferentType_CompilesAndRuns()
    {
        // Arrange
        var code = @"
            func stringify(f:object) -> object {
                wrapper <- (x:int) -> {
                    result:int <- f(x)
                    return result.ToStr() + ""!""
                }
                return wrapper
            }

            @stringify
            func getValue(x:int) -> int {
                return x * 2
            }

            result:string <- getValue(5)
            Assert.Equal(""10!"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 边界测试

    [Fact]
    public void Compiler_DecoratorChainOfThree_AppliesCorrectly()
    {
        // Arrange
        var code = @"
            func add1(f:object) -> object {
                return (x:int) -> f(x) + 1
            }

            func add2(f:object) -> object {
                return (x:int) -> f(x) + 2
            }

            func add3(f:object) -> object {
                return (x:int) -> f(x) + 3
            }

            @add3
            @add2
            @add1
            func getValue(x:int) -> int {
                return x
            }

            result:int <- getValue(0)
            // 应用顺序：0 + 1 + 2 + 3 = 6
            Assert.Equal(6, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_DecoratorWithComplexArguments_CompilesAndRuns()
    {
        // Arrange
        var code = @"
            func config(timeout:int, enabled:bool) -> object {
                return (f:object) -> {
                    wrapper <- (x:int) -> {
                        if enabled {
                            return f(x) + timeout
                        }
                        return f(x)
                    }
                    return wrapper
                }
            }

            @config(timeout: 10, enabled: true)
            func getValue(x:int) -> int {
                return x
            }

            result:int <- getValue(5)
            Assert.Equal(15, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_DecoratorWithExpressionArguments_EvaluatesCorrectly()
    {
        // Arrange
        var code = @"
            func multiply(factor:int) -> object {
                return (f:object) -> {
                    wrapper <- (x:int) -> {
                        return f(x) * factor
                    }
                    return wrapper
                }
            }

            @multiply(factor: 2 + 3)
            func getValue(x:int) -> int {
                return x
            }

            result:int <- getValue(4)
            Assert.Equal(20, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_DecoratorPreservingFunctionName_CompilesAndRuns()
    {
        // Arrange
        var code = @"
            func decorator(f:object) -> object {
                return f
            }

            @decorator
            func myFunction(x:int) -> int {
                return x * 2
            }

            // 应该能够通过原名称调用
            result:int <- myFunction(5)
            Assert.Equal(10, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 错误测试

    [Fact]
    public void Compiler_DecoratorNotReturningFunction_ThrowsError()
    {
        // Arrange
        var code = @"
            func badDecorator(f:object) -> object {
                return 123  // 返回非函数值
            }

            @badDecorator
            func test(x:int) -> int {
                return x
            }

            result:int <- test(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.Throws<RuntimeError>(() => compiledAction());
    }

    [Fact]
    public void Compiler_UndefinedDecorator_ThrowsCompileError()
    {
        // Arrange
        var code = @"
            @nonexistent
            func test(x:int) -> int {
                return x
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);

        // Assert
        Assert.Throws<RuntimeError>(() => Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
    }

    [Fact]
    public void Compiler_DecoratorWithArgumentsNotReturningFunction_ThrowsError()
    {
        // Arrange
        var code = @"
            func badDecorator(timeout:int) -> object {
                return 123  // 应该返回一个接受函数的函数
            }

            @badDecorator(timeout: 60)
            func test(x:int) -> int {
                return x
            }

            result:int <- test(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.Throws<RuntimeError>(() => compiledAction());
    }

    #endregion

    #region 集成测试

    [Fact]
    public void Compiler_MultipleDecoratedFunctions_WorkIndependently()
    {
        // Arrange
        var code = @"
            func double(f:object) -> object {
                return (x:int) -> f(x) * 2
            }

            func triple(f:object) -> object {
                return (x:int) -> f(x) * 3
            }

            @double
            func func1(x:int) -> int {
                return x
            }

            @triple
            func func2(x:int) -> int {
                return x
            }

            r1:int <- func1(5)
            r2:int <- func2(5)
            Assert.Equal(10, r1)
            Assert.Equal(15, r2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_DecoratorWithRecursiveFunction_CompilesAndRuns()
    {
        // Arrange
        var code = @"
            func decorator(f:object) -> object {
                return f
            }

            @decorator
            func factorial(n:int) -> int {
                if n <= 1 {
                    return 1
                }
                return n * factorial(n - 1)
            }

            result:int <- factorial(5)
            Assert.Equal(120, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_DecoratorWithVoidFunction_CompilesAndRuns()
    {
        // Arrange
        var code = @"
            counter:int <- 0

            func decorator(f:object) -> object {
                wrapper <- (x:int) -> {
                    counter <- counter + 1
                    f(x)
                }
                return wrapper
            }

            @decorator
            func increment(x:int) -> void {
                counter <- counter + x
            }

            increment(5)
            Assert.Equal(6, counter)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 类型系统测试

    [Fact]
    public void Compiler_DecoratorWithTypeAnnotations_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func typed_decorator(f:object) -> object {
                wrapper <- (x:int) -> {
                    result:int <- f(x)
                    return result * 2
                }
                return wrapper
            }

            @typed_decorator
            func getValue(x:int) -> int {
                return x
            }

            result:int <- getValue(5)
            Assert.Equal(10, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_DecoratorChangingReturnType_CompilesWithCorrectType()
    {
        // Arrange
        var code = @"
            func to_string_decorator(f:object) -> object {
                wrapper <- (x:int) -> {
                    result:int <- f(x)
                    return ""Result: "" + result.ToStr()
                }
                return wrapper
            }

            @to_string_decorator
            func calculate(x:int) -> int {
                return x * 2
            }

            result:string <- calculate(5)
            Assert.Equal(""Result: 10"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion
}
