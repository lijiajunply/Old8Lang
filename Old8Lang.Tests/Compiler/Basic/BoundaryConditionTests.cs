using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Basic;

/// <summary>
/// 边界条件编译模式测试
/// 测试编译器在处理各种边界情况时的行为
/// </summary>
[Collection("Sequential")]
public class BoundaryConditionTests
{
    #region 数值边界测试

    [Theory]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1)]
    public void IntegerBoundaryValues_CompilesCorrectly(int value)
    {
        // Arrange
        var code = $"a <- {value}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    [InlineData(double.Epsilon)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    public void FloatingPointBoundaryValues_CompilesCorrectly(double value)
    {
        // Arrange
        var code = $"a <- {value}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("0.0000000000001")]
    [InlineData("999999999999999999.0")]
    [InlineData("1e-308")]
    [InlineData("1e+308")]
    public void ScientificNotationBoundary_CompilesCorrectly(string value)
    {
        // Arrange
        var code = $"a <- {value}";
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

    #region 字符串边界测试

    [Fact]
    public void EmptyString_CompilesCorrectly()
    {
        // Arrange
        var code = "a <- \"\"";
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
    public void SingleCharacterString_CompilesCorrectly()
    {
        // Arrange
        var code = "a <- \"a\"";
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
    public void VeryLongString_CompilesCorrectly()
    {
        // Arrange
        var longString = new string('a', 10000);
        var code = $"a <- \"{longString}\"";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("中文测试字符串")]
    [InlineData("🚀 Emoji Test 🌟")]
    [InlineData("Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?")]
    [InlineData("New\nLine\tTab\rCarriage")]
    [InlineData("Back\\Slash and \"Quote\"")]
    public void UnicodeAndSpecialCharacters_CompilesCorrectly(string str)
    {
        // Arrange
        var escapedStr = str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", "\\r");
        var code = $"a <- \"{escapedStr}\"";
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

    #region 集合边界测试

    [Fact]
    public void EmptyArray_CompilesCorrectly()
    {
        // Arrange
        var code = "a <- []";
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
    public void SingleElementArray_CompilesCorrectly()
    {
        // Arrange
        var code = "a <- [42]";
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
    public void LargeArray_CompilesCorrectly()
    {
        // Arrange
        var code = new System.Text.StringBuilder("a <- [");
        for (int i = 0; i < 1000; i++)
        {
            if (i > 0) code.Append(", ");
            code.Append(i);
        }
        code.Append("]");
        
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code.ToString());
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyDictionary_CompilesCorrectly()
    {
        // Arrange
        var code = "a <- {}";
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
    public void SinglePairDictionary_CompilesCorrectly()
    {
        // Arrange
        var code = "a <- {\"key\": \"value\"}";
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

    #region 布尔值边界测试

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BooleanValues_CompilesCorrectly(bool value)
    {
        // Arrange
        var code = $"a <- {value.ToString().ToLower()}";
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
    public void BooleanLogicalOperations_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- true
            b <- false
            c <- a and b
            d <- a or b
            e <- not a
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

    #region 字符边界测试

    [Theory]
    [InlineData('a')]
    [InlineData('Z')]
    [InlineData('0')]
    [InlineData('9')]
    [InlineData('@')]
    [InlineData(' ')]
    [InlineData('\n')]
    [InlineData('\t')]
    public void CharacterBoundaryValues_CompilesCorrectly(char value)
    {
        // Arrange
        var escapedChar = value switch
        {
            '\'' => "\\'",
            '\n' => "\\n",
            '\t' => "\\t",
            '\r' => "\\r",
            '\\' => "\\\\",
            _ => value.ToString()
        };
        
        var code = $"a <- '{escapedChar}'";
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

    #region 算术运算边界测试

    [Fact]
    public void DivisionByNearZero_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 0.0000001
            c <- a / b
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
    public void ModuloOperations_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10 % 3
            b <- 10 % 10
            c <- 10 % 1
            d <- 1 % 10
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
    public void PowerOperations_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 2 ^ 0
            b <- 2 ^ 1
            c <- 0 ^ 5
            d <- 1 ^ 100
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

    #region 嵌套结构边界测试

    [Fact]
    public void DeepNestedArrays_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- [[[[1]]]]
            b <- [1, [2, [3, [4]]]]
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
    public void DeepNestedDictionaries_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- {""a"": {""b"": {""c"": ""value""}}}
            b <- {""x"": 1, ""y"": {""z"": 2}}
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
    public void MixedNestedStructures_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- [{""key"": [1, 2, 3]}, {""another"": {""nested"": [4, 5]}}]
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

    #region 变量名边界测试

    [Theory]
    [InlineData("_")]
    [InlineData("_123")]
    [InlineData("a")]
    [InlineData("Z")]
    [InlineData("中文变量名")]
    [InlineData("variable_with_underscores")]
    [InlineData("variable123")]
    public void EdgeCaseVariableNames_CompilesCorrectly(string varName)
    {
        // Arrange
        var code = $"{varName} <- 42";
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

    #region 空值和null测试

    [Fact]
    public void NullValue_CompilesCorrectly()
    {
        // Arrange
        var code = "a <- null";
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

    #region 注释和空白字符测试

    [Fact]
    public void CodeWithComments_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            // This is a comment
            a <- 42  /* inline comment */
            /* 
             * Multi-line comment
             * with multiple lines
             */
            b <- 24
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
    public void CodeWithExtraWhitespace_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            
            a <- 42
            
            b <-  24   
            
            c <-   12   
            
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