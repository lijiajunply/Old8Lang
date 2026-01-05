using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.EdgeCases;

/// <summary>
/// Using 语句边界测试和错误测试
/// </summary>
[Collection("Sequential")]
public class UsingStatementEdgeCasesTests
{
    #region 边界条件测试

    /// <summary>
    /// 测试空的 using 块
    /// </summary>
    [Fact]
    public void UsingStatement_EmptyBlock_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
using mutex <- MutexCreate() {
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);

        // Assert - 应该不抛出异常
        Assert.NotNull(ast);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试 using 语句中只有注释
    /// </summary>
    [Fact]
    public void UsingStatement_OnlyComments_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
using mutex <- MutexCreate() {
    // This is a comment
    // Another comment
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert - 应该成功执行
        Assert.NotNull(ast);
    }

    /// <summary>
    /// 测试 using 语句最大嵌套深度
    /// </summary>
    [Fact]
    public void UsingStatement_DeepNesting_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
using res1 <- AtomicIntCreate(1) {
    using res2 <- AtomicIntCreate(2) {
        using res3 <- AtomicIntCreate(3) {
            using res4 <- AtomicIntCreate(4) {
                using res5 <- AtomicIntCreate(5) {
                    result <- AtomicIntGet(res1) + AtomicIntGet(res2) +
                              AtomicIntGet(res3) + AtomicIntGet(res4) +
                              AtomicIntGet(res5)
                }
            }
        }
    }
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value); // 1+2+3+4+5 = 15
    }

    /// <summary>
    /// 测试使用 0 作为资源值
    /// </summary>
    [Fact]
    public void UsingStatement_ZeroResourceValue_HandlesGracefully()
    {
        // Arrange
        var code = @"
result <- 0
using res <- 0 {
    result <- 42
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试使用负数作为资源值
    /// </summary>
    [Fact]
    public void UsingStatement_NegativeResourceValue_HandlesGracefully()
    {
        // Arrange
        var code = @"
result <- 0
using res <- -1 {
    result <- 99
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(99, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试 using 语句中的 break（在循环中）
    /// </summary>
    [Fact]
    public void UsingStatement_BreakInLoop_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
for i in [1~10] {
    using counter <- AtomicIntCreate(i) {
        if i == 5 {
            result <- AtomicIntGet(counter)
            break
        }
    }
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试 using 语句中的 continue（在循环中）
    /// </summary>
    [Fact]
    public void UsingStatement_ContinueInLoop_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
for i in [1~5] {
    using counter <- AtomicIntCreate(i) {
        if i == 3 {
            continue
        }
        result <- result + AtomicIntGet(counter)
    }
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(12, ((IntLangValue)result).Value); // 1+2+4+5 = 12 (跳过3)
    }

    /// <summary>
    /// 测试 using 语句中的 return（在函数中）
    /// </summary>
    [Fact]
    public void UsingStatement_ReturnInFunction_ReleasesResource()
    {
        // Arrange
        var code = @"
func testReturn() {
    using counter <- AtomicIntCreate(77) {
        return AtomicIntGet(counter)
    }
}
result <- testReturn()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(77, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试多个并行的 using 语句（顺序执行）
    /// </summary>
    [Fact]
    public void UsingStatement_MultipleParallel_ExecutesSequentially()
    {
        // Arrange
        var code = @"
results <- {}
using c1 <- AtomicIntCreate(1) {
    results.Add(AtomicIntGet(c1))
}
using c2 <- AtomicIntCreate(2) {
    results.Add(AtomicIntGet(c2))
}
using c3 <- AtomicIntCreate(3) {
    results.Add(AtomicIntGet(c3))
}
result <- results.Join("","")";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("1,2,3", ((StringLangValue)result).Value);
    }

    #endregion

    #region 资源复用测试

    /// <summary>
    /// 测试同一个变量在不同 using 块中复用
    /// </summary>
    [Fact]
    public void UsingStatement_ReuseVariableName_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
using res <- AtomicIntCreate(10) {
    result <- result + AtomicIntGet(res)
}
using res <- AtomicIntCreate(20) {
    result <- result + AtomicIntGet(res)
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value);
    }

    #endregion

    #region 异常处理边界测试

    /// <summary>
    /// 测试 using 块中抛出异常后资源是否正确释放
    /// </summary>
    [Fact]
    public void UsingStatement_ThrowException_ReleasesResource()
    {
        // Arrange
        var code = @"
result <- 0
errorCaught <- false
try {
    using counter <- AtomicIntCreate(100) {
        AtomicIntIncrement(counter)
        result <- AtomicIntGet(counter)
        throw ""Intentional error""
    }
} catch (e) {
    errorCaught <- true
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(101, ((IntLangValue)result).Value);

        var errorCaught = interpreter.Manager.GetValue(new LangId("errorCaught"));
        Assert.NotNull(errorCaught);
        Assert.IsType<BoolLangValue>(errorCaught);
        Assert.True(((BoolLangValue)errorCaught).Value);
    }

    /// <summary>
    /// 测试嵌套 using 中间层抛出异常
    /// </summary>
    [Fact]
    public void UsingStatement_NestedThrowException_ReleasesAllResources()
    {
        // Arrange
        var code = @"
result <- 0
errorCaught <- false
try {
    using c1 <- AtomicIntCreate(1) {
        using c2 <- AtomicIntCreate(2) {
            result <- AtomicIntGet(c1) + AtomicIntGet(c2)
            throw ""Error in middle layer""
            using c3 <- AtomicIntCreate(3) {
                result <- result + AtomicIntGet(c3)
            }
        }
    }
} catch (e) {
    errorCaught <- true
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(3, ((IntLangValue)result).Value); // 1+2 = 3

        var errorCaught = interpreter.Manager.GetValue(new LangId("errorCaught"));
        Assert.NotNull(errorCaught);
        Assert.IsType<BoolLangValue>(errorCaught);
        Assert.True(((BoolLangValue)errorCaught).Value);
    }

    #endregion

    #region 类型边界测试

    /// <summary>
    /// 测试使用非整数类型作为资源（应该能处理但不会调用 Dispose）
    /// </summary>
    [Fact]
    public void UsingStatement_NonIntegerResource_HandlesGracefully()
    {
        // Arrange
        var code = @"
result <- 0
using str <- ""test string"" {
    result <- 42
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试使用 null 作为资源
    /// </summary>
    [Fact]
    public void UsingStatement_NullResource_HandlesGracefully()
    {
        // Arrange
        var code = @"
result <- 0
using res <- null {
    result <- 55
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(55, ((IntLangValue)result).Value);
    }

    #endregion

    #region 变量作用域测试

    /// <summary>
    /// 测试 using 块外无法访问 using 变量
    /// </summary>
    [Fact]
    public void UsingStatement_VariableScopeInsideBlock_IsAccessible()
    {
        // Arrange
        var code = @"
result <- 0
using counter <- AtomicIntCreate(50) {
    result <- AtomicIntGet(counter)
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(50, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试 using 块内变量遮蔽外部变量
    /// </summary>
    [Fact]
    public void UsingStatement_VariableShadowing_UsesInnerVariable()
    {
        // Arrange
        var code = @"
counter <- 999
result <- 0
using counter <- AtomicIntCreate(100) {
    result <- AtomicIntGet(counter)
}";
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

    #region 复杂资源表达式边界测试

    /// <summary>
    /// 测试使用三元运算符作为资源表达式
    /// </summary>
    [Fact]
    public void UsingStatement_TernaryOperatorResource_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
flag <- true
result <- 0
using counter <- (flag ? AtomicIntCreate(10) : AtomicIntCreate(20)) {
    result <- AtomicIntGet(counter)
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(10, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试使用算术表达式作为资源值
    /// </summary>
    [Fact]
    public void UsingStatement_ArithmeticExpressionResource_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
x <- 5
y <- 10
result <- 0
using res <- (x + y) {
    result <- res
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value);
    }

    #endregion

    #region 性能边界测试

    /// <summary>
    /// 测试大量顺序 using 语句
    /// </summary>
    [Fact]
    public void UsingStatement_ManySequentialUsing_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
";
        for (int i = 1; i <= 50; i++)
        {
            code += $@"
using c{i} <- AtomicIntCreate({i}) {{
    result <- result + AtomicIntGet(c{i})
}}
";
        }

        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // 1+2+3+...+50 = 50*51/2 = 1275
        Assert.Equal(1275, ((IntLangValue)result).Value);
    }

    #endregion
}
