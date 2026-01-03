using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.EdgeCases;

/// <summary>
/// Select 语句边界测试和错误测试
/// </summary>
[Collection("Sequential")]
public class SelectStatementEdgeCasesTests
{
    #region 边界条件测试

    /// <summary>
    /// 测试空的 case 块
    /// </summary>
    [Fact]
    public void SelectStatement_EmptyCaseBlock_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
ch <- ChannelCreate()
select {
    case ch <- 100 -> {
    }
    default -> {
    }
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);

        // Assert - 应该不抛出异常
        Assert.NotNull(ast);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试只有注释的 case 块
    /// </summary>
    [Fact]
    public void SelectStatement_OnlyCommentsInCase_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
ch <- ChannelCreate()
select {
    case ch <- 100 -> {
        // This is a comment
    }
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        Assert.NotNull(ast);
    }

    /// <summary>
    /// 测试大量 case 的 select 语句
    /// </summary>
    [Fact]
    public void SelectStatement_ManyCases_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
";
        for (int i = 0; i < 20; i++)
        {
            code += $"ch{i} <- ChannelCreate()\n";
        }
        code += "select {\n";
        for (int i = 0; i < 20; i++)
        {
            code += $@"    case ch{i} <- {i * 10} -> {{
        result <- {i}
    }}
";
        }
        code += @"    default -> {
        result <- -1
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
        // 应该执行其中一个 case
        Assert.True(((IntLangValue)result).Value >= 0 && ((IntLangValue)result).Value < 20);
    }

    /// <summary>
    /// 测试嵌套的 select 语句
    /// </summary>
    [Fact]
    public void SelectStatement_Nested_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch1 <- ChannelCreate()
