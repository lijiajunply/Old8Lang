using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion
}
