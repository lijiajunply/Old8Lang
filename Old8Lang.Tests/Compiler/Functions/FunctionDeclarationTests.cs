using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Functions;

/// <summary>
/// 函数声明编译模式测试
/// 测试编译器模式下函数声明的 IL 生成和执行
/// 注意:编译模式要求函数参数必须有类型注解或默认值,返回类型必须显式声明
/// </summary>
[Collection("Sequential")]
public class FunctionDeclarationTests
{
    #region 基础函数声明测试

    [Fact]
    public void FunctionDeclaration_NoParameters_CompilesCorrectly()
    {
        // Arrange - 编译模式要求返回类型注解
        var code = @"
            func sayHello() -> string {
                return ""Hello, World!""
            }
            result <- sayHello()
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
    public void FunctionDeclaration_WithTypeAnnotations_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }
            result <- add(5, 3)
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
    public void FunctionDeclaration_WithMixedTypeParameters_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func createMessage(name:string, age:int) -> string {
                return name + "" is "" + age + "" years old""
            }
            result <- createMessage(""Alice"", 25)
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
    public void FunctionDeclaration_MultiplicationFunction_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func multiply(x:int, y:int) -> int {
                return x * y
            }
            result <- multiply(4, 6)
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

    #region 函数返回类型测试

    [Fact]
    public void FunctionDeclaration_VoidReturnType_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            counter <- 0
            func increment() -> void {
                counter <- counter + 1
            }
            increment()
            increment()
            result <- counter
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
    public void FunctionDeclaration_IntReturnType_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func calculate() -> int {
                a <- 10
                b <- 20
                return a + b
            }
            result <- calculate()
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
    public void FunctionDeclaration_DoubleReturnType_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func calculateAverage(a:int, b:int) -> double {
                return (a + b) / 2.0
            }
            result <- calculateAverage(10, 20)
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
    public void FunctionDeclaration_StringReturnType_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func formatName(firstName:string, lastName:string) -> string {
                return lastName + "", "" + firstName
            }
            result <- formatName(""John"", ""Doe"")
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
    public void FunctionDeclaration_BoolReturnType_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func isPositive(n:int) -> bool {
                return n > 0
            }
            result1 <- isPositive(10)
            result2 <- isPositive(-5)
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

    #region Lambda 语法测试

    [Fact]
    public void FunctionDeclaration_LambdaSyntax_CompilesCorrectly()
    {
        // Arrange - Lambda 语法也需要参数类型注解
        var code = @"
            subtract <- (a:int, b:int) -> a - b
            result <- subtract(10, 3)
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
    public void FunctionDeclaration_LambdaWithMultipleParameters_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            combine <- (a:string, b:string, c:string) -> a + "" "" + b + "" "" + c
            result <- combine(""Hello"", ""from"", ""Old8Lang"")
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

    #region 复杂函数体测试

    [Fact]
    public void FunctionDeclaration_WithLocalVariables_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func complexCalculation(x:int, y:int) -> int {
                temp1 <- x * 2
                temp2 <- y * 3
                result <- temp1 + temp2
                return result
            }
            finalResult <- complexCalculation(5, 4)
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
    public void FunctionDeclaration_WithConditionalReturn_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func absoluteValue(n:int) -> int {
                if n < 0 {
                    return -n
                } else {
                    return n
                }
            }
            result1 <- absoluteValue(-15)
            result2 <- absoluteValue(25)
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
    public void FunctionDeclaration_WithLoop_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func factorial(n:int) -> int {
                result <- 1
                i <- 1
                while i <= n {
                    result <- result * i
                    i <- i + 1
                }
                return result
            }
            result <- factorial(5)
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

    #region 递归函数测试

    [Fact]
    public void FunctionDeclaration_RecursiveFunction_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func fibonacci(n:int) -> int {
                if n <= 1 {
                    return n
                }
                return fibonacci(n - 1) + fibonacci(n - 2)
            }
            result <- fibonacci(7)
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

    #region 多个函数声明测试

    [Fact]
    public void MultipleFunctionDeclarations_CompileCorrectly()
    {
        // Arrange
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }
            func subtract(a:int, b:int) -> int {
                return a - b
            }
            func multiply(a:int, b:int) -> int {
                return a * b
            }
            result1 <- add(10, 5)
            result2 <- subtract(10, 5)
            result3 <- multiply(10, 5)
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
    public void FunctionDeclaration_CallingOtherFunction_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func double(n:int) -> int {
                return n * 2
            }
            func quadruple(n:int) -> int {
                return double(double(n))
            }
            result <- quadruple(5)
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
}
