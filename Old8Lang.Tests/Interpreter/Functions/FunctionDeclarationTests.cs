using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Functions;

/// <summary>
/// 函数声明解释模式测试
/// </summary>
public class FunctionDeclarationTests
{
    [Fact]
    public void FunctionDeclaration_NoParameters_DeclaresCorrectly()
    {
        // Arrange
        var code = @"
            func sayHello() {
                return ""Hello, World!""
            }
            result <- sayHello()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello, World!", ((StringLangValue)result).Value);
    }

    [Fact]
    public void FunctionDeclaration_WithParameters_DeclaresCorrectly()
    {
        // Arrange
        var code = @"
            func add(a, b) {
                return a + b
            }
            result <- add(5, 3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(8, ((IntLangValue)result).Value);
    }

    [Fact]
    public void FunctionDeclaration_WithTypeAnnotations_DeclaresCorrectly()
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(24, ((IntLangValue)result).Value);
    }

    [Fact]
    public void FunctionDeclaration_WithMixedTypeParameters_DeclaresCorrectly()
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Alice is 25 years old", ((StringLangValue)result).Value);
    }

    [Fact]
    public void FunctionDeclaration_AlternativeSyntax_DeclaresCorrectly()
    {
        // Arrange
        var code = @"
            subtract <- (a, b) -> a - b
            result <- subtract(10, 3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(7, ((IntLangValue)result).Value);
    }

    [Fact]
    public void FunctionDeclaration_NoReturnType_InfersReturnType()
    {
        // Arrange
        var code = @"
            func calculate() {
                a <- 10
                b <- 20
                return a + b
            }
            result <- calculate()
        ";
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

    [Fact]
    public void FunctionDeclaration_WithDefaultParameters_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            func greet(name, greeting: ""Hello"") -> string {
                return greeting + "", "" + name
            }
            result1 <- greet(""Alice"")
            result2 <- greet(""Bob"", ""Hi"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as StringLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as StringLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("Hello, Alice", result1.Value);
        Assert.Equal("Hi, Bob", result2.Value);
    }

    [Fact]
    public void FunctionDeclaration_VoidReturnType_HandlesCorrectly()
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(2, ((IntLangValue)result).Value);
    }

    [Fact]
    public void FunctionDeclaration_MultipleFunctions_DeclaresAllCorrectly()
    {
        // Arrange
        var code = @"
            func add(a, b) {
                return a + b
            }
            func subtract(a, b) {
                return a - b
            }
            func multiply(a, b) {
                return a * b
            }
            result1 <- add(10, 5)
            result2 <- subtract(10, 5)
            result3 <- multiply(10, 5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as IntLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as IntLangValue;
        var result3 = interpreter.Manager.GetValue(new LangId("result3")) as IntLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.Equal(15, result1.Value);
        Assert.Equal(5, result2.Value);
        Assert.Equal(50, result3.Value);
    }

    [Fact]
    public void FunctionDeclaration_ComplexBody_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            func complexCalculation(x, y) {
                temp1 <- x * 2
                temp2 <- y + 10
                if temp1 > temp2 {
                    return temp1 - temp2
                } else {
                    return temp2 - temp1
                }
            }
            result1 <- complexCalculation(5, 20)
            result2 <- complexCalculation(15, 10)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as IntLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as IntLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(20, result1.Value); // complexCalculation(5, 20): 10 > 30? false, return 30 - 10 = 20
        Assert.Equal(10, result2.Value); // complexCalculation(15, 10): 30 > 20? true, return 30 - 20 = 10
    }

    [Fact]
    public void FunctionDeclaration_WithLocalVariables_ScopeIsCorrect()
    {
        // Arrange
        var code = @"
            x <- 10
            func testScope() {
                x <- 20  // 局部变量
                y <- 30
                return x + y
            }
            result1 <- testScope()
            result2 <- x  // 应该还是全局的 x
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as IntLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as IntLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(50, result1.Value); // 20 + 30 = 50
        Assert.Equal(20, result2.Value); // 全局 x 没有被改变
    }

    [Fact]
    public void FunctionDeclaration_RecursiveFunction_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            func factorial(n) {
                if n <= 1 {
                    return 1
                } else {
                    return n * factorial(n - 1)
                }
            }
            result1 <- factorial(5)
            result2 <- factorial(0)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as IntLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as IntLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(120, result1.Value); // 5! = 120
        Assert.Equal(1, result2.Value);    // 0! = 1
    }

    [Fact]
    public void FunctionDeclaration_WithArrayParameters_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            func sumArray(arr) {
                sum <- 0
                for i <- 0, i < 5, i++ {
                    sum <- sum + arr[i]
                }
                return sum
            }
            testArray <- [1, 2, 3, 4, 5]
            result <- sumArray(testArray)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value); // 1 + 2 + 3 + 4 + 5 = 15
    }

    [Fact]
    public void FunctionDeclaration_WithFunctionParameter_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            func applyOperation(a, b, operation) {
                return operation(a, b)
            }
            func add(x, y) {
                return x + y
            }
            func multiply(x, y) {
                return x * y
            }
            result1 <- applyOperation(5, 3, add)
            result2 <- applyOperation(5, 3, multiply)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as IntLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as IntLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(8, result1.Value);   // add(5, 3) = 8
        Assert.Equal(15, result2.Value);  // multiply(5, 3) = 15
    }

    [Fact]
    public void FunctionDeclaration_WithEarlyReturn_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            func checkValue(x) {
                if x > 0 {
                    return ""positive""
                }
                if x < 0 {
                    return ""negative""
                }
                return ""zero""
            }
            result1 <- checkValue(10)
            result2 <- checkValue(-5)
            result3 <- checkValue(0)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as StringLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as StringLangValue;
        var result3 = interpreter.Manager.GetValue(new LangId("result3")) as StringLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.Equal("positive", result1.Value);
        Assert.Equal("negative", result2.Value);
        Assert.Equal("zero", result3.Value);
    }

    [Fact]
    public void FunctionDeclaration_WithNestedFunction_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            func outerFunction(x) {
                func innerFunction(y) {
                    return y * 2
                }
                return innerFunction(x) + 10
            }
            result <- outerFunction(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(20, ((IntLangValue)result).Value); // innerFunction(5) = 10, 10 + 10 = 20
    }

    [Fact]
    public void FunctionDeclaration_EmptyFunction_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            func emptyFunction() -> void {
            }
            counter <- 0
            emptyFunction()
            result <- counter
        ";
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

    [Fact]
    public void FunctionDeclaration_WithAccessModifiers_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            public func publicFunction() {
                return ""public""
            }
            private func privateFunction() {
                return ""private""
            }
            static func staticFunction() {
                return ""static""
            }
            result1 <- publicFunction()
            result2 <- privateFunction()
            result3 <- staticFunction()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as StringLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as StringLangValue;
        var result3 = interpreter.Manager.GetValue(new LangId("result3")) as StringLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.Equal("public", result1.Value);
        Assert.Equal("private", result2.Value);
        Assert.Equal("static", result3.Value);
    }

    [Fact]
    public void FunctionDeclaration_WithUnicodeName_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            func 计算总和(a, b) {
                return a + b
            }
            结果 <- 计算总和(100, 200)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("结果"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(300, ((IntLangValue)result).Value);
    }
}