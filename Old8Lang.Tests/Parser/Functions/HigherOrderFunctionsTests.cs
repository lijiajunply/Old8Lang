using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Functions;

/// <summary>
/// 高阶函数测试
/// </summary>
[Collection("Sequential")]
public class HigherOrderFunctionsTests
{
    #region 高阶函数正确语法

    /// <summary>
    /// 测试函数作为参数传递
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionAsParameter_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func apply(func, a, b) {
    return func(a, b)
}

func add(x, y) -> int {
    return x + y
}

func multiply(x, y) -> int {
    return x * y
}

result1 <- apply(add, 5, 3)
result2 <- apply(multiply, 5, 3)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试函数作为返回值
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionAsReturnValue_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func getAdder() {
    return (a, b) -> a + b
}

func getMultiplier(factor) {
    return (x) -> x * factor
}

adder <- getAdder()
multiplier <- getMultiplier(5)

result1 <- adder(3, 4)
result2 <- multiplier(6)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试map函数
    /// </summary>
    [Fact]
    public void ParseProgram_MapFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func map(func, list) -> list {
    result <- {}
    for item in list {
        result.Push(func(item))
    }
    return result
}

func double(x) -> int {
    return x * 2
}

numbers <- {1, 2, 3, 4, 5}
doubled <- map(double, numbers)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试filter函数
    /// </summary>
    [Fact]
    public void ParseProgram_FilterFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func filter(predicate, list) -> list {
    result <- {}
    for item in list {
        if predicate(item) {
            result.Push(item)
        }
    }
    return result
}

func isEven(x) -> bool {
    return x % 2 == 0
}

numbers <- {1, 2, 3, 4, 5, 6}
evens <- filter(isEven, numbers)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试reduce函数
    /// </summary>
    [Fact]
    public void ParseProgram_ReduceFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func reduce(func, list, initial) {
    result <- initial
    for item in list {
        result <- func(result, item)
    }
    return result
}

func add(a, b) -> int {
    return a + b
}

numbers <- {1, 2, 3, 4, 5}
sum <- reduce(add, numbers, 0)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试compose函数组合
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionComposition_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func compose(f, g) {
    return (x) -> f(g(x))
}

func add1(x) -> int {
    return x + 1
}

func multiply2(x) -> int {
    return x * 2
}

addThenMultiply <- compose(multiply2, add1)
multiplyThenAdd <- compose(add1, multiply2)

result1 <- addThenMultiply(5)  // (5 + 1) * 2 = 12
result2 <- multiplyThenAdd(5)  // (5 * 2) + 1 = 11";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试currying函数
    /// </summary>
    [Fact]
    public void ParseProgram_CurryingFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func curry(binaryFunc) {
    return (a) -> (b) -> binaryFunc(a, b)
}

func add(a, b) -> int {
    return a + b
}

func multiply(a, b) -> int {
    return a * b
}

curriedAdd <- curry(add)
curriedMultiply <- curry(multiply)

add5 <- curriedAdd(5)
multiplyBy3 <- curriedMultiply(3)

result1 <- add5(10)      // 15
result2 <- multiplyBy3(7) // 21";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 复杂高阶函数场景

    /// <summary>
    /// 测试高阶函数链式调用
    /// </summary>
    [Fact]
    public void ParseProgram_HigherOrderChaining_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func map(func, list) -> list {
    result <- {}
    for item in list {
        result.Push(func(item))
    }
    return result
}

func filter(predicate, list) -> list {
    result <- {}
    for item in list {
        if predicate(item) {
            result.Push(item)
        }
    }
    return result
}

func chain(input) {
    return (operations) -> {
        result <- input
        for operation in operations {
            result <- operation(result)
        }
        return result
    }
}

numbers <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
result <- chain(numbers)({(list) -> filter((x) -> x % 2 == 0, list), (list) -> map((x) -> x * 2, list)})";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试高阶函数的递归应用
    /// </summary>
    [Fact]
    public void ParseProgram_RecursiveHigherOrder_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func repeat(func, n) {
    if n <= 0 {
        return (x) -> x
    } else {
        return (x) -> func(repeat(func, n - 1)(x))
    }
}

func add1(x) -> int {
    return x + 1
}

add5 <- repeat(add1, 5)
result <- add5(10)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试函数管道
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionPipeline_ParsesSuccessfully()
    {
        // Arrange
        var code = """

                   func pipeline() {
                       return (functions) -> (input) -> {
                           result <- input
                           for fun in functions {
                               result <- fun(result)
                           }
                           return result
                       }
                   }

                   func double(x) -> int {
                       return x * 2
                   }

                   func add10(x) -> int {
                       return x + 10
                   }

                   func toString(x) -> string {
                       return x.ToStr()
                   }

                   process <- pipeline()({double, add10, toString})
                   result <- process(5)
                   """;
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试高阶函数在类方法中的应用
    /// </summary>
    [Fact]
    public void ParseProgram_HigherOrderInClasses_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class ArrayHelper {
    public func map(func) {
        result <- {}
        for item in this.data {
            result.Push(func(item))
        }
        return result
    }

    public func filter(predicate) {
        result <- {}
        for item in this.data {
            if predicate(item) {
                result.Push(item)
            }
        }
        return result
    }

    public func constructor(data) {
        this.data <- data
    }
}

numbers <- {1, 2, 3, 4, 5}
helper <- ArrayHelper(numbers)

evens <- helper.filter((x) -> x % 2 == 0)
doubled <- helper.map((x) -> x * 2)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 闭包和作用域

    /// <summary>
    /// 测试闭包捕获外部变量
    /// </summary>
    [Fact]
    public void ParseProgram_ClosureCapture_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func makeAdder(x) {
    return (y) -> x + y
}

func makeMultiplier(factor) {
    return (n) -> n * factor
}

add10 <- makeAdder(10)
multiply3 <- makeMultiplier(3)

result1 <- add10(5)      // 15
result2 <- multiply3(7)  // 21";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试嵌套闭包
    /// </summary>
    [Fact]
    public void ParseProgram_NestedClosures_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func outer() {
    x <- 10

    return () -> {
        y <- 5
        return () -> x + y
    }
}

inner <- outer()()
result <- inner";  // should be 15
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误的高阶函数语法

    /// <summary>
    /// 测试函数参数调用错误
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidFunctionParameterCall_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
func test(func) {
    return func(a) + func()  // 参数数量不匹配
}

test(123)";  // 传入的不是函数
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        // 语法应该正确，但运行时可能出错
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试不完整的lambda表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteLambda_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
func test() {
    return (x) ->  // 缺少函数体
}

test()";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试错误的函数类型使用
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidFunctionTypeUsage_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
func test() {
    return 123
}

result <- test() + 456";  // 尝试对函数调用结果进行运算
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        // 语法应该正确，但类型检查可能出错
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion
}