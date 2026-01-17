using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Functions;

/// <summary>
/// 装饰器解释器模式测试
/// </summary>
[Collection("Sequential")]
public class DecoratorInterpreterTests
{
    #region 基础功能测试

    [Fact]
    public void Interpreter_SimpleDecorator_ReturnsOriginalFunction()
    {
        // Arrange
        var code = @"
            func identity(f) {
                return f
            }

            @identity
            func test(x:int) -> int {
                return x * 2
            }

            result <- test(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(10, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Interpreter_DecoratorModifiesResult_ReturnsModifiedValue()
    {
        // Arrange
        var code = @"
            func double(f) {
                wrapper <- (x) -> {
                    result <- f(x)
                    return result * 2
                }
                return wrapper
            }

            @double
            func getValue(x:int) -> int {
                return x
            }

            result <- getValue(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(10, ((IntLangValue)result).Value); // 5 * 2 = 10
    }

    [Fact]
    public void Interpreter_DecoratorWithArguments_AppliesCorrectly()
    {
        // Arrange
        var code = @"
            func multiply(factor) {
                return (f) -> {
                    wrapper <- (x) -> {
                        result <- f(x)
                        return result * factor
                    }
                    return wrapper
                }
            }

            @multiply(factor: 3)
            func getValue(x:int) -> int {
                return x
            }

            result <- getValue(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value); // 5 * 3 = 15
    }

    [Fact]
    public void Interpreter_MultipleDecorators_AppliesInCorrectOrder()
    {
        // Arrange
        var code = @"
            func addOne(f) {
                wrapper <- (x) -> {
                    result <- f(x)
                    return result + 1
                }
                return wrapper
            }

            func double(f) {
                wrapper <- (x) -> {
                    result <- f(x)
                    return result * 2
                }
                return wrapper
            }

            @double
            @addOne
            func getValue(x:int) -> int {
                return x
            }

            result <- getValue(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // 应用顺序：getValue(5) -> 5, addOne -> 6, double -> 12
        Assert.Equal(12, ((IntLangValue)result).Value);
    }

    #endregion

    #region 高级功能测试

    [Fact]
    public void Interpreter_DecoratorWithCache_CachesResults()
    {
        // Arrange
        var code = @"
            callCount <- 0

            func cache(f) {
                cacheDict <- {}
                wrapper <- (n) -> {
                    key <- n.ToStr()
                    if cacheDict.ContainsKey(key) {
                        return cacheDict[key]
                    }
                    callCount <- callCount + 1
                    result <- f(n)
                    cacheDict[key] <- result
                    return result
                }
                return wrapper
            }

            @cache
            func expensiveOp(n:int) -> int {
                return n * n
            }

            r1 <- expensiveOp(5)
            r2 <- expensiveOp(5)
            r3 <- expensiveOp(10)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var callCount = interpreter.Manager.GetValue(new LangId("callCount"));
        var r1 = interpreter.Manager.GetValue(new LangId("r1"));
        var r2 = interpreter.Manager.GetValue(new LangId("r2"));
        var r3 = interpreter.Manager.GetValue(new LangId("r3"));

        Assert.Equal(2, ((IntLangValue)callCount).Value); // 只调用了2次（5和10各一次）
        Assert.Equal(25, ((IntLangValue)r1).Value);
        Assert.Equal(25, ((IntLangValue)r2).Value);
        Assert.Equal(100, ((IntLangValue)r3).Value);
    }

    [Fact]
    public void Interpreter_DecoratorWithMultipleParameters_WorksCorrectly()
    {
        // Arrange
        var code = @"
            func log(f) {
                wrapper <- (a, b) -> {
                    result <- f(a, b)
                    return result
                }
                return wrapper
            }

            @log
            func add(a:int, b:int) -> int {
                return a + b
            }

            result <- add(3, 5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(8, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Interpreter_DecoratorWithNoParameters_WorksCorrectly()
    {
        // Arrange
        var code = @"
            func decorator(f) {
                wrapper <- () -> {
                    result <- f()
                    return result + 10
                }
                return wrapper
            }

            @decorator
            func getValue() -> int {
                return 32
            }

            result <- getValue()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value); // 32 + 10 = 42
    }

    [Fact]
    public void Interpreter_DecoratorReturningDifferentType_WorksCorrectly()
    {
        // Arrange
        var code = @"
            func stringify(f) {
                wrapper <- (x) -> {
                    result <- f(x)
                    return result.ToStr() + ""!""
                }
                return wrapper
            }

            @stringify
            func getValue(x:int) -> int {
                return x * 2
            }

            result <- getValue(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("10!", ((StringLangValue)result).Value);
    }

    #endregion

    #region 边界测试

    [Fact]
    public void Interpreter_DecoratorChainOfFive_AppliesCorrectly()
    {
        // Arrange
        var code = @"
            func add1(f) {
                return (x) -> f(x) + 1
            }

            func add2(f) {
                return (x) -> f(x) + 2
            }

            func add3(f) {
                return (x) -> f(x) + 3
            }

            func add4(f) {
                return (x) -> f(x) + 4
            }

            func add5(f) {
                return (x) -> f(x) + 5
            }

            @add5
            @add4
            @add3
            @add2
            @add1
            func getValue(x:int) -> int {
                return x
            }

            result <- getValue(0)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // 应用顺序：0 + 1 + 2 + 3 + 4 + 5 = 15
        Assert.Equal(15, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Interpreter_DecoratorWithComplexArguments_WorksCorrectly()
    {
        // Arrange
        var code = @"
            func config(timeout, enabled, name) {
                return (f) -> {
                    wrapper <- (x) -> {
                        if enabled {
                            return f(x) + timeout
                        }
                        return f(x)
                    }
                    return wrapper
                }
            }

            @config(timeout: 10, enabled: true, name: ""test"")
            func getValue(x:int) -> int {
                return x
            }

            result <- getValue(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value); // 5 + 10 = 15
    }

    [Fact]
    public void Interpreter_DecoratorWithExpressionArguments_EvaluatesCorrectly()
    {
        // Arrange
        var code = @"
            func multiply(factor) {
                return (f) -> {
                    wrapper <- (x) -> {
                        return f(x) * factor
                    }
                    return wrapper
                }
            }

            @multiply(factor: 2 + 3)
            func getValue(x:int) -> int {
                return x
            }

            result <- getValue(4)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(20, ((IntLangValue)result).Value); // 4 * (2 + 3) = 20
    }

    [Fact]
    public void Interpreter_DecoratorPreservingFunctionName_WorksCorrectly()
    {
        // Arrange
        var code = @"
            func decorator(f) {
                return f
            }

            @decorator
            func myFunction(x:int) -> int {
                return x * 2
            }

            // 应该能够通过原名称调用
            result <- myFunction(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(10, ((IntLangValue)result).Value);
    }

    #endregion

    #region 错误测试

    [Fact]
    public void Interpreter_DecoratorNotReturningFunction_ThrowsError()
    {
        // Arrange
        var code = @"
            func badDecorator(f) {
                return 123  // 返回非函数值
            }

            @badDecorator
            func test(x:int) -> int {
                return x
            }

            result <- test(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);

        // Assert
        Assert.Throws<InvalidOperationError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void Interpreter_DecoratorWithWrongSignature_ThrowsError()
    {
        // Arrange
        var code = @"
            func decorator(f) {
                // 返回的包装函数参数数量不匹配
                wrapper <- () -> {
                    return 42
                }
                return wrapper
            }

            @decorator
            func test(x:int) -> int {
                return x
            }

            result <- test(5)  // 调用时参数不匹配
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);

        // Assert
        Assert.Throws<ArgumentError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void Interpreter_UndefinedDecorator_ThrowsError()
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
        Assert.Throws<NameError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void Interpreter_DecoratorWithArgumentsNotReturningFunction_ThrowsError()
    {
        // Arrange
        var code = @"
            func badDecorator(timeout) {
                return 123  // 应该返回一个接受函数的函数
            }

            @badDecorator(timeout: 60)
            func test(x:int) -> int {
                return x
            }

            result <- test(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);

        // Assert
        Assert.Throws<InvalidOperationError>(() => ast.Run(interpreter.Manager));
    }

    #endregion

    #region 集成测试

    [Fact]
    public void Interpreter_MultipleDecoratedFunctions_WorkIndependently()
    {
        // Arrange
        var code = @"
            func double(f) {
                return (x) -> f(x) * 2
            }

            func triple(f) {
                return (x) -> f(x) * 3
            }

            @double
            func func1(x:int) -> int {
                return x
            }

            @triple
            func func2(x:int) -> int {
                return x
            }

            r1 <- func1(5)
            r2 <- func2(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var r1 = interpreter.Manager.GetValue(new LangId("r1"));
        var r2 = interpreter.Manager.GetValue(new LangId("r2"));

        Assert.Equal(10, ((IntLangValue)r1).Value); // 5 * 2
        Assert.Equal(15, ((IntLangValue)r2).Value); // 5 * 3
    }

    [Fact]
    public void Interpreter_DecoratorAccessingClosureVariables_WorksCorrectly()
    {
        // Arrange
        var code = @"
            multiplier <- 10

            func useMultiplier(f) {
                wrapper <- (x) -> {
                    result <- f(x)
                    return result * multiplier
                }
                return wrapper
            }

            @useMultiplier
            func getValue(x:int) -> int {
                return x
            }

            result <- getValue(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(50, ((IntLangValue)result).Value); // 5 * 10
    }

    [Fact]
    public void Interpreter_DecoratorWithRecursiveFunction_WorksCorrectly()
    {
        // Arrange
        var code = @"
            func decorator(f) {
                return f
            }

            @decorator
            func factorial(n:int) -> int {
                if n <= 1 {
                    return 1
                }
                return n * factorial(n - 1)
            }

            result <- factorial(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(120, ((IntLangValue)result).Value); // 5! = 120
    }

    #endregion
}
