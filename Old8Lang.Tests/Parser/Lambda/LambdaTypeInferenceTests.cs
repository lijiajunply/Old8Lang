using Old8Lang.Error;

namespace Old8Lang.Tests.Parser.Lambda;

/// <summary>
/// Lambda表达式类型推断测试
/// </summary>
[Collection("Sequential")]
public class LambdaTypeInferenceTests
{
    #region Lambda类型推断正确语法

    /// <summary>
    /// 测试基本Lambda类型推断
    /// </summary>
    [Fact]
    public void ParseProgram_BasicLambdaTypeInference_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 推断为 int -> int
lambda1 <- (x) -> x * 2

// 推断为 int, int -> int
lambda2 <- (a, b) -> a + b

// 推断为 string -> string
lambda3 <- (s) -> s + "" world""

// 推断为 bool -> bool
lambda4 <- (b) -> not b

result1 <- lambda1(5)
result2 <- lambda2(3, 4)
result3 <- lambda3(""hello"")
result4 <- lambda4(true)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试复杂表达式的类型推断
    /// </summary>
    [Fact]
    public void ParseProgram_ComplexExpressionTypeInference_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 推断为 int, int -> double (因为除法产生浮点数)
lambda1 <- (a, b) -> a / b

// 推断为 int -> string (因为返回字符串)
lambda2 <- (x) -> ""Value: "" + x.ToStr()

// 推断为 double -> int (因为返回整数部分)
lambda3 <- (d) -> d.ToInt()

// 推断为 string, int -> string
lambda4 <- (s, n) -> s.Repeat(n)

result1 <- lambda1(10, 3)      // 3.333...
result2 <- lambda2(42)        // ""Value: 42""
result3 <- lambda3(3.14)      // 3
result4 <- lambda4(""abc"", 3) // ""abcabcabc""";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试条件表达式的类型推断
    /// </summary>
    [Fact]
    public void ParseProgram_ConditionalExpressionTypeInference_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 推断为 int -> int
lambda1 <- (x) -> x > 0 ? x : -x

// 推断为 int -> string
lambda2 <- (x) -> x % 2 == 0 ? ""even"" : ""odd""

// 推断为 int, int -> int
lambda3 <- (a, b) -> a > b ? a : b

result1 <- lambda1(-5)    // 5
result2 <- lambda2(4)     // ""even""
result3 <- lambda3(3, 7)  // 7";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试集合操作的类型推断
    /// </summary>
    [Fact]
    public void ParseProgram_CollectionOperationTypeInference_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 推断为 list[int] -> int (返回列表长度)
lambda1 <- (list) -> list.Count()

// 推断为 list[int] -> list[int] (返回处理后的列表)
lambda2 <- (numbers) -> {
    result <- {}
    for num in numbers {
        result.Push(num * 2)
    }
    return result
}

// 推断为 dict[string, int] -> int (返回字典值的总和)
lambda3 <- (dict) -> {
    sum <- 0
    for value in dict.Values() {
        sum <- sum + value
    }
    return sum
}

numbers <- {1, 2, 3, 4, 5}
scores <- {""math"": 90, ""english"": 85, ""science"": 95}

length <- lambda1(numbers)
doubled <- lambda2(numbers)
total <- lambda3(scores)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试函数返回Lambda的类型推断
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionReturningLambdaTypeInference_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 返回 int -> int 的Lambda
func makeAdder(offset) {
    return (x) -> x + offset
}

// 返回 int, int -> int 的Lambda
func makeBinaryOperation(op) {
    if op == ""add"" {
        return (a, b) -> a + b
    } else if op == ""multiply"" {
        return (a, b) -> a * b
    } else {
        return (a, b) -> a - b
    }
}

// 返回 string -> string 的Lambda
func makeFormatter(prefix) {
    return (text) -> prefix + "" "" + text
}

add10 <- makeAdder(10)
multiply <- makeBinaryOperation(""multiply"")
formatHello <- makeFormatter(""Hello"")

result1 <- add10(5)              // 15
result2 <- multiply(4, 3)         // 12
result3 <- formatHello(""World"") // ""Hello World""";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 编译器模式类型注解

