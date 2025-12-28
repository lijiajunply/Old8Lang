using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

/// <summary>
/// 元组（Tuple）编译模式测试
/// 测试编译器模式下的元组操作的 IL 生成和执行
/// </summary>
[Collection("Sequential")]
public class TupleTests
{
    [Fact]
    public void Tuple_BasicCreation_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> int {
                tuple <- (1, ""hello"", true)
                return tuple[0]
            }

            Assert.True(test() == 1)
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
    public void Tuple_Deconstruction_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> int {
                tuple <- (10, 20, 30)
                (a, b, c) <- tuple
                return a + b + c
            }

            Assert.True(test() == 60)
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
