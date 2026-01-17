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

            result <- test()
            PrintLine(result.ToStr())
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
                a <- tuple[0]
                b <- tuple[1]
                c <- tuple[2]
                return a + b + c
            }

            result <- test()
            PrintLine(result.ToStr())
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
    public void Tuple_LargeElements_CompilesAndExecutes()
    {
        // Arrange - 测试超过 7 个元素的元组
        var code = @"
            func test() -> int {
                // 创建一个包含 10 个元素的元组
                largeTuple <- (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)

                // 访问第 8 个元素（索引 7）
                eighth <- largeTuple[7]
                // 访问第 10 个元素（索引 9）
                tenth <- largeTuple[9]

                return eighth + tenth
            }

            result <- test()
            PrintLine(result.ToStr())
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
    public void Tuple_NestedTuples_CompilesAndExecutes()
    {
        // Arrange - 测试嵌套元组（不再扁平化）
        var code = @"
            func test() -> int {
                // 嵌套元组
                nested <- ((1, 2), 3)

                // 访问嵌套元组的元素
                innerTuple <- nested[0]
                first <- innerTuple[0]
                second <- innerTuple[1]
                third <- nested[1]

                return first + second + third
            }

            result <- test()
            PrintLine(result.ToStr())
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