    /// <summary>
    /// 测试Lambda参数类型注解
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaParameterTypeAnnotations_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 编译器模式下的Lambda，需要明确的类型注解
lambda1 <- (x:int) -> x * 2
lambda2 <- (a:int, b:int) -> a + b
lambda3 <- (s:string) -> s.ToUpper()
lambda4 <- (b:bool) -> not b

result1 <- lambda1(5)
result2 <- lambda2(3, 4)
result3 <- lambda3(""hello"")
result4 <- lambda4(true)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试Lambda返回类型注解
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaReturnTypeAnnotations_ParsesSuccessfully()
    {
        // Arrange
        var code = """
                   // 带返回类型注解的Lambda
                   lambda1 <- (x:int):int -> x * 2
                   lambda2 <- (a:int, b:int):double -> a / b
                   lambda3 <- (s:string):string -> s.ToUpper()
                   lambda4 <- (x:int):bool -> x % 2 == 0

                   result1 <- lambda1(5)      // 10
                   result2 <- lambda2(10, 3)  // 3.333...
                   result3 <- lambda3("hello") // "HELLO"
                   result4 <- lambda4(4)      // true
                   """;
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试混合类型注解的Lambda
    /// </summary>
    [Fact]
    public void ParseProgram_MixedTypeAnnotationsLambda_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 部分参数有类型注解，部分没有
lambda1 <- (x:int, y) -> x + y  // x明确为int，y推断
lambda2 <- (a, b:string) -> a.ToStr() + b  // b明确为string，a推断

// 复杂返回类型
lambda3 <- (numbers:list[int]) -> list[int]: {
    result <- {}
    for num in numbers {
        result.Push(num * 2)
    }
    return result
}

result1 <- lambda1(5, ""hello"")
result2 <- lambda2(42, "" world"")
result3 <- lambda3({1, 2, 3})";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 类型推断边界情况

    /// <summary>
    /// 测试空Lambda的类型推断
    /// </summary>
    [Fact]
    public void ParseProgram_EmptyLambdaTypeInference_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 空参数Lambda，推断返回类型
lambda1 <- () -> 42
lambda2 <- () -> ""hello""
lambda3 <- () -> true

result1 <- lambda1()
result2 <- lambda2()
result3 <- lambda3()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试多态Lambda的类型推断
    /// </summary>
    [Fact]
    public void ParseProgram_PolymorphicLambdaTypeInference_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 可以用于不同类型的通用Lambda
identity <- (x) -> x  // 推断类型取决于使用场景
first <- (a, b) -> a  // 返回第一个参数
second <- (a, b) -> b // 返回第二个参数

// 在不同上下文中使用
intResult1 <- identity(42)        // int类型
stringResult1 <- identity(""test"") // string类型
intResult2 <- first(1, 2)         // int类型
stringResult2 <- second(""a"", ""b"") // string类型";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试嵌套Lambda的类型推断
    /// </summary>
    [Fact]
    public void ParseProgram_NestedLambdaTypeInference_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 外层Lambda返回内层Lambda
makeAdder <- (offset) -> (x) -> x + offset
makeMultiplier <- (factor) -> (y) -> y * factor
makeComposer <- (f, g) -> (x) -> f(g(x))

add10 <- makeAdder(10)
multiply3 <- makeMultiplier(3)

// 使用内层Lambda
result1 <- add10(5)        // 15
result2 <- multiply3(7)    // 21";

        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试Lambda在集合中的类型推断
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaInCollectionTypeInference_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// Lambda集合，每个Lambda可能有不同的签名
operations <- [
    (x) -> x + 1,      // int -> int
    (s) -> s.ToUpper(), // string -> string
    (b) -> not b,      // bool -> bool
    (a, b) -> a + b    // int, int -> int
]

result1 <- operations[0](10)      // 11
result2 <- operations[1](""hello"") // ""HELLO""
result3 <- operations[2](true)    // false
result4 <- operations[3](5, 3)    // 8";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 类型推断错误场景

    /// <summary>
    /// 测试类型冲突的Lambda推断
    /// </summary>
    [Fact]
    public void ParseProgram_TypeConflictLambdaInference_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// Lambda中有不同类型的返回值，可能需要明确类型
lambda1 <- (x) -> x > 0 ? x : ""negative""
lambda2 <- (condition) -> condition ? 123 : ""false""

// 语法解析应该成功，但类型检查可能需要额外信息
result1 <- lambda1(5)
result2 <- lambda2(true)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试无法推断的Lambda类型
    /// </summary>
    [Fact]
    public void ParseProgram_UninferrableLambdaType_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 无法从表达式推断类型的Lambda
lambda1 <- (x) -> unknownFunction(x)
lambda2 <- () -> {
    // 复杂逻辑但返回类型不明确
    if someCondition {
        return 42
    } else {
        return ""test""
    }
}

// 语法解析应该成功，但可能需要类型注解或上下文
// result1 <- lambda1(5)
// result2 <- lambda2()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误的Lambda类型注解语法

    /// <summary>
    /// 测试错误的Lambda类型注解
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidLambdaTypeAnnotations_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
lambda1 <- (x:invalid_type) -> x  // 无效类型
lambda2 <- (x:int) -> invalid_type: x  // 无效返回类型
lambda3 <- (x:int::extra) -> x  // 错误的注解语法";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不完整的Lambda类型注解
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteLambdaTypeAnnotations_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
lambda1 <- (x: -> x  // 缺少类型
lambda2 <- (x:int) ->  // 缺少返回类型和函数体
lambda3 <- ( -> x  // 缺少参数和类型";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}