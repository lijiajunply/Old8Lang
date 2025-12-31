using Xunit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Collections;

/// <summary>
/// 列表推导式测试 - 测试 List Comprehension 功能
/// </summary>
[Collection("Sequential")]
public class ListComprehensionTests
{
    #region 基础列表推导式测试

    /// <summary>
    /// 测试简单的列表推导式 - [x for x in list]
    /// </summary>
    [Fact]
    public void Run_SimpleListComprehension_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- [x for x in [1, 2, 3, 4, 5]]
sum <- 0
for item in result {
    sum <- sum + item
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);
        Assert.Equal(5, ((ListLangValue)result).Values.Count);

        var sum = interpreter.Manager.GetValue(new LangId("sum"));
        Assert.IsType<IntLangValue>(sum);
        Assert.Equal(15, ((IntLangValue)sum).Value); // 1+2+3+4+5 = 15
    }

    /// <summary>
    /// 测试列表推导式中的表达式 - [x * 2 for x in list]
    /// </summary>
    [Fact]
    public void Run_ListComprehensionWithExpression_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- [x * 2 for x in [1, 2, 3, 4, 5]]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(5, list.Values.Count);
        Assert.Equal(2, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(4, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(6, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(8, ((IntLangValue)list.Values[3]).Value);
        Assert.Equal(10, ((IntLangValue)list.Values[4]).Value);
    }

    /// <summary>
    /// 测试列表推导式遍历范围 - [x * x for x in [1~5]]
    /// </summary>
    [Fact]
    public void Run_ListComprehensionWithRange_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- [x * x for x in [1~5]]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(5, list.Values.Count);
        Assert.Equal(1, ((IntLangValue)list.Values[0]).Value);   // 1*1
        Assert.Equal(4, ((IntLangValue)list.Values[1]).Value);   // 2*2
        Assert.Equal(9, ((IntLangValue)list.Values[2]).Value);   // 3*3
        Assert.Equal(16, ((IntLangValue)list.Values[3]).Value);  // 4*4
        Assert.Equal(25, ((IntLangValue)list.Values[4]).Value);  // 5*5
    }

    #endregion

    #region 带条件的列表推导式测试

    /// <summary>
    /// 测试带单个条件的列表推导式 - [x for x in list if x > 5]
    /// </summary>
    [Fact]
    public void Run_ListComprehensionWithCondition_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- [x for x in [1, 2, 3, 4, 5, 6, 7, 8, 9, 10] if x > 5]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(5, list.Values.Count); // 6, 7, 8, 9, 10
        Assert.Equal(6, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(7, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(8, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(9, ((IntLangValue)list.Values[3]).Value);
        Assert.Equal(10, ((IntLangValue)list.Values[4]).Value);
    }

    /// <summary>
    /// 测试偶数筛选 - [x for x in list if x % 2 == 0]
    /// </summary>
    [Fact]
    public void Run_ListComprehensionFilterEven_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- [x for x in [1~10] if x % 2 == 0]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(5, list.Values.Count); // 2, 4, 6, 8, 10
        Assert.Equal(2, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(4, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(6, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(8, ((IntLangValue)list.Values[3]).Value);
        Assert.Equal(10, ((IntLangValue)list.Values[4]).Value);
    }

    /// <summary>
    /// 测试带条件和表达式的推导式 - [x * x for x in list if x % 2 == 0]
    /// </summary>
    [Fact]
    public void Run_ListComprehensionWithConditionAndExpression_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- [x * x for x in [1~10] if x % 2 == 0]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(5, list.Values.Count);
        Assert.Equal(4, ((IntLangValue)list.Values[0]).Value);   // 2*2
        Assert.Equal(16, ((IntLangValue)list.Values[1]).Value);  // 4*4
        Assert.Equal(36, ((IntLangValue)list.Values[2]).Value);  // 6*6
        Assert.Equal(64, ((IntLangValue)list.Values[3]).Value);  // 8*8
        Assert.Equal(100, ((IntLangValue)list.Values[4]).Value); // 10*10
    }

    #endregion

    #region 字符串列表推导式测试

    /// <summary>
    /// 测试遍历字符串 - [c for c in "hello"]
    /// </summary>
    [Fact]
    public void Run_ListComprehensionOverString_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- [c for c in ""hello""]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(5, list.Values.Count);
        Assert.Equal('h', ((CharLangValue)list.Values[0]).Value);
        Assert.Equal('e', ((CharLangValue)list.Values[1]).Value);
        Assert.Equal('l', ((CharLangValue)list.Values[2]).Value);
        Assert.Equal('l', ((CharLangValue)list.Values[3]).Value);
        Assert.Equal('o', ((CharLangValue)list.Values[4]).Value);
    }

    /// <summary>
    /// 测试字符串转大写 - [c.ToUpper() for c in string]
    /// </summary>
    [Fact]
    public void Run_ListComprehensionStringTransform_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
text <- ""hello""
result <- [c.ToUpper() for c in text]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(5, list.Values.Count);
    }

    #endregion

    #region 嵌套循环列表推导式测试

    /// <summary>
    /// 测试嵌套循环推导式 - [x+y for x in list1 for y in list2]
    /// </summary>
    [Fact]
    public void Run_ListComprehensionNestedLoops_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- [x + y for x in [1, 2] for y in [10, 20]]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(4, list.Values.Count);
        Assert.Equal(11, ((IntLangValue)list.Values[0]).Value); // 1+10
        Assert.Equal(21, ((IntLangValue)list.Values[1]).Value); // 1+20
        Assert.Equal(12, ((IntLangValue)list.Values[2]).Value); // 2+10
        Assert.Equal(22, ((IntLangValue)list.Values[3]).Value); // 2+20
    }

    /// <summary>
    /// 测试嵌套循环生成坐标对 - [(x,y) for x in [1~3] for y in [1~3]]
    /// </summary>
    [Fact]
    public void Run_ListComprehensionCoordinatePairs_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- [(x, y) for x in [1~3] for y in [1~3]]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(9, list.Values.Count); // 3*3 combinations
    }

    /// <summary>
    /// 测试嵌套循环带条件 - [x*y for x in [1~5] for y in [1~5] if x < y]
    /// </summary>
    [Fact]
    public void Run_ListComprehensionNestedLoopsWithCondition_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- [x * y for x in [1~3] for y in [1~3] if x < y]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        // x=1: y can be 2,3 -> 1*2=2, 1*3=3
        // x=2: y can be 3   -> 2*3=6
        // x=3: no valid y
        Assert.Equal(3, list.Values.Count);
        Assert.Equal(2, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(3, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(6, ((IntLangValue)list.Values[2]).Value);
    }

    #endregion

    #region 复杂表达式列表推导式测试

    /// <summary>
    /// 测试使用函数调用的推导式
    /// </summary>
    [Fact]
    public void Run_ListComprehensionWithFunctionCall_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
func square(n: int) -> int {
    return n * n
}

result <- [square(x) for x in [1~5]]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(5, list.Values.Count);
        Assert.Equal(1, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(4, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(9, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(16, ((IntLangValue)list.Values[3]).Value);
        Assert.Equal(25, ((IntLangValue)list.Values[4]).Value);
    }

    /// <summary>
    /// 测试使用三元表达式的推导式
    /// </summary>
    [Fact]
    public void Run_ListComprehensionWithTernary_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- [x % 2 == 0 ? x : -x for x in [1~5]]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(5, list.Values.Count);
        Assert.Equal(-1, ((IntLangValue)list.Values[0]).Value); // odd: -1
        Assert.Equal(2, ((IntLangValue)list.Values[1]).Value);  // even: 2
        Assert.Equal(-3, ((IntLangValue)list.Values[2]).Value); // odd: -3
        Assert.Equal(4, ((IntLangValue)list.Values[3]).Value);  // even: 4
        Assert.Equal(-5, ((IntLangValue)list.Values[4]).Value); // odd: -5
    }

    #endregion

    #region 边界情况测试

    /// <summary>
    /// 测试空列表推导式
    /// </summary>
    [Fact]
    public void Run_ListComprehensionEmptySource_ReturnsEmptyList()
    {
        // Arrange
        var code = @"
result <- [x for x in []]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);
        Assert.Equal(0, ((ListLangValue)result).Values.Count);
    }

    /// <summary>
    /// 测试条件永不满足的推导式
    /// </summary>
    [Fact]
    public void Run_ListComprehensionConditionNeverMet_ReturnsEmptyList()
    {
        // Arrange
        var code = @"
result <- [x for x in [1~10] if x > 100]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);
        Assert.Equal(0, ((ListLangValue)result).Values.Count);
    }

    /// <summary>
    /// 测试单元素列表的推导式
    /// </summary>
    [Fact]
    public void Run_ListComprehensionSingleElement_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- [x * 10 for x in [42]]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(1, list.Values.Count);
        Assert.Equal(420, ((IntLangValue)list.Values[0]).Value);
    }

    #endregion
}
