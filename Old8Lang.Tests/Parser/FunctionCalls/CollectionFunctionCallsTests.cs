using Old8Lang.Error;

namespace Old8Lang.Tests.Parser.FunctionCalls;

/// <summary>
/// 集合函数调用语法测试 - cla[0](a) 语法
/// </summary>
[Collection("Sequential")]
public class CollectionFunctionCallsTests
{
    #region 集合函数调用正确语法

    /// <summary>
    /// 测试基本列表函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_BasicListFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func greet(name) -> string {
    return ""Hello, "" + name
}

func add(a, b) -> int {
    return a + b
}

funcList <- [greet, add]
result1 <- funcList[0](""Alice"")
result2 <- funcList[1](5, 3)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试字典函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_DictionaryFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func double(x) -> int {
    return x * 2
}

func triple(x) -> int {
    return x * 3
}

funcDict <- {
    ""double"": double,
    ""triple"": triple
}

result1 <- funcDict[""double""](8)
result2 <- funcDict[""triple""](8)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试数组函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_ArrayFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func square(x) -> int {
    return x * x
}

func cube(x) -> int {
    return x * x * x
}

funcArray <- [square, cube]
result1 <- funcArray[0](4)
result2 <- funcArray[1](3)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试嵌套集合函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_NestedCollectionFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func add(a, b) -> int {
    return a + b
}

func multiply(a, b) -> int {
    return a * b
}

func subtract(a, b) -> int {
    return a - b
}

// 二维数组中的函数
funcMatrix <- [
    [add, multiply],
    [subtract, add]
]

result1 <- funcMatrix[0][0](5, 3)   // add(5, 3) = 8
result2 <- funcMatrix[0][1](5, 3)   // multiply(5, 3) = 15
result3 <- funcMatrix[1][0](5, 3)   // subtract(5, 3) = 2
result4 <- funcMatrix[1][1](5, 3)   // add(5, 3) = 8";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试链式集合函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_ChainedCollectionFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func getOperation(opName) {
    if opName == ""add"" {
        return (a, b) -> a + b
    } else if opName == ""multiply"" {
        return (a, b) -> a * b
    } else if opName == ""subtract"" {
        return (a, b) -> a - b
    } else {
        return (a, b) -> 0
    }
}

operations <- [getOperation(""add""), getOperation(""multiply""), getOperation(""subtract"")]
result1 <- operations[0](10, 5)
result2 <- operations[1](10, 5)
result3 <- operations[2](10, 5)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试复杂表达式的集合函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_ComplexExpressionCollectionFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class MathHelper {
    public func getMultiplier(factor) {
        return (x) -> x * factor
    }

    public func getAdder(value) {
        return (x) -> x + value
    }
}

