using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Functions;

/// <summary>
/// 函数调用编译模式测试
/// 测试编译器模式下的函数调用的 IL 生成和执行
/// </summary>
[Collection("Sequential")]
public class FunctionCallTests
{
    [Fact]
    public void FunctionCall_NoParameters_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func getValue() -> int {
                return 42
            }

            func test() -> int {
                return getValue()
            }

            Assert.True(test() == 42)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void FunctionCall_WithParameters_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }

            func test() -> int {
                return add(10, 20)
            }

            Assert.True(test() == 30)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void FunctionCall_NestedCalls_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func double(x:int) -> int {
                return x * 2
            }

            func addTen(x:int) -> int {
                return x + 10
            }

            func test() -> int {
                return addTen(double(5))
            }

            Assert.True(test() == 20)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void FunctionCall_Recursive_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func factorial(n:int) -> int {
                if n <= 1 {
                    return 1
                }
                return n * factorial(n - 1)
            }

            Assert.True(factorial(5) == 120)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
