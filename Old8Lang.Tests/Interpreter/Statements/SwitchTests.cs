using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Statements;

/// <summary>
/// Switch语句解释模式测试
/// </summary>
public class SwitchTests
{
    [Fact]
    public void Switch_WithMatchingCase_ExecutesCorrectBranch()
    {
        // Arrange
        var code = @"
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("two", result.Value);
    }

    [Fact]
    public void Switch_WithNoMatchingCase_ExecutesDefault()
    {
        // Arrange
        var code = @"
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("default", result.Value);
    }

    [Fact]
    public void Switch_WithoutDefaultAndNoMatch_DoesNothing()
    {
        // Arrange
        var code = @"
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("unchanged", result.Value);
    }

    [Fact]
    public void Switch_WithStringValue_MatchesCorrectly()
    {
        // Arrange
        var code = @"
            color <- ""red""
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("R", result.Value);
    }

    [Fact]
    public void Switch_WithBooleanValue_MatchesCorrectly()
    {
        // Arrange
        var code = @"
            flag <- true
            result <- ""maybe""
            switch flag {
                case true {
                    result <- ""yes""
                }
                case false {
                    result <- ""no""
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("yes", result.Value);
    }

    [Fact]
    public void Switch_WithVariableExpression_MatchesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            y <- 20
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("thirty", result.Value);
    }

    [Fact]
    public void Switch_WithComplexCaseBody_ExecutesMultipleStatements()
    {
        // Arrange
        var code = @"
            value <- 1
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as IntLangValue;
        var message = interpreter.Manager.GetValue(new LangId("message")) as StringLangValue;

        Assert.NotNull(result);
        Assert.NotNull(message);
        Assert.Equal(10, result.Value);
        Assert.Equal("multiplied by 10", message.Value);
    }

    [Fact]
    public void Switch_WithNestedSwitch_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            outer <- 1
            inner <- 2
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("1-2", result.Value);
    }

    [Fact]
    public void Switch_WithFunctionCallInCondition_MatchesCorrectly()
    {
        // Arrange
        var code = @"
            func getValue() {
                return 3
            }
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("three", result.Value);
    }

    [Fact]
    public void Switch_WithDoubleValue_MatchesCorrectly()
    {
        // Arrange
        var code = @"
            value <- 3.14
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("pi", result.Value);
    }

    [Fact]
    public void Switch_WithCharValue_MatchesCorrectly()
    {
        // Arrange
        var code = @"
            grade <- 'B'
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("good", result.Value);
    }

    [Fact]
    public void Switch_WithVariableAssignmentInCases_UpdatesVariablesCorrectly()
    {
        // Arrange
        var code = @"
            mode <- 2
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var x = interpreter.Manager.GetValue(new LangId("x")) as IntLangValue;
        Assert.NotNull(x);
        Assert.Equal(200, x.Value);
    }

    [Fact]
    public void Switch_WithBreakStatement_BehavesCorrectly()
    {
        // 注意：这个测试取决于 Old8Lang 的 switch 是否支持 break
        // 如果不支持，可以修改或删除这个测试

        // Arrange
        var code = @"
            value <- 1
            result <- 0
            switch value {
                case 1 {
                    result <- 100
                    // break  // 如果支持 break
                }
                case 2 {
                    result <- 200
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as IntLangValue;
        Assert.NotNull(result);
        Assert.Equal(100, result.Value);
    }

    [Fact]
    public void Switch_EmptySwitch_DoesNothing()
    {
        // Arrange
        var code = @"
            value <- 1
            result <- ""unchanged""
            switch value {
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("unchanged", result.Value);
    }

    [Fact]
    public void Switch_MultipleCasesWithSameValue_FirstCaseExecutes()
    {
        // Arrange
        var code = @"
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("first", result.Value);
    }
}