using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.EdgeCases;

/// <summary>
/// Defer 语句边界测试和错误测试
/// </summary>
[Collection("Sequential")]
public class DeferStatementEdgeCasesTests
{
    #region 边界条件测试

    /// <summary>
    /// 测试空的 defer 块
    /// </summary>
    [Fact]
    public void DeferStatement_EmptyBlock_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- ""ok""
func test() {
    defer {
    }
    result <- ""executed""
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("executed", ((StringLangValue)result).Value);
    }

    /// <summary>
    /// 测试 defer 语句最大嵌套深度（在函数嵌套中）
    /// </summary>
    [Fact]
    public void DeferStatement_DeepNesting_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
order <- """"
func level5() {
    defer order <- order + ""5""
    order <- order + ""B5""
}
func level4() {
    defer order <- order + ""4""
    level5()
}
func level3() {
    defer order <- order + ""3""
    level4()
}
func level2() {
    defer order <- order + ""2""
    level3()
}
func level1() {
    defer order <- order + ""1""
    level2()
}
level1()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("order"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        // 执行顺序：B5, defer5, defer4, defer3, defer2, defer1
        Assert.Equal("B554321", ((StringLangValue)result).Value);
    }

    /// <summary>
    /// 测试大量 defer 语句（100个）
    /// </summary>
    [Fact]
    public void DeferStatement_ManyDefers_ExecutesInCorrectOrder()
    {
        // Arrange
        var code = @"
count <- 0
func test() {
";
        for (int i = 0; i < 100; i++)
        {
            code += $"    defer count <- count + 1\n";
        }
        code += @"}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("count"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(100, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试循环中注册大量 defer
    /// </summary>
    [Fact]
    public void DeferStatement_ManyDefersInLoop_ExecutesAll()
    {
        // Arrange
        var code = @"
count <- 0
func test() {
    for i <- 0, i < 50, i++ {
        defer count <- count + 1
    }
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("count"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(50, ((IntLangValue)result).Value);
    }

    #endregion

    #region 异常处理边界测试

    /// <summary>
    /// 测试 defer 中抛出异常（不应阻止其他 defer 执行）
    /// </summary>
    [Fact]
    public void DeferStatement_ThrowException_ContinuesOtherDefers()
    {
        // Arrange
        var code = @"
count <- 0
func test() {
    defer count <- count + 1  // defer 3
    defer throw ""Intentional error""  // defer 2 - 会抛异常
    defer count <- count + 1  // defer 1
}
try {
    test()
} catch (e) {
    // 捕获异常
}
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("count"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // defer 1 和 defer 3 都应该执行
        Assert.Equal(2, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试函数主体抛出异常后 defer 仍然执行
    /// </summary>
    [Fact]
    public void DeferStatement_FunctionThrowsException_StillExecutesDefer()
    {
        // Arrange
        var code = @"
deferExecuted <- false
func test() {
    defer deferExecuted <- true
    throw ""Error in function body""
}
try {
    test()
} catch (e) {
    // 捕获异常
}
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("deferExecuted"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    /// <summary>
    /// 测试 defer 块中包含 try-catch
    /// </summary>
    [Fact]
    public void DeferStatement_WithTryCatchInside_HandlesErrors()
    {
        // Arrange
        var code = @"
result <- """"
func test() {
    defer {
        try {
            throw ""error""
        } catch (e) {
            result <- ""caught""
        }
    }
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("caught", ((StringLangValue)result).Value);
    }

    #endregion

    #region 控制流边界测试

