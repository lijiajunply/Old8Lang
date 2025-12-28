using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Basic;

/// <summary>
/// 变量相关编译模式测试
/// 测试编译器模式下的变量声明、赋值、作用域等功能
/// </summary>
[Collection("Sequential")]
public class VariableTests
{
    #region 变量声明和赋值

    [Fact]
    public void VariableDeclaration_SimpleInteger_CompilesAndExecutes()
    {
        // Arrange
        var code = "x <- 100";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void VariableDeclaration_MultipleVariables_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            c <- 30
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void VariableDeclaration_AllBasicTypes_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            intVar <- 42
            doubleVar <- 3.14
            strVar <- ""hello""
            boolVar <- true
            charVar <- 'x'
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 变量重新赋值

    [Fact]
    public void VariableReassignment_SameType_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            x <- 10
            x <- 20
            x <- 30
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void VariableReassignment_DifferentTypes_CompilesAndExecutes()
    {
        // Arrange - Old8Lang支持动态类型
        var code = @"
            x <- 10
            x <- ""hello""
            x <- true
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void VariableReassignment_WithExpression_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            x <- 10
            x <- x + 5
            x <- x * 2
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 变量作用域测试

    [Fact]
    public void VariableScope_FunctionScope_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            x <- 10

            func test() -> void {
                y <- 20
            }

            test()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void VariableScope_BlockScope_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            x <- 10

            if true {
                y <- 20
                z <- x + y
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void VariableScope_NestedBlocks_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            x <- 1

            if true {
                y <- 2

                if true {
                    z <- 3
                    result <- x + y + z
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void VariableScope_LoopScope_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            sum <- 0

            for i <- 0, i < 5, i <- i + 1 {
                temp <- i * 2
                sum <- sum + temp
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 类型注解测试

    [Fact]
    public void VariableDeclaration_WithTypeAnnotation_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            x:int <- 42
            y:double <- 3.14
            z:string <- ""hello""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void VariableDeclaration_CollectionTypeAnnotation_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            arr:int[] <- [1, 2, 3]
            list:int{} <- {1, 2, 3}
            dict:dict <- {""a"": 1, ""b"": 2}
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region Unicode和特殊字符测试

    [Fact]
    public void VariableDeclaration_UnicodeIdentifier_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            变量 <- 100
            数字 <- 变量 + 50
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void VariableDeclaration_MixedLanguageIdentifiers_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            myVar <- 10
            我的变量 <- 20
            result <- myVar + 我的变量
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 复杂表达式赋值

    [Fact]
    public void VariableAssignment_ArithmeticExpression_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            c <- a + b * 2 - 5
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void VariableAssignment_LogicalExpression_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            result <- a < b && b > 0
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void VariableAssignment_TernaryExpression_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            x <- 10
            result <- x > 5 ? ""big"" : ""small""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public void VariableDeclaration_LongIdentifier_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            thisIsAVeryLongVariableNameThatTestsTheLimitsOfIdentifierLength <- 42
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void VariableDeclaration_MaxIntValue_CompilesAndExecutes()
    {
        // Arrange
        var code = $"x <- {int.MaxValue}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void VariableDeclaration_MinIntValue_CompilesAndExecutes()
    {
        // Arrange
        var code = $"x <- {int.MinValue}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion
}
