using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Statement;

/// <summary>
/// Defer 语句解析测试
/// </summary>
[Collection("Sequential")]
public class DeferStatementTests
{
    #region 基本 Defer 测试

    /// <summary>
    /// 测试基本的 defer 语句
    /// </summary>
    [Fact]
    public void ParseDeferStatement_Basic_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() {
    defer PrintLine(""cleanup"")
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        var funcInit = result[0] as FuncInit;
        Assert.NotNull(funcInit);
        var blockStatement = funcInit!.FuncLangValue.BlockStatement;
        Assert.Equal(1, blockStatement.Count);
        Assert.IsType<DeferStatement>(blockStatement[0]);
    }

    /// <summary>
    /// 测试 defer 代码块
    /// </summary>
    [Fact]
    public void ParseDeferStatement_Block_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() {
    defer {
        PrintLine(""cleanup 1"")
        PrintLine(""cleanup 2"")
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        var funcInit = result[0] as FuncInit;
        Assert.NotNull(funcInit);
        var blockStatement = funcInit!.FuncLangValue.BlockStatement;
        Assert.Equal(1, blockStatement.Count);
        var deferStmt = blockStatement[0] as DeferStatement;
        Assert.NotNull(deferStmt);
        Assert.IsType<BlockStatement>(deferStmt!.Statement);
    }

    /// <summary>
    /// 测试多个 defer 语句
    /// </summary>
    [Fact]
    public void ParseDeferStatement_Multiple_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() {
    defer PrintLine(""1"")
    defer PrintLine(""2"")
    defer PrintLine(""3"")
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        var funcInit = result[0] as FuncInit;
        Assert.NotNull(funcInit);
        var blockStatement = funcInit!.FuncLangValue.BlockStatement;
        Assert.Equal(3, blockStatement.Count);
        Assert.All(blockStatement.OtherStatements, stmt => Assert.IsType<DeferStatement>(stmt));
    }

    #endregion

    #region Defer 与变量交互测试

    /// <summary>
    /// 测试 defer 访问局部变量
    /// </summary>
    [Fact]
    public void ParseDeferStatement_WithLocalVariable_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() {
    x <- 100
    defer PrintLine(x.ToStr())
    x <- 200
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        var funcInit = result[0] as FuncInit;
        Assert.NotNull(funcInit);
        var blockStatement = funcInit!.FuncLangValue.BlockStatement;
        Assert.Equal(3, blockStatement.Count);
        Assert.IsType<DeferStatement>(blockStatement[1]);
    }

    /// <summary>
    /// 测试 defer 中使用复杂表达式
    /// </summary>
    [Fact]
    public void ParseDeferStatement_WithComplexExpression_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() {
    x <- 10
    y <- 20
    defer PrintLine((x + y).ToStr())
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        var funcInit = result[0] as FuncInit;
        Assert.NotNull(funcInit);
        var blockStatement = funcInit!.FuncLangValue.BlockStatement;
        Assert.Equal(3, blockStatement.Count);
        Assert.IsType<DeferStatement>(blockStatement[2]);
    }

    #endregion

    #region Defer 与控制流交互测试

    /// <summary>
    /// 测试 defer 与 return 一起使用
    /// </summary>
    [Fact]
    public void ParseDeferStatement_WithReturn_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() -> int {
    defer PrintLine(""cleanup"")
    return 42
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        var funcInit = result[0] as FuncInit;
        Assert.NotNull(funcInit);
        var blockStatement = funcInit!.FuncLangValue.BlockStatement;
        Assert.Equal(2, blockStatement.Count);
        Assert.IsType<DeferStatement>(blockStatement[0]);
        Assert.IsType<ReturnStatement>(blockStatement[1]);
    }

    /// <summary>
    /// 测试 defer 在 if 语句中
    /// </summary>
    [Fact]
    public void ParseDeferStatement_InIfStatement_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() {
    if true {
        defer PrintLine(""in if"")
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    /// <summary>
    /// 测试 defer 在循环中
    /// </summary>
    [Fact]
    public void ParseDeferStatement_InLoop_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() {
    for i <- 0, i < 5, i++ {
        defer PrintLine(i.ToStr())
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    #endregion

    #region Defer 与函数调用测试

    /// <summary>
    /// 测试 defer 调用函数
    /// </summary>
    [Fact]
    public void ParseDeferStatement_FunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func cleanup(msg) {
    PrintLine(msg)
}

func test() {
    defer cleanup(""done"")
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        var funcInit = result[1] as FuncInit;
        Assert.NotNull(funcInit);
        var blockStatement = funcInit!.FuncLangValue.BlockStatement;
        Assert.Equal(1, blockStatement.Count);
        Assert.IsType<DeferStatement>(blockStatement[0]);
    }

    /// <summary>
    /// 测试 defer 调用带参数的函数
    /// </summary>
    [Fact]
    public void ParseDeferStatement_FunctionCallWithMultipleArguments_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() {
    x <- 10
    y <- 20
    defer DoSomething(x, y, ""result"")
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    #endregion

    #region Defer 与资源管理测试

    /// <summary>
    /// 测试 defer 用于资源清理（互斥锁）
    /// </summary>
    [Fact]
    public void ParseDeferStatement_MutexCleanup_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() {
    mutex <- MutexCreate()
    defer MutexDispose(mutex)
    MutexLock(mutex)
    // critical section
    MutexUnlock(mutex)
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    /// <summary>
    /// 测试 defer 用于资源清理（Channel）
    /// </summary>
    [Fact]
    public void ParseDeferStatement_ChannelCleanup_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() {
    ch <- ChannelCreate()
    defer ChannelDispose(ch)
    ChannelSend(ch, 123)
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    #endregion

    #region 嵌套和复杂场景测试

    /// <summary>
    /// 测试嵌套函数中的 defer
    /// </summary>
    [Fact]
    public void ParseDeferStatement_NestedFunctions_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func outer() {
    defer PrintLine(""outer defer"")

    inner <- () -> {
        defer PrintLine(""inner defer"")
        PrintLine(""inner body"")
    }

    inner()
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    /// <summary>
    /// 测试 defer 在 try-catch 中
    /// </summary>
    [Fact]
    public void ParseDeferStatement_InTryCatch_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() {
    try {
        defer PrintLine(""cleanup in try"")
        DoSomething()
    } catch (e) {
        PrintLine(e)
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    /// <summary>
    /// 测试 defer 代码块包含多种语句
    /// </summary>
    [Fact]
    public void ParseDeferStatement_ComplexBlock_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() {
    x <- 10
    defer {
        if x > 5 {
            PrintLine(""x is large"")
        } else {
            PrintLine(""x is small"")
        }
        PrintLine(""cleanup done"")
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    #endregion

    #region 错误场景测试

    /// <summary>
    /// 测试 defer 后缺少语句 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseDeferStatement_MissingStatement_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
func test() {
    defer
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试空的 defer 代码块
    /// </summary>
    [Fact]
    public void ParseDeferStatement_EmptyBlock_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() {
    defer {
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    /// <summary>
    /// 测试在函数外使用 defer - 解析应该成功（运行时会报错）
    /// </summary>
    [Fact]
    public void ParseDeferStatement_OutsideFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
defer PrintLine(""outside"")
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<DeferStatement>(result[0]);
    }

    #endregion

    #region 并发原语结合测试

    /// <summary>
    /// 测试 defer 与 using 语句结合（两种资源管理方式）
    /// </summary>
    [Fact]
    public void ParseDeferStatement_WithUsing_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() {
    defer PrintLine(""function cleanup"")

    using mutex <- MutexCreate() {
        MutexLock(mutex)
        defer PrintLine(""mutex cleanup"")
        MutexUnlock(mutex)
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    /// <summary>
    /// 测试 defer 清理多个资源
    /// </summary>
    [Fact]
    public void ParseDeferStatement_MultipleResources_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() {
    mutex <- MutexCreate()
    defer MutexDispose(mutex)

    ch <- ChannelCreate()
    defer ChannelDispose(ch)

    sem <- SemaphoreCreate(1, 5)
    defer SemaphoreDispose(sem)
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    #endregion
}
