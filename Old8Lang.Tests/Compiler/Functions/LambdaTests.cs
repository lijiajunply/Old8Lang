using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Functions;

/// <summary>
/// Lambda表达式编译模式测试
/// 测试编译器模式下的Lambda表达式的 IL 生成和执行
/// </summary>
[Collection("Sequential")]
public class LambdaTests
{
    [Fact]
    public void Lambda_BasicLambda_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> int {
                add <- (x:int, y:int) -> x + y
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
    public void Lambda_NoParameters_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> int {
                getFortyTwo <- () -> 42
                return getFortyTwo()
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
    public void Lambda_WithBlock_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> int {
                calculate <- (x:int, y:int) -> {
                    sum <- x + y
                    return sum * 2
                }
                return calculate(5, 10)
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
}
