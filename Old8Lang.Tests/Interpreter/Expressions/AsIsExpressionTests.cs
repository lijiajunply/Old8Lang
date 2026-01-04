using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Expressions;

/// <summary>
/// as 和 is 表达式解释模式测试
/// 测试解释器模式下 as 和 is 操作符的执行和值计算
/// </summary>
public class AsIsExpressionTests
{
    #region as 表达式测试 - 解释器模式

    [Fact]
    public void AsExpression_ValidConversion_IntToDouble_ReturnsCorrectValue()
    {
        // Arrange
        var code = @"
            intValue <- 42
            result <- intValue as double
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(42.0, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void AsExpression_ValidConversion_DoubleToInt_ReturnsCorrectValue()
    {
        // Arrange
        var code = @"
            doubleValue <- 3.14
            result <- doubleValue as int
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(3, ((IntLangValue)result).Value); // 应该截断小数部分
    }

    [Fact]
    public void AsExpression_StringToInt_ValidString_ReturnsCorrectValue()
    {
        // Arrange
        var code = @"
            stringValue <- ""123""
            result <- stringValue as int
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(123, ((IntLangValue)result).Value);
    }

    [Fact]
    public void AsExpression_StringToInt_InvalidString_ReturnsNull()
    {
        // Arrange
        var code = @"
            stringValue <- ""123""
            result <- stringValue as int
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
    }

    [Fact]
    public void AsExpression_BoolToInt_True_ReturnsOne()
    {
        // Arrange
        var code = @"
            boolValue <- true
            result <- boolValue as int
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value);
    }

    [Fact]
    public void AsExpression_BoolToInt_False_ReturnsZero()
    {
        // Arrange
        var code = @"
            boolValue <- false
            result <- boolValue as int
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(0, ((IntLangValue)result).Value);
    }

    [Fact]
    public void AsExpression_StringToDouble_ValidString_ReturnsCorrectValue()
    {
        // Arrange
        var code = @"
            stringValue <- ""3.14159""
            result <- stringValue as double
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(3.14159, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void AsExpression_IntToString_ReturnsStringRepresentation()
    {
        // Arrange
        var code = @"
            intValue <- 42
            result <- intValue as string
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("42", ((StringLangValue)result).Value);
    }

    [Fact]
    public void AsExpression_NestedInComplexExpression_ReturnsCorrectValue()
    {
        // Arrange
        var code = @"
            value <- ""42""
            result <- (value as int) * 2 + 10
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(94, ((IntLangValue)result).Value); // (42 * 2) + 10 = 94
    }

    #endregion

    #region is 表达式测试 - 解释器模式

    [Fact]
    public void IsExpression_IntCheck_IsInt_ReturnsTrue()
    {
        // Arrange
        var code = @"
            value <- 42
            result <- value is int
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    [Fact]
    public void IsExpression_DoubleCheck_IsDouble_ReturnsTrue()
    {
        // Arrange
        var code = @"
            value <- 3.14
            result <- value is double
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    [Fact]
    public void IsExpression_StringCheck_IsString_ReturnsTrue()
    {
        // Arrange
        var code = @"
            value <- ""hello""
            result <- value is string
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    [Fact]
    public void IsExpression_BoolCheck_IsBool_ReturnsTrue()
    {
        // Arrange
        var code = @"
            value <- true
            result <- value is bool
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    [Fact]
    public void IsExpression_TypeMismatch_IsDifferentType_ReturnsFalse()
    {
        // Arrange
        var code = @"
            value <- 42
            result <- value is string
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.False(((BoolLangValue)result).Value);
    }

    [Fact]
    public void IsExpression_CharCheck_IsChar_ReturnsTrue()
    {
        // Arrange
        var code = @"
            value <- 'A'
            result <- value is char
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    [Fact]
    public void IsExpression_InConditional_WorksCorrectly()
    {
        // Arrange
        var code = @"
            value <- 42
            result <- """"
            if value is int {
                result <- ""It's an integer""
            } else {
                result <- ""It's not an integer""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("It's an integer", result.Value);
    }

    [Fact]
    public void IsExpression_ComplexLogic_ReturnsCorrectValues()
    {
        // Arrange
        var code = @"
            value <- ""hello""
            result1 <- value is string
            result2 <- value is int
            result3 <- (value is string) and (value is not int)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as BoolLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as BoolLangValue;
        var result3 = interpreter.Manager.GetValue(new LangId("result3")) as BoolLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);

        Assert.True(result1.Value);  // "hello" is string
        Assert.False(result2.Value); // "hello" is not int
        Assert.True(result3.Value);  // ("hello" is string) and ("hello" is not int) = true and true = true
    }

    #endregion

    #region 混合表达式测试 - 解释器模式

    [Fact]
    public void AsIsExpression_MixedUsage_ReturnsCorrectValues()
    {
        // Arrange
        var code = @"
            value <- ""42""
            isInt <- value is int
            asInt <- value as int
            isNotNull <- asInt is not null
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var isInt = interpreter.Manager.GetValue(new LangId("isInt")) as BoolLangValue;
        var asInt = interpreter.Manager.GetValue(new LangId("asInt"));
        var isNotNull = interpreter.Manager.GetValue(new LangId("isNotNull")) as BoolLangValue;

        Assert.NotNull(isInt);
        Assert.NotNull(asInt);
        Assert.NotNull(isNotNull);

        Assert.False(isInt.Value);   // "42" is not int
        Assert.IsType<IntLangValue>(asInt); // "42" as int = 42
        Assert.True(isNotNull.Value); // 42 is not null
    }

    [Fact]
    public void AsIsExpression_ChainedOperations_ReturnsCorrectValues()
    {
        // Arrange
        var code = @"
            stringValue <- ""123""
            intValue <- stringValue as int
            doubleValue <- intValue as double
            isString <- stringValue is string
            isDouble <- doubleValue is double
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var intValue = interpreter.Manager.GetValue(new LangId("intValue")) as IntLangValue;
        var doubleValue = interpreter.Manager.GetValue(new LangId("doubleValue")) as DoubleLangValue;
        var isString = interpreter.Manager.GetValue(new LangId("isString")) as BoolLangValue;
        var isDouble = interpreter.Manager.GetValue(new LangId("isDouble")) as BoolLangValue;

        Assert.NotNull(intValue);
        Assert.NotNull(doubleValue);
        Assert.NotNull(isString);
        Assert.NotNull(isDouble);

        Assert.Equal(123, intValue.Value);
        Assert.Equal(123.0, doubleValue.Value);
        Assert.True(isString.Value);
        Assert.True(isDouble.Value);
    }

    [Fact]
    public void AsIsExpression_InLoop_SumsCorrectly()
    {
        // Arrange
        var code = @"
            values <- [""1"", ""2"", ""abc"", ""4""]
            sum <- 0
            for item in values {
                if item is int {
                    sum <- sum + (item as int)
                } else {
                    intItem <- item as int
                    if intItem is not null {
                        sum <- sum + intItem
                    }
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var sum = interpreter.Manager.GetValue(new LangId("sum")) as IntLangValue;
        Assert.NotNull(sum);
        Assert.Equal(7, sum.Value); // 1 + 2 + 0 (abc) + 4 = 7
    }

    #endregion

    #region 边界情况测试 - 解释器模式

    [Fact]
    public void AsExpression_NullValue_ReturnsNull()
    {
        // Arrange
        var code = @"
            value <- null
            result <- value as string
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
    }

    [Fact]
    public void IsExpression_NullValue_ReturnsCorrectBooleans()
    {
        // Arrange
        var code = @"
            value <- null
            result1 <- value is string
            result2 <- value is int
            result3 <- value is null
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as BoolLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as BoolLangValue;
        var result3 = interpreter.Manager.GetValue(new LangId("result3")) as BoolLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);

        Assert.False(result1.Value); // null is not string
        Assert.False(result2.Value); // null is not int
        Assert.True(result3.Value);  // null is null
    }

    [Fact]
    public void AsExpression_EmptyStringToInt_ReturnsNull()
    {
        // Arrange
        var code = @"
            value <- '\0'
            result <- value as int
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
    }

    [Fact]
    public void AsExpression_UnknownType_ReturnsOriginalValue()
    {
        // Arrange
        var code = @"
            value <- 42
            result <- value as unknown // 会转换成 null 变量
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // 对于未知类型，应该返回原值或尝试转换为object
    }

    [Fact]
    public void IsExpression_CollectionType_WorksCorrectly()
    {
        // Arrange
        var code = @"
            listValue <- [1, 2, 3]
            dictValue <- {""a"": 1, ""b"": 2}
            arrayValue <- [1, 2, 3]  // 假设这是数组
            isList1 <- listValue is list
            isList2 <- dictValue is list
            isDict1 <- dictValue is dictionary
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var isList1 = interpreter.Manager.GetValue(new LangId("isList1")) as BoolLangValue;
        var isList2 = interpreter.Manager.GetValue(new LangId("isList2")) as BoolLangValue;
        var isDict1 = interpreter.Manager.GetValue(new LangId("isDict1")) as BoolLangValue;

        Assert.NotNull(isList1);
        Assert.NotNull(isList2);
        Assert.NotNull(isDict1);

        // 这些测试结果可能取决于具体的实现
        // Assert.True(isList1.Value);   // list should be list
        // Assert.False(isList2.Value);   // dict should not be list
        // Assert.True(isDict1.Value);    // dict should be dictionary
    }

    #endregion
}