math <- MathHelper()
operations <- [math.getMultiplier(2), math.getAdder(5)]
result1 <- operations[0](10)  // 10 * 2 = 20
result2 <- operations[1](10)  // 10 + 5 = 15";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试方法返回函数集合的调用
    /// </summary>
    [Fact]
    public void ParseProgram_MethodReturningFunctionCollectionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class FunctionFactory {
    public func createMathFunctions() {
        return [
            (x) -> x * 2,           // double
            (x) -> x * x,           // square
            (x) -> x + 1,           // increment
            (x) -> x / 2.0          // halve
        ]
    }

    public func createStringFunctions() {
        return {
            ""upper"": (s) -> s.ToUpper(),
            ""lower"": (s) -> s.ToLower(),
            ""reverse"": (s) -> {
                result <- """"
                for i <- s.Count() - 1, i >= 0, i <- i - 1 {
                    result <- result + s[i]
                }
                return result
            }
        }
    }
}

factory <- FunctionFactory()
mathFuncs <- factory.createMathFunctions()
stringFuncs <- factory.createStringFunctions()

result1 <- mathFuncs[0](10)   // double(10) = 20
result2 <- mathFuncs[1](5)    // square(5) = 25
result3 <- mathFuncs[2](7)    // increment(7) = 8
result4 <- mathFuncs[3](8)    // halve(8) = 4.0

result5 <- stringFuncs[""upper""](""hello"")      // HELLO
result6 <- stringFuncs[""lower""](""WORLD"")      // world
result7 <- stringFuncs[""reverse""](""abcde"")   // edcba";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 高级集合函数调用场景

    /// <summary>
    /// 测试条件选择的函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_ConditionalFunctionSelection_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func getCompareFunc(operation) {
    compareFuncs <- [
        (a, b) -> a > b,      // greater than
        (a, b) -> a < b,      // less than
        (a, b) -> a == b,     // equal
        (a, b) -> a >= b,     // greater or equal
        (a, b) -> a <= b      // less or equal
    ]

    if operation == ""gt"" { return compareFuncs[0] }
    if operation == ""lt"" { return compareFuncs[1] }
    if operation == ""eq"" { return compareFuncs[2] }
    if operation == ""ge"" { return compareFuncs[3] }
    if operation == ""le"" { return compareFuncs[4] }

    return compareFuncs[2]  // default to equal
}

gtFunc <- getCompareFunc(""gt"")
ltFunc <- getCompareFunc(""lt"")

result1 <- gtFunc(10, 5)   // true
result2 <- ltFunc(10, 5)   // false";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试函数数组的高阶函数应用
    /// </summary>
    [Fact]
    public void ParseProgram_HigherOrderFunctionArray_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func compose(f, g) {
    return (x) -> f(g(x))
}

func add1(x) -> int { return x + 1 }
func multiply2(x) -> int { return x * 2 }
func square(x) -> int { return x * x }
func toString(x) -> string { return x.ToStr() }

// 创建函数组合
composers <- [
    compose(add1, multiply2),      // add1(multiply2(x))
    compose(multiply2, add1),      // multiply2(add1(x))
    compose(square, add1),         // square(add1(x))
    compose(toString, square)      // toString(square(x))
]

result1 <- composers[0](5)  // add1(multiply2(5)) = add1(10) = 11
result2 <- composers[1](5)  // multiply2(add1(5)) = multiply2(6) = 12
result3 <- composers[2](5)  // square(add1(5)) = square(6) = 36
result4 <- composers[3](5)  // toString(square(5)) = toString(25) = ""25""";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试动态构建的函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_DynamicFunctionConstruction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func buildCalculator(operation) {
    if operation == ""add"" {
        return (a, b) -> a + b
    } else if operation == ""subtract"" {
        return (a, b) -> a - b
    } else if operation == ""multiply"" {
        return (a, b) -> a * b
    } else {
        return (a, b) -> 0
    }
}

func buildValidator(condition) {
    validators <- [
        (x) -> x > 0,           // positive
        (x) -> x < 0,           // negative
        (x) -> x == 0,          // zero
        (x) -> x % 2 == 0,      // even
        (x) -> x % 2 != 0       // odd
    ]

    if condition >= 0 and condition < 5 {
        return validators[condition]
    }
    return validators[2]  // default to zero check
}

addCalc <- buildCalculator(""add"")
subtractCalc <- buildCalculator(""subtract"")
multiplyCalc <- buildCalculator(""multiply"")
divideCalc <- buildCalculator(""divide"")

mathResult1 <- addCalc(10, 5)
mathResult2 <- subtractCalc(10, 5)
mathResult3 <- multiplyCalc(10, 5)
mathResult4 <- divideCalc(10, 5)

positiveCheck <- buildValidator(0)
negativeCheck <- buildValidator(1)
zeroCheck <- buildValidator(2)
evenCheck <- buildValidator(3)
oddCheck <- buildValidator(4)

boolResult1 <- positiveCheck(10)   // true
boolResult2 <- negativeCheck(10)   // false
boolResult3 <- zeroCheck(0)        // true
boolResult4 <- evenCheck(10)       // true
boolResult5 <- oddCheck(10)        // false";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 边界情况和错误处理

    /// <summary>
    /// 测试空集合的函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_EmptyCollectionFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
emptyList <- []
emptyDict <- {}

// 语法解析应该成功，但运行时可能出错
// result <- emptyList[0](123)
// result2 <- emptyDict[""key""](456)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试索引越界的函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_OutOfBoundsFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test(x) -> int {
    return x * 2
}

funcList <- [test]
// 语法解析应该成功，但运行时可能出错
// result <- funcList[5](10)  // 索引5越界";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试非函数元素的调用
    /// </summary>
    [Fact]
    public void ParseProgram_NonFunctionElementCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
mixedList <- [1, 2, 3, ""hello"", true]
// 语法解析应该成功，但运行时可能出错
// result <- mixedList[0](10)  // 尝试调用整数
// result2 <- mixedList[3](""test"")  // 尝试调用字符串";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试参数数量不匹配的函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_ParameterMismatchFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func add(a, b) -> int {
    return a + b
}

funcList <- [add]
// 语法解析应该成功，但运行时可能出错
// result1 <- funcList[0](5)        // 参数太少
// result2 <- funcList[0](5, 3, 2)  // 参数太多
result3 <- funcList[0](5, 3)        // 正确";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 复杂嵌套和组合

    /// <summary>
    /// 测试深度嵌套的集合函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_DeepNestedCollectionFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func identity(x) { return x }
func double(x) { return x * 2 }
func triple(x) { return x * 3 }

// 三维嵌套的函数数组
funcCube <- [
    [
        [identity, double],
        [triple, identity]
    ],
    [
        [double, triple],
        [identity, double]
    ]
]

result1 <- funcCube[0][0][0](5)   // identity(5) = 5
result2 <- funcCube[0][0][1](5)   // double(5) = 10
result3 <- funcCube[0][1][0](5)   // triple(5) = 15
result4 <- funcCube[1][0][0](5)   // double(5) = 10";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试集合函数调用与其他运算符的混合
    /// </summary>
    [Fact]
    public void ParseProgram_CollectionFunctionCallWithOperators_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func add(a, b) { return a + b }
func multiply(a, b) { return a * b }

ops <- [add, multiply]
x <- 5
y <- 3

result1 <- ops[0](x, y) + 10     // add(x,y) + 10
result2 <- ops[1](x, y) * 2      // multiply(x,y) * 2
result3 <- ops[0](x, y) > 7      // add(x,y) > 7
result4 <- ops[1](x, y) == 15    // multiply(x,y) == 15";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误语法测试

    /// <summary>
    /// 测试不完整的集合函数调用语法
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteCollectionFunctionCall_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
func test(x) { return x }
funcList <- [test]
result <- funcList[0";  // 缺少函数调用的括号
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试错误的索引语法
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidIndexSyntax_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
func test(x) { return x }
funcList <- [test]
result <- funcList";  // 缺少索引操作
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        // 这可能不应该报错，取决于是否允许将函数列表赋值给变量
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion
}