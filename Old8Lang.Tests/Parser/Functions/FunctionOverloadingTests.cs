using Old8Lang.Error;

namespace Old8Lang.Tests.Parser.Functions;

/// <summary>
/// 函数重载测试
/// </summary>
[Collection("Sequential")]
public class FunctionOverloadingTests
{
    #region 函数重载正确语法

    /// <summary>
    /// 测试参数数量不同的函数重载
    /// </summary>
    [Fact]
    public void ParseProgram_ParameterCountOverloading_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func add(a, b) -> int {
    return a + b
}

func add(a, b, c) -> int {
    return a + b + c
}

result1 <- add(1, 2)
result2 <- add(1, 2, 3)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试参数类型不同的函数重载
    /// </summary>
    [Fact]
    public void ParseProgram_ParameterTypeOverloading_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func add(a:int, b:int) -> int {
    return a + b
}

func add(a:string, b:string) -> string {
    return a + b
}

func add(a:double, b:double) -> double {
    return a + b
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试混合类型的函数重载
    /// </summary>
    [Fact]
    public void ParseProgram_MixedTypeOverloading_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func process(a) -> string {
    return a.ToStr()
}

func process(a:int) -> int {
    return a * 2
}

func process(a:string) -> string {
    return ""Hello: "" + a
}

func process(a, b) -> string {
    return a.ToStr() + "" + "" + b.ToStr()
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试可选参数的函数重载
    /// </summary>
    [Fact]
    public void ParseProgram_OptionalParameterOverloading_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func greet(name) -> string {
    return ""Hello, "" + name
}

func greet(name, message: ""Welcome"") -> string {
    return message + "", "" + name
}

func greet(name, message, title: ""Mr."") -> string {
    return title + "" "" + name + "" - "" + message
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试返回类型不同的函数重载
    /// </summary>
    [Fact]
    public void ParseProgram_ReturnTypeOverloading_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func calculate(a) -> int {
    return a * 2
}

func calculate(a) -> string {
    return ""Result: "" + (a * 2).ToStr()
}

func calculate(a) -> double {
    return a * 2.5
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试类方法重载
    /// </summary>
    [Fact]
    public void ParseProgram_ClassMethodOverloading_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Calculator {
    func add(a, b) -> int {
        return a + b
    }

    func add(a, b, c) -> int {
        return a + b + c
    }

    func add(a:double, b:double) -> double {
        return a + b
    }

    func add(a:string, b:string) -> string {
        return a + b
    }
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试静态方法重载
    /// </summary>
    [Fact]
    public void ParseProgram_StaticMethodOverloading_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class MathUtils {
    public static func max(a, b) -> int {
        if a > b {
            return a
        } else {
            return b
        }
    }

    public static func max(a, b, c) -> int {
        if a > b and a > c {
            return a
        } else if b > c {
            return b
        } else {
            return c
        }
    }

    public static func max(a:double, b:double) -> double {
        if a > b {
            return a
        } else {
            return b
        }
    }
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 复杂函数重载场景

    /// <summary>
    /// 测试嵌套函数重载
    /// </summary>
    [Fact]
    public void ParseProgram_NestedFunctionOverloading_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func outer(x) {
    func inner(a) -> int {
        return a + x
    }

    func inner(a, b) -> int {
        return a + b + x
    }

    return inner(5)
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试重载函数的递归调用
    /// </summary>
    [Fact]
    public void ParseProgram_RecursiveOverloadedFunctions_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func factorial(n) -> int {
    if n <= 1 {
        return 1
    } else {
        return n * factorial(n - 1)
    }
}

func factorial(n, acc) -> int {
    if n <= 1 {
        return acc
    } else {
        return factorial(n - 1, n * acc)
    }
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试重载函数的高阶函数应用
    /// </summary>
    [Fact]
    public void ParseProgram_HigherOrderWithOverloads_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func apply(func, a, b) {
    return func(a, b)
}

func add(x, y) -> int {
    return x + y
}

func add(x, y, z) -> int {
    return x + y + z
}

func multiply(x, y) -> int {
    return x * y
}

result <- apply(add, 5, 3)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 边界情况和错误处理

    /// <summary>
    /// 测试重载函数的歧义调用
    /// </summary>
    [Fact]
    public void ParseProgram_AmbiguousOverloadCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test(a) -> int {
    return 1
}

func test(b) -> string {
    return ""test""
}

result <- test(123)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        // 这个测试应该能解析，但可能在运行时有歧义
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试不完全匹配的重载调用
    /// </summary>
    [Fact]
    public void ParseProgram_PartialOverloadMatch_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func process(a, b) -> int {
    return a + b
}

func process(a, b, c) -> int {
    return a + b + c
}

func process(a, b, c, d) -> int {
    return a + b + c + d
}

result <- process(1, 2, 3, 4, 5)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误的重载语法

    /// <summary>
    /// 测试不完整的重载函数定义
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteOverloadDefinition_ThrowsSyntaxError()
    {
        // Arrange
        var code = """
                   func test(a, b) -> int {
                       return a + b
                   }

                   func test
                   """;
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}