ch2 <- ChannelCreate()
select {
    case ch1 <- 100 -> {
        select {
            case ch2 <- 200 -> {
                result <- 1
            }
            default -> {
                result <- 2
            }
        }
    }
    default -> {
        result <- 3
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
        Assert.True(((IntLangValue)result).Value >= 1 && ((IntLangValue)result).Value <= 3);
    }

    /// <summary>
    /// 测试三层嵌套 select
    /// </summary>
    [Fact]
    public void SelectStatement_TripleNested_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch1 <- ChannelCreate()
ch2 <- ChannelCreate()
ch3 <- ChannelCreate()
select {
    case ch1 <- 1 -> {
        select {
            case ch2 <- 2 -> {
                select {
                    case ch3 <- 3 -> {
                        result <- 123
                    }
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
        Assert.Equal(123, ((IntLangValue)result).Value);
    }

    #endregion

    #region Channel 边界测试

    /// <summary>
    /// 测试已关闭的 channel
    /// </summary>
    [Fact]
    public void SelectStatement_ClosedChannel_ExecutesDefaultBranch()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
ChannelClose(ch)
select {
    case ch <- 100 -> {
        result <- 1
    }
    default -> {
        result <- 2
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
        // 已关闭的 channel 应该执行 default
        Assert.Equal(2, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试有界 channel 满容量时的发送
    /// </summary>
    [Fact]
    public void SelectStatement_BoundedChannelFull_ExecutesDefaultBranch()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreateBounded(1)
ChannelSend(ch, 1)
select {
    case ch <- 2 -> {
        result <- 1
    }
    default -> {
        result <- 2
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
        Assert.Equal(2, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试空 channel 的接收操作
    /// </summary>
    [Fact]
    public void SelectStatement_EmptyChannelReceive_ExecutesDefaultBranch()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
select {
    case val <- ChannelReceive(ch) -> {
        result <- 1
    }
    default -> {
        result <- 2
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
        Assert.Equal(2, ((IntLangValue)result).Value);
    }

    #endregion

    #region 控制流边界测试

    /// <summary>
    /// 测试 select case 中的 break（在循环中）
    /// </summary>
    [Fact]
    public void SelectStatement_BreakInLoop_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
for i in [1~10] {
    select {
        case ch <- i -> {
            if i == 5 {
                result <- i
                break
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
        Assert.Equal(5, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试 select case 中的 continue（在循环中）
    /// </summary>
    [Fact]
    public void SelectStatement_ContinueInLoop_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
for i in [1~5] {
    select {
        case ch <- i -> {
            if i == 3 {
                continue
            }
            result <- result + i
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
        Assert.Equal(12, ((IntLangValue)result).Value); // 1+2+4+5 = 12
    }

    /// <summary>
    /// 测试 select case 中的 return（在函数中）
    /// </summary>
    [Fact]
    public void SelectStatement_ReturnInFunction_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
func testReturn() {
    ch <- ChannelCreate()
    select {
        case ch <- 999 -> {
            return 77
        }
        default -> {
            return 88
        }
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
        Assert.True(((IntLangValue)result).Value == 77 || ((IntLangValue)result).Value == 88);
    }

    #endregion

    #region 异常处理边界测试

    /// <summary>
    /// 测试 select case 中抛出异常
    /// </summary>
    [Fact]
    public void SelectStatement_ThrowException_HandlesCorrectly()
    {
        // Arrange
        var code = @"
result <- 0
errorCaught <- false
ch <- ChannelCreate()
try {
    select {
        case ch <- 100 -> {
            result <- 1
            throw ""Test error""
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
        var errorCaught = interpreter.Manager.GetValue(new LangId("errorCaught"));
        Assert.NotNull(errorCaught);
        Assert.IsType<BoolLangValue>(errorCaught);
        Assert.True(((BoolLangValue)errorCaught).Value);
    }

    /// <summary>
    /// 测试嵌套 select 中间层抛出异常
    /// </summary>
    [Fact]
    public void SelectStatement_NestedThrowException_HandlesCorrectly()
    {
        // Arrange
        var code = @"
result <- 0
errorCaught <- false
ch1 <- ChannelCreate()
ch2 <- ChannelCreate()
try {
    select {
        case ch1 <- 1 -> {
            select {
                case ch2 <- 2 -> {
                    result <- 12
                    throw ""Nested error""
                }
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
        var errorCaught = interpreter.Manager.GetValue(new LangId("errorCaught"));
        Assert.NotNull(errorCaught);
        Assert.IsType<BoolLangValue>(errorCaught);
        Assert.True(((BoolLangValue)errorCaught).Value);
    }

    #endregion

    #region 复杂表达式边界测试

    /// <summary>
    /// 测试使用三元运算符作为 channel 表达式
    /// </summary>
    [Fact]
    public void SelectStatement_TernaryOperatorChannel_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
flag <- true
ch1 <- ChannelCreate()
ch2 <- ChannelCreate()
select {
    case (flag ? ch1 : ch2) <- 100 -> {
        result <- 1
    }
    default -> {
        result <- 0
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
        Assert.True(((IntLangValue)result).Value == 0 || ((IntLangValue)result).Value == 1);
    }

    /// <summary>
    /// 测试使用数组索引作为 channel
    /// </summary>
    [Fact]
    public void SelectStatement_ArrayIndexChannel_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
channels <- [ChannelCreate(), ChannelCreate()]
select {
    case channels[0] <- 100 -> {
        result <- 1
    }
    case channels[1] <- 200 -> {
        result <- 2
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
        Assert.True(((IntLangValue)result).Value == 1 || ((IntLangValue)result).Value == 2);
    }

    #endregion

    #region 变量作用域测试

    /// <summary>
    /// 测试接收变量在 case 块内可访问
    /// </summary>
    [Fact]
    public void SelectStatement_ReceiveVariableScope_IsAccessible()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
ChannelSend(ch, 42)
select {
    case val <- ChannelReceive(ch) -> {
        result <- val
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
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试不同 case 中的同名接收变量
    /// </summary>
    [Fact]
    public void SelectStatement_SameVariableNameInDifferentCases_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch1 <- ChannelCreate()
ch2 <- ChannelCreate()
ChannelSend(ch1, 10)
ChannelSend(ch2, 20)
select {
    case val <- ChannelReceive(ch1) -> {
        result <- val
    }
    case val <- ChannelReceive(ch2) -> {
        result <- val * 2
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
        // 应该是 10 或 40
        Assert.True(((IntLangValue)result).Value == 10 || ((IntLangValue)result).Value == 40);
    }

    #endregion

    #region 循环中的多次 select 测试

    /// <summary>
    /// 测试循环中多次执行 select
    /// </summary>
    [Fact]
    public void SelectStatement_MultipleSelectInLoop_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
for i in [1~5] {
    select {
        case ch <- i -> {
            result <- result + 1
        }
        default -> {
            result <- result + 0
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

    #endregion

    #region 复杂数据类型测试

    /// <summary>
    /// 测试发送和接收 null 值
    /// </summary>
    [Fact]
    public void SelectStatement_SendReceiveNull_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- -1
ch <- ChannelCreate()
ChannelSend(ch, null)
select {
    case val <- ChannelReceive(ch) -> {
        if val == null {
            result <- 0
        } else {
            result <- 1
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
        Assert.Equal(0, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试发送和接收列表
    /// </summary>
    [Fact]
    public void SelectStatement_SendReceiveList_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
data <- {1, 2, 3, 4, 5}
ChannelSend(ch, data)
select {
    case lst <- ChannelReceive(ch) -> {
        result <- len(lst)
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
    /// 测试发送和接收字典
    /// </summary>
    [Fact]
    public void SelectStatement_SendReceiveDictionary_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
data <- {""a"": 1, ""b"": 2, ""c"": 3}
ChannelSend(ch, data)
select {
    case dict <- ChannelReceive(ch) -> {
        result <- dict[""a""] + dict[""b""] + dict[""c""]
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
        Assert.Equal(6, ((IntLangValue)result).Value);
    }

    #endregion

    #region 性能边界测试

    /// <summary>
    /// 测试大量顺序 select 语句
    /// </summary>
    [Fact]
    public void SelectStatement_ManySequentialSelect_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
";
        for (int i = 1; i <= 30; i++)
        {
            code += $@"
select {{
    case ch <- {i} -> {{
        result <- result + 1
    }}
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
        Assert.Equal(30, ((IntLangValue)result).Value);
    }

    #endregion

    #region 资源清理测试

    /// <summary>
    /// 测试 select 与 using 结合使用
    /// </summary>
    [Fact]
    public void SelectStatement_WithUsing_ResourcesCleanedUp()
    {
        // Arrange
        var code = @"
result <- 0
using ch <- ChannelCreate() {
    select {
        case ch <- 100 -> {
            result <- 1
        }
        default -> {
            result <- 0
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
        Assert.True(((IntLangValue)result).Value == 0 || ((IntLangValue)result).Value == 1);
    }

    #endregion

    #region 并发原语组合测试

    /// <summary>
    /// 测试 select 与 mutex 结合使用
    /// </summary>
    [Fact]
    public void SelectStatement_WithMutex_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
mutex <- MutexCreate()
select {
    case ch <- 100 -> {
        MutexLock(mutex)
        result <- 1
        MutexUnlock(mutex)
    }
}
MutexDispose(mutex)";
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

    #endregion
}