    /// <summary>
    /// 测试 defer 在 break 之前执行
    /// </summary>
    [Fact]
    public void DeferStatement_WithBreak_ExecutesBeforeBreak()
    {
        // Arrange
        var code = @"
count <- 0
func test() {
    for i <- 0, i < 10, i++ {
        defer count <- count + 1
        if i == 5 {
            break
        }
    }
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("count"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // 循环6次（i=0到5），每次注册1个defer
        Assert.Equal(6, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试 defer 在 continue 时行为
    /// </summary>
    [Fact]
    public void DeferStatement_WithContinue_ExecutesForEachIteration()
    {
        // Arrange
        var code = @"
count <- 0
func test() {
    for i <- 0, i < 5, i++ {
        defer count <- count + 1
        if i == 2 {
            continue
        }
    }
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("count"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // 循环5次，每次注册1个defer
        Assert.Equal(5, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试多个 return 路径的 defer 执行
    /// </summary>
    [Fact]
    public void DeferStatement_MultipleReturnPaths_ExecutesOnce()
    {
        // Arrange
        var code = @"
count <- 0
func test(x:int) -> int {
    defer count <- count + 1
    if x > 0 {
        return x
    } elif x < 0 {
        return -x
    } else {
        return 0
    }
}
test(10)
test(-5)
test(0)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("count"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // 调用3次函数，每次执行1次defer
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    #endregion

    #region 变量作用域边界测试

    /// <summary>
    /// 测试 defer 访问循环变量
    /// </summary>
    [Fact]
    public void DeferStatement_AccessLoopVariable_CapturesValue()
    {
        // Arrange
        var code = @"
sum <- 0
func test() {
    for i <- 0, i < 5, i++ {
        defer sum <- sum + i
    }
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("sum"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // defer按LIFO执行：i=4,3,2,1,0，累加 = 4+3+2+1+0 = 10
        Assert.Equal(10, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试 defer 访问已修改的变量（闭包行为）
    /// </summary>
    [Fact]
    public void DeferStatement_ClosureVariable_SeesModifiedValue()
    {
        // Arrange
        var code = @"
result <- 0
func test() {
    x <- 10
    defer result <- x
    x <- 20
    defer result <- result + x
    x <- 30
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // defer按LIFO执行：
        // defer 2: result = 0 + 30 = 30
        // defer 1: result = 30 (x的最终值是30)
        Assert.Equal(60, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试 defer 访问函数参数
    /// </summary>
    [Fact]
    public void DeferStatement_AccessParameter_AccessesCorrectly()
    {
        // Arrange
        var code = @"
result <- 0
func test(x:int, y:int) {
    defer result <- x + y
}
test(15, 25)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(40, ((IntLangValue)result).Value);
    }

    #endregion

    #region 资源管理边界测试

    /// <summary>
    /// 测试 defer 与 using 结合（两种资源管理方式）
    /// </summary>
    [Fact]
    public void DeferStatement_WithUsing_ExecutesInCorrectOrder()
    {
        // Arrange
        var code = @"
order <- """"
func test() {
    defer order <- order + ""D1""

    using mutex <- MutexCreate() {
        MutexLock(mutex)
        defer order <- order + ""D2""
        order <- order + ""U""
        MutexUnlock(mutex)
    }

    defer order <- order + ""D3""
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("order"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        // 执行顺序：U -> D2(函数内defer) -> D3 -> D1
        Assert.Equal("UD2D3D1", ((StringLangValue)result).Value);
    }

    /// <summary>
    /// 测试多个资源的 defer 清理（LIFO顺序）
    /// </summary>
    [Fact]
    public void DeferStatement_MultipleResourcesCleanup_FollowsLIFO()
    {
        // Arrange
        var code = @"
order <- """"
func test() {
    m1 <- MutexCreate()
    defer {
        MutexDispose(m1)
        order <- order + ""M1""
    }

    m2 <- MutexCreate()
    defer {
        MutexDispose(m2)
        order <- order + ""M2""
    }

    m3 <- MutexCreate()
    defer {
        MutexDispose(m3)
        order <- order + ""M3""
    }
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("order"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        // LIFO: M3 -> M2 -> M1
        Assert.Equal("M3M2M1", ((StringLangValue)result).Value);
    }

    #endregion

    #region 复杂表达式边界测试

    /// <summary>
    /// 测试 defer 中使用复杂表达式
    /// </summary>
    [Fact]
    public void DeferStatement_ComplexExpression_EvaluatesCorrectly()
    {
        // Arrange
        var code = @"
result <- 0
func test(x:int) {
    y <- 10
    defer result <- (x + y) * 2 + 5
    y <- 20
}
test(15)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // defer执行时 y=20，所以 (15+20)*2+5 = 75
        Assert.Equal(75, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试 defer 中使用三元运算符
    /// </summary>
    [Fact]
    public void DeferStatement_TernaryOperator_EvaluatesCorrectly()
    {
        // Arrange
        var code = @"
result <- 0
func test(flag:bool) {
    defer result <- (flag ? 100 : 200)
}
test(true)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(100, ((IntLangValue)result).Value);
    }

    #endregion

    #region 递归函数中的 defer 测试

    /// <summary>
    /// 测试递归函数中的 defer
    /// </summary>
    [Fact]
    public void DeferStatement_InRecursiveFunction_ExecutesForEachCall()
    {
        // Arrange
        var code = @"
count <- 0
func factorial(n:int) -> int {
    defer count <- count + 1
    if n <= 1 {
        return 1
    }
    return n * factorial(n - 1)
}
result <- factorial(5)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var countResult = interpreter.Manager.GetValue(new LangId("count"));
        Assert.NotNull(countResult);
        Assert.IsType<IntLangValue>(countResult);
        // factorial(5) 递归调用5次
        Assert.Equal(5, ((IntLangValue)countResult).Value);

        var factorialResult = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(factorialResult);
        Assert.IsType<IntLangValue>(factorialResult);
        Assert.Equal(120, ((IntLangValue)factorialResult).Value); // 5! = 120
    }

    #endregion

    #region 性能边界测试

    /// <summary>
    /// 测试大量顺序函数调用，每个都有 defer
    /// </summary>
    [Fact]
    public void DeferStatement_ManyFunctionCalls_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
count <- 0
func increment() {
    defer count <- count + 1
}
";
        for (int i = 0; i < 100; i++)
        {
            code += "increment()\n";
        }

        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("count"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(100, ((IntLangValue)result).Value);
    }

    #endregion

    #region Lambda 和闭包边界测试

    /// <summary>
    /// 测试 Lambda 表达式中的 defer
    /// </summary>
    [Fact]
    public void DeferStatement_InLambda_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
result <- 0
action <- () -> {
    defer result <- 42
    result <- 10
}
action()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var resultValue = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(resultValue);
        Assert.IsType<IntLangValue>(resultValue);
        // Lambda执行：result=10，然后defer执行：result=42
        Assert.Equal(42, ((IntLangValue)resultValue).Value);
    }

    /// <summary>
    /// 测试闭包捕获外部变量的 defer
    /// </summary>
    [Fact]
    public void DeferStatement_ClosureCapturesVariable_WorksCorrectly()
    {
        // Arrange
        var code = @"
result <- 0
func makeIncrementer() {
    x <- 0
    return () -> {
        defer result <- x
        x <- x + 10
    }
}
inc <- makeIncrementer()
inc()
inc()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var resultValue = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(resultValue);
        Assert.IsType<IntLangValue>(resultValue);
        // 第二次调用：x=10，defer看到x=10
        Assert.Equal(10, ((IntLangValue)resultValue).Value);
    }

    #endregion
}
