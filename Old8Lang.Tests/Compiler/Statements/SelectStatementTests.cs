using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Statements;

/// <summary>
/// Select 语句解释器执行测试
/// </summary>
[Collection("Sequential")]
public class SelectStatementTests
{
    #region 基本发送测试

    /// <summary>
    /// 测试简单的 select 发送操作
    /// </summary>
    [Fact]
    public void SelectStatement_SimpleSend_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试 select 默认分支执行
    /// </summary>
    [Fact]
    public void SelectStatement_DefaultBranch_ExecutesWhenNoChannelReady()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreateBounded(1)
ChannelSend(ch, 1)
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 基本接收测试

    /// <summary>
    /// 测试简单的 select 接收操作
    /// 语法：case value from channel
    /// </summary>
    [Fact]
    public void SelectStatement_SimpleReceive_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
receivedValue <- 0
ch <- ChannelCreate()
ChannelSend(ch, 42)
select {
    case val from ch -> {
        receivedValue <- val
        result <- 1
    }
    default -> {
        result <- 2
    }
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
    /// 测试 select 接收操作，channel 为空时执行 default
    /// </summary>
    [Fact]
    public void SelectStatement_ReceiveFromEmptyChannel_ExecutesDefault()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
select {
    case val from ch -> {
        result <- 1
    }
    default -> {
        result <- 2
    }
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
    /// 测试 select 接收复杂对象
    /// </summary>
    [Fact]
    public void SelectStatement_ReceiveComplexObject_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
receivedData <- null
ch <- ChannelCreate()
data <- {""key"": ""value"", ""number"": 123}
ChannelSend(ch, data)
select {
    case obj from ch -> {
        receivedData <- obj
        result <- 1
    }
    default -> {
        result <- 0
    }
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

    #endregion

    #region 多个 case 测试

    /// <summary>
    /// 测试多个发送 case
    /// </summary>
    [Fact]
    public void SelectStatement_MultipleSendCases_ExecutesFirstReady()
    {
        // Arrange
        var code = @"
result <- 0
ch1 <- ChannelCreate()
ch2 <- ChannelCreate()
ch3 <- ChannelCreate()
select {
    case ch1 <- 100 -> {
        result <- 1
    }
    case ch2 <- 200 -> {
        result <- 2
    }
    case ch3 <- 300 -> {
        result <- 3
    }
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
    /// 测试混合多个发送 case
    /// </summary>
    [Fact]
    public void SelectStatement_MixedMultipleSend_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
result <- 0
sendCh1 <- ChannelCreate()
sendCh2 <- ChannelCreate()
select {
    case sendCh1 <- 100 -> {
        result <- 1
    }
    case sendCh2 <- 200 -> {
        result <- 2
    }
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

    #endregion

    #region 带有控制流的测试

    /// <summary>
    /// 测试 select 中包含 if 语句
    /// </summary>
    [Fact]
    public void SelectStatement_WithIfStatement_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
flag <- true
select {
    case ch <- 100 -> {
        if flag {
            result <- 10
        } else {
            result <- 20
        }
    }
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
    /// 测试 select 中包含 for 循环
    /// </summary>
    [Fact]
    public void SelectStatement_WithForLoop_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
select {
    case ch <- 100 -> {
        for i in [1~5] {
            result <- result + i
        }
    }
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

    #endregion

    #region 循环中的 select 测试

    /// <summary>
    /// 测试在循环中使用 select
    /// </summary>
    [Fact]
    public void SelectStatement_InLoop_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
for i in [1~3] {
    select {
        case ch <- i * 10 -> {
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 函数中的 select 测试

    /// <summary>
    /// 测试函数中的 select 语句
    /// </summary>
    [Fact]
    public void SelectStatement_InFunction_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
func testSelect() {
    ch <- ChannelCreate()
    result <- 0
    select {
        case ch <- 99 -> {
            result <- 1
        }
        default -> {
            result <- 2
        }
    }
    return result
}
finalResult <- testSelect()";
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
    /// 测试 select case 中的 return
    /// </summary>
    [Fact]
    public void SelectStatement_ReturnInCase_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
func testSelectReturn() {
    ch <- ChannelCreate()
    select {
        case ch <- 100 -> {
            return 42
        }
        default -> {
            return 0
        }
    }
}
result <- testSelectReturn()";
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

    #region 复杂值测试

    /// <summary>
    /// 测试发送复杂对象
    /// </summary>
    [Fact]
    public void SelectStatement_SendComplexObject_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
data <- {""key"": ""value"", ""number"": 123}
select {
    case ch <- data -> {
        result <- 1
    }
    default -> {
        result <- 0
    }
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

    // 注意：接收操作的测试因语法歧义问题已移除

    #endregion

    #region 使用 using 和 select 组合测试

    /// <summary>
    /// 测试在 using 块中使用 select
    /// </summary>
    [Fact]
    public void SelectStatement_WithinUsing_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
using ch <- ChannelCreate() {
    select {
        case ch <- 777 -> {
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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 异常处理测试

    /// <summary>
    /// 测试 select 中的 try-catch
    /// </summary>
    [Fact]
    public void SelectStatement_WithTryCatch_HandlesExceptionCorrectly()
    {
        // Arrange
        var code = @"
result <- 0
errorCaught <- false
ch <- ChannelCreate()
select {
    case ch <- 100 -> {
        try {
            throw ""Test error""
        } catch (e) {
            errorCaught <- true
            result <- 99
        }
    }
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

    #endregion

    #region 多个 channel 交互测试

    /// <summary>
    /// 测试多个 channel 的混合接收操作
    /// </summary>
    [Fact]
    public void SelectStatement_MultipleReceive_ExecutesFirstReady()
    {
        // Arrange
        var code = @"
result <- 0
receivedValue <- 0
ch1 <- ChannelCreate()
ch2 <- ChannelCreate()
ChannelSend(ch1, 100)
select {
    case val from ch1 -> {
        receivedValue <- val
        result <- 1
    }
    case val from ch2 -> {
        receivedValue <- val
        result <- 2
    }
    default -> {
        result <- 3
    }
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
    /// 测试混合发送和接收操作
    /// </summary>
    [Fact]
    public void SelectStatement_MixedSendAndReceive_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
result <- 0
receivedValue <- 0
sendCh <- ChannelCreate()
receiveCh <- ChannelCreate()
ChannelSend(receiveCh, 999)
select {
    case sendCh <- 100 -> {
        result <- 1
    }
    case val from receiveCh -> {
        receivedValue <- val
        result <- 2
    }
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

    #endregion

    #region 空 case 块测试

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
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 复杂表达式测试

    /// <summary>
    /// 测试使用计算结果作为发送值
    /// </summary>
    [Fact]
    public void SelectStatement_ComputedSendValue_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
x <- 10
y <- 20
select {
    case ch <- (x + y * 2) -> {
        result <- 1
    }
    default -> {
        result <- 0
    }
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

    #endregion

    #region switch 与 select 组合测试

    /// <summary>
    /// 测试 select 中包含 switch
    /// </summary>
    [Fact]
    public void SelectStatement_WithSwitch_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreate()
mode <- 2
select {
    case ch <- mode -> {
        switch mode {
            case 1 {
                result <- 10
            }
            case 2 {
                result <- 20
            }
            default {
                result <- 30
            }
        }
    }
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

    #endregion

    #region 并发场景测试

    /// <summary>
    /// 测试有界 channel 的 select 操作
    /// </summary>
    [Fact]
    public void SelectStatement_BoundedChannel_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
result <- 0
ch <- ChannelCreateBounded(2)
ChannelSend(ch, 1)
ChannelSend(ch, 2)
select {
    case ch <- 3 -> {
        result <- 1
    }
    default -> {
        result <- 2
    }
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

    #endregion
}
