using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

/// <summary>
/// 列表（List）编译模式测试
/// 测试编译器模式下的列表操作的 IL 生成和执行
/// </summary>
[Collection("Sequential")]
public class ListTests
{
    [Fact]
    public void List_BasicCreation_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> int {
                list <- {1, 2, 3, 4, 5}
                return list[2]
            }

            Assert.True(test() == 3)
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
    public void List_AddAndRemove_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> int {
                list <- {1, 2, 3}
                list.Add(4)
                list.Remove(2)
                return list.Count()
            }

            Assert.True(test() == 3)
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
    public void List_ForInLoop_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> int {
                list <- {1, 2, 3, 4, 5}
                sum <- 0
                for item in list {
                    sum <- sum + item
                }
                return sum
            }

            Assert.True(test() == 15)
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
