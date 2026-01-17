using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Functions;

/// <summary>
/// 命名参数错误和边界测试
/// </summary>
public class NamedArgumentsErrorTests
{
    #region 参数名错误测试

    [Fact]
    public void NamedArguments_NonexistentParameterName_ThrowsError()
    {
        // Arrange
        var code = @"
            func test(a:int, b:int) -> int {
                return a + b
            }
            result <- test(a: 1, invalid: 2)
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var exception = Assert.Throws<ArgumentError>(() =>
        {
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
        });
        Assert.Contains("没有名为 'invalid' 的参数", exception.Message);
    }

    [Fact]
    public void NamedArguments_DuplicateParameterName_ThrowsError()
    {
        // Arrange
        var code = @"
            func test(a:int, b:int, c:int) -> int {
                return a + b + c
            }
            result <- test(a: 1, a: 2, c: 3)
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var exception = Assert.Throws<ArgumentError>(() =>
        {
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
        });
        Assert.Contains("命名参数 'a' 重复指定", exception.Message);
    }

    [Fact]
    public void NamedArguments_PositionalAndNamedConflict_ThrowsError()
    {
        // Arrange
        var code = @"
            func test(a:int, b:int, c:int) -> int {
                return a + b + c
            }
            result <- test(1, a: 2, c: 3)
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var exception = Assert.Throws<ArgumentError>(() =>
        {
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
        });
        Assert.Contains("参数 'a' 已经通过位置参数提供", exception.Message);
    }

    #endregion

    #region 必需参数缺失测试

    [Fact]
    public void NamedArguments_MissingRequiredParameter_ThrowsError()
    {
        // Arrange
        var code = @"
            func test(a:int, b:int, c:int) -> int {
                return a + b + c
            }
            result <- test(a: 1, c: 3)
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var exception = Assert.Throws<ArgumentError>(() =>
        {
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
        });
        Assert.Contains("必需参数 'b'", exception.Message);
        Assert.Contains("未提供值", exception.Message);
    }

    [Fact]
    public void NamedArguments_AllParametersMissing_ThrowsError()
    {
        // Arrange
        var code = @"
            func test(a:int, b:int) -> int {
                return a + b
            }
            result <- test()
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var exception = Assert.Throws<ArgumentError>(() =>
        {
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
        });
        // 检查错误消息包含函数名和参数不匹配的信息
        Assert.Contains("test", exception.Message);
    }

    #endregion

    #region params 参数限制测试

    [Fact]
    public void NamedArguments_ParamsParameter_ThrowsError()
    {
        // Arrange
        var code = @"
            func sum(params values:array<int>) -> int {
                result <- 0
                for val in values {
                    result <- result + val
                }
                return result
            }
            result <- sum(values: {1, 2, 3})
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var exception = Assert.Throws<ArgumentError>(() =>
        {
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
        });
        Assert.Contains("不支持对 params 参数", exception.Message);
        Assert.Contains("使用命名参数", exception.Message);
    }

    #endregion

    #region 参数数量边界测试

    [Fact]
    public void NamedArguments_TooManyArguments_ThrowsError()
    {
        // Arrange
        var code = @"
            func test(a:int, b:int) -> int {
                return a + b
            }
            result <- test(a: 1, b: 2, c: 3)
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var exception = Assert.Throws<ArgumentError>(() =>
        {
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
        });
        Assert.Contains("没有名为 'c' 的参数", exception.Message);
    }

    [Fact]
    public void NamedArguments_EmptyFunctionCall_NoParameters_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test() -> int {
                return 42
            }
            result <- test()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert - 应该成功执行
        var result = interpreter.Manager.GetValue(new AST.Expression.LangId("result")) as AST.Expression.Value.IntLangValue;
        Assert.NotNull(result);
        Assert.Equal(42, result.Value);
    }

    #endregion

    #region 默认参数边界测试

    [Fact]
    public void NamedArguments_OnlyDefaultParameters_AllOmitted_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test(a: 1, b: 2, c: 3) -> int {
                return a + b + c
            }
            result <- test()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new AST.Expression.LangId("result")) as AST.Expression.Value.IntLangValue;
        Assert.NotNull(result);
        Assert.Equal(6, result.Value); // 1 + 2 + 3 = 6
    }

    [Fact]
    public void NamedArguments_MixedRequiredAndDefault_OnlyRequiredProvided_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test(a:int, b: 10, c: 20) -> int {
                return a + b + c
            }
            result <- test(a: 5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new AST.Expression.LangId("result")) as AST.Expression.Value.IntLangValue;
        Assert.NotNull(result);
        Assert.Equal(35, result.Value); // 5 + 10 + 20 = 35
    }

    [Fact]
    public void NamedArguments_DefaultParameters_PartialOverride_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test(a: 1, b: 2, c: 3, d: 4) -> int {
                return a + b + c + d
            }
            result <- test(b: 20, d: 40)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new AST.Expression.LangId("result")) as AST.Expression.Value.IntLangValue;
        Assert.NotNull(result);
        Assert.Equal(64, result.Value); // 1 + 20 + 3 + 40 = 64
    }

    #endregion

    #region 特殊边界情况

    [Fact]
    public void NamedArguments_SameNameAsLocalVariable_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test(value:int) -> int {
                return value * 2
            }
            value <- 10
            result <- test(value: 5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new AST.Expression.LangId("result")) as AST.Expression.Value.IntLangValue;
        Assert.NotNull(result);
        Assert.Equal(10, result.Value); // test(5) = 5 * 2 = 10
    }

    [Fact]
    public void NamedArguments_VariableAsArgumentValue_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test(a:int, b:int) -> int {
                return a + b
            }
            x <- 10
            y <- 20
            result <- test(b: y, a: x)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new AST.Expression.LangId("result")) as AST.Expression.Value.IntLangValue;
        Assert.NotNull(result);
        Assert.Equal(30, result.Value); // 10 + 20 = 30
    }

    [Fact]
    public void NamedArguments_NullValue_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test(value) -> string {
                if value == null {
                    return ""null""
                } else {
                    return ""not null""
                }
            }
            result <- test(value: null)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new AST.Expression.LangId("result")) as AST.Expression.Value.StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("null", result.Value);
    }

    #endregion

    #region 函数重载与命名参数

    [Fact]
    public void NamedArguments_OverloadedFunction_SelectsCorrectOverload()
    {
        // Arrange
        var code = @"
            func test(a:int, b:int) -> int {
                return a + b
            }
            func test(a:int, b:int, c:int) -> int {
                return a + b + c
            }
            result1 <- test(a: 1, b: 2)
            result2 <- test(c: 3, b: 2, a: 1)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new AST.Expression.LangId("result1")) as AST.Expression.Value.IntLangValue;
        var result2 = interpreter.Manager.GetValue(new AST.Expression.LangId("result2")) as AST.Expression.Value.IntLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(3, result1.Value);  // test(1, 2) = 1 + 2 = 3
        Assert.Equal(6, result2.Value);  // test(1, 2, 3) = 1 + 2 + 3 = 6
    }

    #endregion
}
