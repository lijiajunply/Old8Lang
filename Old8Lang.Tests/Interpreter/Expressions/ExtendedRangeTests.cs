using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.Intermediates;

namespace Old8Lang.Tests.Interpreter.Expressions;

/// <summary>
/// 扩展范围表达式测试（包含排除边界语法）
/// </summary>
public class ExtendedRangeTests
{
    [Fact]
    public void Range_InclusiveRange_WorksCorrectly()
    {
        // Arrange
        var code = @"
            result <- [1~5]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var rangeResult = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(rangeResult);
        Assert.IsType<ArrayLangValue>(rangeResult);

        var array = ((ArrayLangValue)rangeResult).GetItems().ToList();
        Assert.Equal(5, array.Count);
        Assert.Equal(1, ((IntLangValue)array[0]).Value);
        Assert.Equal(5, ((IntLangValue)array[4]).Value);
    }

    [Fact]
    public void Range_ExclusiveEndRange_WorksCorrectly()
    {
        // Arrange
        var code = @"
            result <- [1~<5]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var rangeResult = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(rangeResult);
        Assert.IsType<ArrayLangValue>(rangeResult);

        var array = ((ArrayLangValue)rangeResult).GetItems().ToList();
        Assert.Equal(4, array.Count);
        Assert.Equal(1, ((IntLangValue)array[0]).Value);
        Assert.Equal(4, ((IntLangValue)array[3]).Value);
    }

    [Fact]
    public void Range_ExclusiveStartRange_WorksCorrectly()
    {
        // Arrange
        var code = @"
            result <- [1>~5]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var rangeResult = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(rangeResult);
        Assert.IsType<ArrayLangValue>(rangeResult);

        var array = ((ArrayLangValue)rangeResult).GetItems().ToList();
        Assert.Equal(4, array.Count);
        Assert.Equal(2, ((IntLangValue)array[0]).Value);
        Assert.Equal(5, ((IntLangValue)array[3]).Value);
    }

    [Fact]
    public void Range_ExclusiveBothRange_WorksCorrectly()
    {
        // Arrange
        var code = @"
            result <- [1>~<5]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var rangeResult = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(rangeResult);
        Assert.IsType<ArrayLangValue>(rangeResult);

        var array = ((ArrayLangValue)rangeResult).GetItems().ToList();
        Assert.Equal(3, array.Count);
        Assert.Equal(2, ((IntLangValue)array[0]).Value);
        Assert.Equal(4, ((IntLangValue)array[2]).Value);
    }

    [Fact]
    public void Range_ExclusiveBothEmptyRange_ReturnsEmptyArray()
    {
        // Arrange
        var code = @"
            result <- [1>~<2]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var rangeResult = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(rangeResult);
        Assert.IsType<ArrayLangValue>(rangeResult);

        var array = ((ArrayLangValue)rangeResult).GetItems().ToList();
        Assert.Empty(array);
    }

    [Fact]
    public void Range_SingleElementInclusive_WorksCorrectly()
    {
        // Arrange
        var code = @"
            result <- [3~3]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var rangeResult = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(rangeResult);
        Assert.IsType<ArrayLangValue>(rangeResult);

        var array = ((ArrayLangValue)rangeResult).GetItems().ToList();
        Assert.Single(array);
        Assert.Equal(3, ((IntLangValue)array[0]).Value);
    }

    [Fact]
    public void Range_NegativeNumbers_WorksCorrectly()
    {
        // Arrange
        var code = @"
            result <- [-2>~<2]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var rangeResult = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(rangeResult);
        Assert.IsType<ArrayLangValue>(rangeResult);

        var array = ((ArrayLangValue)rangeResult).GetItems().ToList();
        Assert.Equal(3, array.Count);
        Assert.Equal(-1, ((IntLangValue)array[0]).Value);
        Assert.Equal(0, ((IntLangValue)array[1]).Value);
        Assert.Equal(1, ((IntLangValue)array[2]).Value);
    }

    [Fact]
    public void Range_VariablesInRange_WorksCorrectly()
    {
        // Arrange
        var code = @"
            start <- 10
            end <- 15
            result1 <- [start~<end]
            result2 <- [start>~end]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        var array1 = ((ArrayLangValue)result1).GetItems().ToList();
        Assert.Equal(5, array1.Count);
        Assert.Equal(10, ((IntLangValue)array1[0]).Value);
        Assert.Equal(14, ((IntLangValue)array1[4]).Value);

        var array2 = ((ArrayLangValue)result2).GetItems().ToList();
        Assert.Equal(5, array2.Count);
        Assert.Equal(11, ((IntLangValue)array2[0]).Value);
        Assert.Equal(15, ((IntLangValue)array2[4]).Value);
    }

    [Fact]
    public void Range_ToString_DisplaysCorrectly()
    {
        // Arrange & Act
        var inclusiveRange = new RangeLangValue(new IntLangValue(1), new IntLangValue(5), default, true, true);
        var exclusiveEndRange = new RangeLangValue(new IntLangValue(1), new IntLangValue(5), default, true, false);
        var exclusiveStartRange = new RangeLangValue(new IntLangValue(1), new IntLangValue(5), default, false, true);
        var exclusiveBothRange = new RangeLangValue(new IntLangValue(1), new IntLangValue(5), default, false, false);

        // Assert - ToString使用括号显示包含/排除状态
        Assert.Equal("[1~5]", inclusiveRange.ToString());      // 包含两边 []
        Assert.Equal("[1~5)", exclusiveEndRange.ToString());   // 包含start，排除end []
        Assert.Equal("(1~5]", exclusiveStartRange.ToString()); // 排除start，包含end ()
        Assert.Equal("(1~5)", exclusiveBothRange.ToString()); // 排除两边 ()
    }
}