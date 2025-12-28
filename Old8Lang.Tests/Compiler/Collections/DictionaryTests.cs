using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

/// <summary>
/// 字典（Dictionary）编译模式测试
/// 测试编译器模式下的字典操作的 IL 生成和执行
/// </summary>
[Collection("Sequential")]
public class DictionaryTests
{
    #region 基础创建和访问

    [Fact]
    public void Dictionary_BasicCreation_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> int {
                dict <- {""a"": 1, ""b"": 2, ""c"": 3}
                return dict[""b""]
            }

            Assert.True(test() == 2)
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
    public void Dictionary_EmptyDictionary_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> int {
                dict <- {}
                dict[""key""] <- 42
                return dict[""key""]
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
    public void Dictionary_MixedValueTypes_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> string {
                dict <- {
                    ""int"": 42,
                    ""string"": ""hello"",
                    ""bool"": true,
                    ""double"": 3.14
                }
                return dict[""string""]
            }

            Assert.True(test() == ""hello"")
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

    #endregion

    #region 字典操作

    [Fact]
    public void Dictionary_AddAndUpdate_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> int {
                dict <- {""a"": 1}
                dict[""b""] <- 2
                dict[""a""] <- 10
                return dict[""a""] + dict[""b""]
            }

            Assert.True(test() == 12)
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
    public void Dictionary_Remove_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> int {
                dict <- {""a"": 1, ""b"": 2, ""c"": 3}
                dict.Remove(""b"")
                return dict.Count()
            }

            Assert.True(test() == 2)
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
    public void Dictionary_ContainsKey_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> bool {
                dict <- {""a"": 1, ""b"": 2}
                return dict.ContainsKey(""a"")
            }

            Assert.True(test() == true)
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

    #endregion

    #region 字典迭代

    [Fact]
    public void Dictionary_ForInLoop_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> int {
                dict <- {""a"": 1, ""b"": 2, ""c"": 3}
                sum <- 0
                for key in dict.Keys() {
                    sum <- sum + dict[key]
                }
                return sum
            }

            Assert.True(test() == 6)
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

    #endregion

    #region 嵌套字典

    [Fact]
    public void Dictionary_Nested_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            func test() -> int {
                dict <- {
                    ""outer"": {""inner"": 42}
                }
                return dict[""outer""][""inner""]
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

    #endregion
}
