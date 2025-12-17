using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Lambda;

/// <summary>
/// Lambda闭包行为测试
/// </summary>
[Collection("Sequential")]
public class ClosureBehaviorTests
{
    #region 闭包基本行为

    /// <summary>
    /// 测试基本闭包创建和使用
    /// </summary>
    [Fact]
    public void ParseProgram_BasicClosureCreation_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
x <- 10
closure <- (y) -> x + y
result <- closure(5)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试闭包保持外部变量状态
    /// </summary>
    [Fact]
    public void ParseProgram_ClosureVariableState_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
counter <- 0
makeIncrementer <- (step) -> {
    return () -> {
        counter <- counter + step
        return counter
    }
}

incBy2 <- makeIncrementer(2)
value1 <- incBy2()
value2 <- incBy2()";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试多个闭包共享相同变量
    /// </summary>
    [Fact]
    public void ParseProgram_MultipleClosuresSharedVariable_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
shared <- 10
closure1 <- (x) -> x + shared
closure2 <- (y) -> y * shared

result1 <- closure1(5)
result2 <- closure2(3)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试闭包的变量隔离
    /// </summary>
    [Fact]
    public void ParseProgram_ClosureVariableIsolation_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func makeAccumulator(initial) {
    sum <- initial
    return (value) -> {
        sum <- sum + value
        return sum
    }
}

acc1 <- makeAccumulator(10)
acc2 <- makeAccumulator(100)

result1 <- acc1(5)
result2 <- acc2(5)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 闭包边界情况

    /// <summary>
    /// 测试空闭包
    /// </summary>
    [Fact]
    public void ParseProgram_EmptyClosure_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
emptyClosure1 <- () -> 42
emptyClosure2 <- () -> ""hello""

value1 <- emptyClosure1()
value2 <- emptyClosure2()";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试捕获大量变量的闭包
    /// </summary>
    [Fact]
    public void ParseProgram_ClosureWithManyVariables_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
a <- 1
b <- 2
c <- 3

complexClosure <- (x, y) -> a + b + c + x + y
result <- complexClosure(10, 20)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误的闭包语法

    /// <summary>
    /// 测试未定义变量的闭包引用
    /// </summary>
    [Fact]
    public void ParseProgram_UndefinedVariableInClosure_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
closure <- (x) -> undefinedVar + x";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试不完整的闭包定义
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteClosureDefinition_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
closure1 <- (x) -> ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}