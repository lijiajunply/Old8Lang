using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Statement;

/// <summary>
/// Select 语句解析测试
/// </summary>
[Collection("Sequential")]
public class SelectStatementTests
{
    #region 基本 Select 测试

    /// <summary>
    /// 测试简单的 select 语句（只有发送 case）
    /// </summary>
    [Fact]
    public void ParseSelectStatement_SimpleSendCase_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
ch <- ChannelCreate()
select {
    case ch <- 100 -> {
        PrintLine(""Sent"")
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.IsType<SelectStatement>(result[1]);
        var selectStmt = (SelectStatement)result[1];
        Assert.Single(selectStmt.Cases);
        Assert.Null(selectStmt.DefaultCase);
    }

    /// <summary>
    /// 测试带 default 的 select 语句
    /// </summary>
    [Fact]
    public void ParseSelectStatement_WithDefault_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
ch <- ChannelCreate()
select {
    case ch <- 100 -> {
        PrintLine(""Sent"")
    }
    default -> {
        PrintLine(""Default"")
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.IsType<SelectStatement>(result[1]);
        var selectStmt = (SelectStatement)result[1];
        Assert.Single(selectStmt.Cases);
        Assert.NotNull(selectStmt.DefaultCase);
    }

    /// <summary>
    /// 测试多个 case 的 select 语句
    /// </summary>
    [Fact]
    public void ParseSelectStatement_MultipleCases_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
ch1 <- ChannelCreate()
ch2 <- ChannelCreate()
ch3 <- ChannelCreate()
select {
    case ch1 <- 100 -> {
        PrintLine(""Sent to ch1"")
    }
    case ch2 <- 200 -> {
        PrintLine(""Sent to ch2"")
    }
    case ch3 <- 300 -> {
        PrintLine(""Sent to ch3"")
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        Assert.IsType<SelectStatement>(result[3]);
        var selectStmt = (SelectStatement)result[3];
        Assert.Equal(3, selectStmt.Cases.Count);
        Assert.Null(selectStmt.DefaultCase);
    }

    #endregion

    #region 发送和接收混合测试

    /// <summary>
    /// 测试混合发送和接收的 select 语句
    /// </summary>
    [Fact]
    public void ParseSelectStatement_MixedSendAndReceive_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
sendCh <- ChannelCreate()
receiveCh <- ChannelCreate()
select {
    case sendCh <- 100 -> {
        PrintLine(""Sent"")
    }
    case val <- ChannelReceive(receiveCh) -> {
        PrintLine(""Received: "" + val)
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.IsType<SelectStatement>(result[2]);
        var selectStmt = (SelectStatement)result[2];
        Assert.Equal(2, selectStmt.Cases.Count);
    }

    #endregion

    #region 复杂表达式测试

    /// <summary>
    /// 测试使用函数调用作为 channel 表达式
    /// </summary>
    [Fact]
    public void ParseSelectStatement_FunctionCallChannel_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func getChannel() {
    return ChannelCreate()
}
select {
    case getChannel() <- 100 -> {
        PrintLine(""Sent"")
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count >= 1);
        Assert.IsType<SelectStatement>(result[^1]);
    }

    /// <summary>
    /// 测试使用复杂表达式作为发送值
    /// </summary>
    [Fact]
    public void ParseSelectStatement_ComplexSendValue_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
ch <- ChannelCreate()
x <- 10
y <- 20
select {
    case ch <- (x + y * 2) -> {
        PrintLine(""Sent computed value"")
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        Assert.IsType<SelectStatement>(result[3]);
    }

    #endregion

    #region 嵌套和组合测试

    /// <summary>
    /// 测试 select 中包含 if 语句
    /// </summary>
    [Fact]
    public void ParseSelectStatement_WithIfStatement_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
ch <- ChannelCreate()
flag <- true
select {
    case ch <- 100 -> {
        if flag {
            PrintLine(""Flag is true"")
        } else {
            PrintLine(""Flag is false"")
        }
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.IsType<SelectStatement>(result[2]);
    }

    /// <summary>
    /// 测试 select 中包含循环
    /// </summary>
    [Fact]
    public void ParseSelectStatement_WithLoop_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
ch <- ChannelCreate()
select {
    case ch <- 100 -> {
        for i in [1~5] {
            PrintLine(i)
        }
    }
    default -> {
        PrintLine(""No channel ready"")
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.IsType<SelectStatement>(result[1]);
    }

    /// <summary>
    /// 测试嵌套的 select 语句
    /// </summary>
    [Fact]
    public void ParseSelectStatement_Nested_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
ch1 <- ChannelCreate()
ch2 <- ChannelCreate()
select {
    case ch1 <- 100 -> {
        select {
            case ch2 <- 200 -> {
                PrintLine(""Nested send"")
            }
        }
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.IsType<SelectStatement>(result[2]);
    }

    #endregion

    #region 单行语句测试

    /// <summary>
    /// 测试 case 中的单行语句（不带大括号）
    /// </summary>
    [Fact]
    public void ParseSelectStatement_SingleStatementCase_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
ch <- ChannelCreate()
select {
    case ch <- 100 ->
        PrintLine(""Sent"")
    default ->
        PrintLine(""Default"")
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.IsType<SelectStatement>(result[1]);
    }

    #endregion

    #region 空块测试

    /// <summary>
    /// 测试空的 case 块
    /// </summary>
    [Fact]
    public void ParseSelectStatement_EmptyCaseBlock_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
ch <- ChannelCreate()
select {
    case ch <- 100 -> {
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.IsType<SelectStatement>(result[1]);
    }

    /// <summary>
    /// 测试空的 default 块
    /// </summary>
    [Fact]
    public void ParseSelectStatement_EmptyDefaultBlock_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
ch <- ChannelCreate()
select {
    case ch <- 100 -> {
        PrintLine(""Sent"")
    }
    default -> {
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.IsType<SelectStatement>(result[1]);
    }

    #endregion

    #region 错误场景测试

    /// <summary>
    /// 测试缺少箭头符号 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseSelectStatement_MissingArrow_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
ch <- ChannelCreate()
select {
    case ch <- 100 {
        PrintLine(""Missing arrow"")
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少 case 块 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseSelectStatement_MissingCaseBlock_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
ch <- ChannelCreate()
select {
    case ch <- 100 ->
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 函数中的 Select 测试

    /// <summary>
    /// 测试函数中的 select 语句
    /// </summary>
    [Fact]
    public void ParseSelectStatement_InFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func testSelect() {
    ch <- ChannelCreate()
    select {
        case ch <- 100 -> {
            return ""Sent""
        }
        default -> {
            return ""Default""
        }
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count >= 0);
    }

    #endregion

    #region 带有多个操作的 case 测试

    /// <summary>
    /// 测试 case 块中包含多个语句
    /// </summary>
    [Fact]
    public void ParseSelectStatement_MultipleStatementsInCase_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
ch <- ChannelCreate()
result <- 0
select {
    case ch <- 100 -> {
        PrintLine(""First statement"")
        result <- 42
        PrintLine(""Second statement"")
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.IsType<SelectStatement>(result[2]);
    }

    #endregion

    #region Channel 数组和集合测试

    /// <summary>
    /// 测试使用数组元素作为 channel
    /// </summary>
    [Fact]
    public void ParseSelectStatement_ArrayElementChannel_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
channels <- [ChannelCreate(), ChannelCreate()]
select {
    case channels[0] <- 100 -> {
        PrintLine(""Sent to first channel"")
    }
    case channels[1] <- 200 -> {
        PrintLine(""Sent to second channel"")
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.IsType<SelectStatement>(result[1]);
    }

    #endregion

    #region 使用 try-catch 组合测试

    /// <summary>
    /// 测试 select 中包含 try-catch
    /// </summary>
    [Fact]
    public void ParseSelectStatement_WithTryCatch_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
ch <- ChannelCreate()
select {
    case ch <- 100 -> {
        try {
            DoSomething()
        } catch (e) {
            PrintLine(e)
        }
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.IsType<SelectStatement>(result[1]);
    }

    #endregion
}
