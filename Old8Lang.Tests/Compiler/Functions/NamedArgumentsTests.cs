using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Functions;

/// <summary>
/// 命名参数解释器执行测试
/// </summary>
[Collection("Sequential")]
public class NamedArgumentsTests
{
    #region 基础命名参数测试

    [Fact]
    public void NamedArguments_AllNamed_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func greet(name:string, age:int, message:string) -> string {
                return message + "", "" + name + ""! Age: "" + age.ToStr()
            }
            result <- greet(name: ""Bob"", age: 30, message: ""Hi"")
        ";
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

    [Fact]
    public void NamedArguments_MixedWithPositional_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func greet(name:string, age:int, message:string) -> string {
                return message + "", "" + name + ""! Age: "" + age.ToStr()
            }
            result <- greet(""Charlie"", age: 35, message: ""Good morning"")
        ";
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

    [Fact]
    public void NamedArguments_OutOfOrder_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func calculate(x:int, y:int, operation:string) -> int {
                if operation == ""add"" {
                    return x + y
                } elif operation == ""mul"" {
                    return x * y
                } else {
                    return x / y
                }
            }
            result <- calculate(operation: ""mul"", y: 3, x: 7)
        ";
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

    [Fact]
    public void NamedArguments_WithDefaultParameters_SkipsSomeArgs_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func display(title:string, width: 800, height: 600) -> string {
                return title + "": "" + width.ToStr() + ""x"" + height.ToStr()
            }
            result <- display(title: ""Window"", height: 1080)
        ";
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

    [Fact]
    public void NamedArguments_WithDefaultParameters_OverridesAll_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func display(title:string, width: 800, height: 600) -> string {
                return title + "": "" + width.ToStr() + ""x"" + height.ToStr()
            }
            result <- display(height: 720, width: 1280, title: ""HD Window"")
        ";
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

    #region 表达式作为命名参数值

    [Fact]
    public void NamedArguments_WithExpressionValues_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func compute(a:int, b:int, c:int) -> int {
                return a + b * c
            }
            x <- 10
            y <- 5
            result <- compute(a: x + 5, b: y * 2, c: 3)
        ";
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

    [Fact]
    public void NamedArguments_WithFunctionCallValues_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func tw(n:int) -> int {
                return n * 2
            }
            func add(x:int, y:int) -> int {
                return x + y
            }
            result <- add(x: tw(5), y: tw(10))
        ";
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

    #region 不同数据类型测试

    [Fact]
    public void NamedArguments_StringParameters_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func concat(first:string, second:string, sep:string) -> string {
                return first + sep + second
            }
            result <- concat(sep: "" - "", second: ""World"", first: ""Hello"")
        ";
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

    [Fact]
    public void NamedArguments_BooleanParameters_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func check(a:bool, b:bool, c:bool) -> string {
                if a && b && c {
                    return ""all true""
                } elif a || b || c {
                    return ""some true""
                } else {
                    return ""all false""
                }
            }
            result1 <- check(a: true, b: true, c: true)
            result2 <- check(c: false, a: true, b: false)
            result3 <- check(b: false, c: false, a: false)
        ";
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

    [Fact]
    public void NamedArguments_DoubleParameters_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func calculate(x:double, y:double, z:double) -> double {
                return x * y + z
            }
            result <- calculate(z: 1.5, x: 2.5, y: 3.0)
        ";
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

    #region Lambda 和高阶函数测试

    [Fact]
    public void NamedArguments_LambdaCall_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            multiply <- (x:int, y:int) -> x * y
            result <- multiply(y: 7, x: 6)
        ";
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

    [Fact]
    public void NamedArguments_HigherOrderFunction_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func applyOp(x:int, y:int, operation) -> int {
                return operation(x, y)
            }
            add <- (a:int, b:int) -> a + b
            result <- applyOp(operation: add, y: 20, x: 10)
        ";
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

    #region 方法调用测试

    [Fact]
    public void NamedArguments_MethodCall_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Calculator {
                func compute(x:int, y:int, op:string) -> int {
                    if op == ""add"" {
                        return x + y
                    } else {
                        return x * y
                    }
                }
            }
            calc <- Calculator()
            result <- calc.compute(op: ""add"", y: 15, x: 25)
        ";
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

    #region 递归函数测试

    [Fact]
    public void NamedArguments_RecursiveFunction_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func power(base:int, exp:int) -> int {
                if exp == 0 {
                    return 1
                }
                return base * power(base: base, exp: exp - 1)
            }
            result <- power(exp: 3, base: 2)
        ";
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

    #region 边界测试

    [Fact]
    public void NamedArguments_SingleParameter_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func tw(x:int) -> int {
                return x * 2
            }
            result <- tw(x: 21)
        ";
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

    [Fact]
    public void NamedArguments_ManyParameters_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func sum(a:int, b:int, c:int, d:int, e:int) -> int {
                return a + b + c + d + e
            }
            result <- sum(e: 5, d: 4, c: 3, b: 2, a: 1)
        ";
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

    [Fact]
    public void NamedArguments_ComplexNestedCalls_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func add(x:int, y:int) -> int {
                return x + y
            }
            func multiply(a:int, b:int) -> int {
                return a * b
            }
            func complex(p:int, q:int, r:int) -> int {
                return add(x: multiply(a: p, b: q), y: r)
            }
            result <- complex(r: 10, p: 2, q: 3)
        ";
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
