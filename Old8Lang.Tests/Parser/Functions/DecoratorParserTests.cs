using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Functions;

/// <summary>
/// 装饰器语法解析测试
/// </summary>
[Collection("Sequential")]
public class DecoratorParserTests
{
    #region 基础语法解析测试

    [Fact]
    public void ParseProgram_SimpleDecorator_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @log
            func test(x:int) -> int {
                return x * 2
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert - 应该成功解析，不抛出异常
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_DecoratorWithArguments_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @cache(timeout: 60)
            func test(x:int) -> int {
                return x * x
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_MultipleDecorators_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @log
            @cache(timeout: 30)
            @validate
            func test(x:int) -> int {
                return x + 1
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_DecoratorOnAsyncFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @async_log
            async func test(x:int) -> int {
                return x * 2
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 装饰器参数测试

    [Fact]
    public void ParseProgram_DecoratorWithMultipleArguments_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @config(timeout: 60, enabled: true, name: ""test"")
            func test(x:int) -> int {
                return x
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_DecoratorWithExpressionArguments_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @config(timeout: 30 + 30, factor: 2.0 * 1.5)
            func test(x:int) -> int {
                return x
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_DecoratorWithCollectionArguments_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @config(items: {1, 2, 3}, options: {""key"": ""value""})
            func test(x:int) -> int {
                return x
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_DecoratorWithNegativeNumbers_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @config(offset: -10, scale: -1.5)
            func test(x:int) -> int {
                return x
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_DecoratorWithCharArgument_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @config(separator: 'x')
            func test(x:int) -> int {
                return x
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_DecoratorWithEmptyString_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @config(prefix: """")
            func test(x:int) -> int {
                return x
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 装饰器名称测试

    [Fact]
    public void ParseProgram_DecoratorWithUnderscores_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @log_v2
            func test(x:int) -> int {
                return x
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_DecoratorWithNumbers_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @cache_123
            func test(x:int) -> int {
                return x
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_DecoratorWithLongName_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @very_long_decorator_name_with_many_words
            func test(x:int) -> int {
                return x
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 边界测试

    [Fact]
    public void ParseProgram_DecoratorOnNoParamFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @simple
            func test() -> int {
                return 42
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_DecoratorOnManyParamFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @log
            func test(a:int, b:int, c:int, d:int, e:int) -> int {
                return a + b + c + d + e
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_DecoratorOnVoidFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @timing
            func test(x:int) -> void {
                PrintLine(x.ToStr())
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_FiveDecoratorsStacked_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @decorator1
            @decorator2
            @decorator3
            @decorator4
            @decorator5
            func test(x:int) -> int {
                return x
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_DecoratorWithLongParameterName_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @config(very_long_parameter_name_with_many_words: 100)
            func test(x:int) -> int {
                return x
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误语法测试

    [Fact]
    public void ParseProgram_DecoratorWithoutFunction_ThrowsError()
    {
        // Arrange
        var code = @"
            @log
            x <- 123
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.Throws<SyntaxError>(() => parser.ParseProgram());
    }

    [Fact]
    public void ParseProgram_DecoratorMissingName_ThrowsError()
    {
        // Arrange
        var code = @"
            @
            func test(x:int) -> int {
                return x
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.Throws<SyntaxError>(() => parser.ParseProgram());
    }

    [Fact]
    public void ParseProgram_DecoratorMissingClosingParen_ThrowsError()
    {
        // Arrange
        var code = @"
            @cache(timeout: 60
            func test(x:int) -> int {
                return x
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.Throws<SyntaxError>(() => parser.ParseProgram());
    }

    [Fact]
    public void ParseProgram_DecoratorInvalidArgumentSyntax_ThrowsError()
    {
        // Arrange
        var code = @"
            @cache(timeout:)
            func test(x:int) -> int {
                return x
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.Throws<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 多函数装饰器测试

    [Fact]
    public void ParseProgram_MultipleFunctionsWithDecorators_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            @log
            func func1(x:int) -> int {
                return x
            }

            @cache(timeout: 60)
            func func2(y:int) -> int {
                return y * 2
            }

            @validate
            @timing
            func func3(z:int) -> int {
                return z + 1
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    [Fact]
    public void ParseProgram_DecoratedAndNonDecoratedFunctions_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            func normal(x:int) -> int {
                return x
            }

            @log
            func decorated(y:int) -> int {
                return y * 2
            }

            func anotherNormal(z:int) -> int {
                return z + 1
            }
        ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion
}
