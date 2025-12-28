using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Statements;

/// <summary>
/// Switch语句编译模式测试
/// 测试编译器模式下的 switch-case 语句的 IL 生成和执行
/// </summary>
[Collection("Sequential")]
public class SwitchTests
{
    #region 基础匹配测试

    [Fact]
    public void Switch_WithMatchingCase_ExecutesCorrectBranch()
    {
        // Arrange
        var code = @"
            func test() -> string {
                value <- 2
                result <- ""default""
                switch value {
                    case 1 {
                        result <- ""one""
                    }
                    case 2 {
                        result <- ""two""
                    }
                    case 3 {
                        result <- ""three""
                    }
                }
                return result
            }

            Assert.True(test() == ""two"")
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
    public void Switch_WithNoMatchingCase_ExecutesDefault()
    {
        // Arrange
        var code = @"
            func test() -> string {
                value <- 5
                result <- ""initial""
                switch value {
                    case 1 {
                        result <- ""one""
                    }
                    case 2 {
                        result <- ""two""
                    }
                    default {
                        result <- ""default""
                    }
                }
                return result
            }

            Assert.True(test() == ""default"")
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
    public void Switch_WithoutDefaultAndNoMatch_DoesNothing()
    {
        // Arrange
        var code = @"
            func test() -> string {
                value <- 5
                result <- ""unchanged""
                switch value {
                    case 1 {
                        result <- ""one""
                    }
                    case 2 {
                        result <- ""two""
                    }
                }
                return result
            }

            Assert.True(test() == ""unchanged"")
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

    #region 不同类型测试

    [Fact]
    public void Switch_WithStringValue_MatchesCorrectly()
    {
        // Arrange
        var code = @"
            func test(color:string) -> string {
                result <- ""unknown""
                switch color {
                    case ""red"" {
                        result <- ""R""
                    }
                    case ""green"" {
                        result <- ""G""
                    }
                    case ""blue"" {
                        result <- ""B""
                    }
                    default {
                        result <- ""X""
                    }
                }
                return result
            }

            Assert.True(test(""red"") == ""R"")
            Assert.True(test(""yellow"") == ""X"")
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
    public void Switch_WithBooleanValue_MatchesCorrectly()
    {
        // Arrange
        var code = @"
            func test(flag:bool) -> string {
                result <- ""maybe""
                switch flag {
                    case true {
                        result <- ""yes""
                    }
                    case false {
                        result <- ""no""
                    }
                }
                return result
            }

            Assert.True(test(true) == ""yes"")
            Assert.True(test(false) == ""no"")
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
    public void Switch_WithDoubleValue_MatchesCorrectly()
    {
        // Arrange
        var code = @"
            func test(value:double) -> string {
                result <- ""none""
                switch value {
                    case 2.71 {
                        result <- ""e""
                    }
                    case 3.14 {
                        result <- ""pi""
                    }
                    default {
                        result <- ""other""
                    }
                }
                return result
            }

            Assert.True(test(3.14) == ""pi"")
            Assert.True(test(1.41) == ""other"")
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
    public void Switch_WithCharValue_MatchesCorrectly()
    {
        // Arrange
        var code = @"
            func test(grade:char) -> string {
                result <- ""unknown""
                switch grade {
                    case 'A' {
                        result <- ""excellent""
                    }
                    case 'B' {
                        result <- ""good""
                    }
                    case 'C' {
                        result <- ""average""
                    }
                    default {
                        result <- ""needs improvement""
                    }
                }
                return result
            }

            Assert.True(test('B') == ""good"")
            Assert.True(test('D') == ""needs improvement"")
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

    #region 表达式测试

    [Fact]
    public void Switch_WithVariableExpression_MatchesCorrectly()
    {
        // Arrange
        var code = @"
            func test(x:int, y:int) -> string {
                result <- ""none""
                switch (x + y) {
                    case 15 {
                        result <- ""fifteen""
                    }
                    case 30 {
                        result <- ""thirty""
                    }
                    default {
                        result <- ""other""
                    }
                }
                return result
            }

            Assert.True(test(10, 20) == ""thirty"")
            Assert.True(test(5, 5) == ""other"")
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
    public void Switch_WithFunctionCallInCondition_MatchesCorrectly()
    {
        // Arrange
        var code = @"
            func getValue() -> int {
                return 3
            }

            func test() -> string {
                result <- ""none""
                switch getValue() {
                    case 1 {
                        result <- ""one""
                    }
                    case 2 {
                        result <- ""two""
                    }
                    case 3 {
                        result <- ""three""
                    }
                }
                return result
            }

            Assert.True(test() == ""three"")
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

    #region 复杂分支测试

    [Fact]
    public void Switch_WithComplexCaseBody_ExecutesMultipleStatements()
    {
        // Arrange
        var code = @"
            func test(value:int) -> string {
                result <- 0
                message <- """"
                switch value {
                    case 1 {
                        result <- value * 10
                        message <- ""multiplied by 10""
                    }
                    case 2 {
                        result <- value * 20
                        message <- ""multiplied by 20""
                    }
                }
                return message + ""="" + result.ToStr()
            }

            Assert.True(test(1) == ""multiplied by 10=10"")
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
    public void Switch_WithVariableAssignmentInCases_UpdatesVariablesCorrectly()
    {
        // Arrange
        var code = @"
            func test(mode:int) -> int {
                x <- 0
                switch mode {
                    case 1 {
                        x <- 100
                    }
                    case 2 {
                        x <- 200
                    }
                    case 3 {
                        x <- 300
                    }
                }
                return x
            }

            Assert.True(test(2) == 200)
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

    #region 嵌套Switch测试

    [Fact]
    public void Switch_WithNestedSwitch_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            func test(outer:int, inner:int) -> string {
                result <- ""none""
                switch outer {
                    case 1 {
                        switch inner {
                            case 1 {
                                result <- ""1-1""
                            }
                            case 2 {
                                result <- ""1-2""
                            }
                        }
                    }
                    case 2 {
                        switch inner {
                            case 1 {
                                result <- ""2-1""
                            }
                            case 2 {
                                result <- ""2-2""
                            }
                        }
                    }
                }
                return result
            }

            Assert.True(test(1, 2) == ""1-2"")
            Assert.True(test(2, 1) == ""2-1"")
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

    #region 边界条件测试

    [Fact]
    public void Switch_EmptySwitch_DoesNothing()
    {
        // Arrange
        var code = @"
            func test() -> string {
                value <- 1
                result <- ""unchanged""
                switch value {
                }
                return result
            }

            Assert.True(test() == ""unchanged"")
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
    public void Switch_MultipleCasesWithSameValue_FirstCaseExecutes()
    {
        // Arrange
        var code = @"
            func test() -> string {
                value <- 1
                result <- ""initial""
                switch value {
                    case 1 {
                        result <- ""first""
                    }
                    case 1 {
                        result <- ""second""
                    }
                }
                return result
            }

            Assert.True(test() == ""first"")
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
    public void Switch_ManyCases_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            func test(value:int) -> string {
                result <- ""none""
                switch value {
                    case 0 { result <- ""zero"" }
                    case 1 { result <- ""one"" }
                    case 2 { result <- ""two"" }
                    case 3 { result <- ""three"" }
                    case 4 { result <- ""four"" }
                    case 5 { result <- ""five"" }
                    case 6 { result <- ""six"" }
                    case 7 { result <- ""seven"" }
                    case 8 { result <- ""eight"" }
                    case 9 { result <- ""nine"" }
                    default { result <- ""many"" }
                }
                return result
            }

            Assert.True(test(5) == ""five"")
            Assert.True(test(10) == ""many"")
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

    #region 特殊场景测试

    [Fact]
    public void Switch_InLoop_WorksCorrectly()
    {
        // Arrange
        var code = @"
            func test() -> string {
                result <- """"
                for i <- 1, i <= 3, i <- i + 1 {
                    switch i {
                        case 1 { result <- result + ""A"" }
                        case 2 { result <- result + ""B"" }
                        case 3 { result <- result + ""C"" }
                    }
                }
                return result
            }

            Assert.True(test() == ""ABC"")
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
    public void Switch_WithReturn_ReturnsEarly()
    {
        // Arrange
        var code = @"
            func test(value:int) -> string {
                switch value {
                    case 1 {
                        return ""one""
                    }
                    case 2 {
                        return ""two""
                    }
                    default {
                        return ""other""
                    }
                }
                return ""unreachable""
            }

            Assert.True(test(1) == ""one"")
            Assert.True(test(5) == ""other"")
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
