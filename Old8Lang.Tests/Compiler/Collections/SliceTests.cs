using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

/// <summary>
/// 切片（Slice）编译模式测试
/// 测试编译器模式下的切片操作的 IL 生成和执行
/// </summary>
[Collection("Sequential")]
public class SliceTests
{
    [Fact]
    public void Slice_BasicSlice_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> int {
                list <- {1, 2, 3, 4, 5}
                slice <- list[1:4]
                return slice.Count()
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
    public void Slice_WithStep_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> int {
                list <- {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}
                slice <- list[0:10:2]
                return slice.Count()
            }

            Assert.True(test() == 5)
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
