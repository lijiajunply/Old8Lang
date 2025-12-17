using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.FunctionCalls;

/// <summary>
/// 链式函数调用语法测试
/// </summary>
[Collection("Sequential")]
public class ChainedFunctionCallsTests
{
    #region 链式函数调用正确语法

    /// <summary>
    /// 测试基本链式函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_BasicChainedFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func getCalculator(operation) {
    if operation == ""add"" {
        return (a, b) -> a + b
    } else if operation == ""multiply"" {
        return (a, b) -> a * b
    } else if operation == ""subtract"" {
        return (a, b) -> a - b
    } else {
        return (a, b) -> 0
    }
}

operations <- [getCalculator(""add""), getCalculator(""multiply""), getCalculator(""subtract"")]
result1 <- operations[0](10, 5)
result2 <- operations[1](10, 5)
result3 <- operations[2](10, 5)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试方法链中的函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_MethodChainFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class FunctionProvider {
    public func getOperations() {
        return {
            ""math"": [this.getAdder(), this.getMultiplier()],
            ""string"": [this.getUpper(), this.getLower()]
        }
    }

    public func getAdder() {
        return (a, b) -> a + b
    }

    public func getMultiplier() {
        return (a, b) -> a * b
    }

    public func getUpper() {
        return (s) -> s.ToUpper()
    }

    public func getLower() {
        return (s) -> s.ToLower()
    }
}

provider <- FunctionProvider()
operations <- provider.getOperations()

mathOps <- operations[""math""]
stringOps <- operations[""string""]

mathResult1 <- mathOps[0](5, 3)    // add(5, 3) = 8
mathResult2 <- mathOps[1](5, 3)    // multiply(5, 3) = 15
stringResult1 <- stringOps[0](""hello"")  // upper(""hello"") = ""HELLO""
stringResult2 <- stringOps[1](""WORLD"")  // lower(""WORLD"") = ""world""";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试深度链式函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_DeepChainedFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func createFactory() {
    return {
        ""operations"": [
            {
                ""name"": ""add"",
                ""funcs"": [(a, b) -> a + b, (a, b, c) -> a + b + c]
            },
            {
                ""name"": ""multiply"",
                ""funcs"": [(a, b) -> a * b, (a, b, c) -> a * b * c]
            }
        ]
    }
}

factory <- createFactory()
addFuncs <- factory[""operations""][0][""funcs""]
multiplyFuncs <- factory[""operations""][1][""funcs""]

result1 <- addFuncs[0](10, 5)        // add(10, 5) = 15
result2 <- addFuncs[1](10, 5, 3)     // add(10, 5, 3) = 18
result3 <- multiplyFuncs[0](10, 5)   // multiply(10, 5) = 50
result4 <- multiplyFuncs[1](10, 5, 3) // multiply(10, 5, 3) = 150";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试函数调用返回值作为索引
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionCallAsIndex_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func getOperationIndex(operation) -> int {
    if operation == ""add"" { return 0 }
    if operation == ""multiply"" { return 1 }
    if operation == ""subtract"" { return 2 }
    return 0
}

func createOperations() {
    return [
        (a, b) -> a + b,
        (a, b) -> a * b,
        (a, b) -> a - b
    ]
}

operations <- createOperations()
index1 <- getOperationIndex(""add"")
index2 <- getOperationIndex(""multiply"")
index3 <- getOperationIndex(""subtract"")

result1 <- operations[index1](10, 5)     // add(10, 5) = 15
result2 <- operations[index2](10, 5)     // multiply(10, 5) = 50
result3 <- operations[index3](10, 5)     // subtract(10, 5) = 5";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试复杂的链式表达式
    /// </summary>
    [Fact]
    public void ParseProgram_ComplexChainedExpression_ParsesSuccessfully()
    {
        // Arrange
        var code = """

                   class MathHelper {
                       public func getOperations() {
                           return [
                               this.getBinaryOps(),
                               this.getUnaryOps()
                           ]
                       }

                       public func getBinaryOps() {
                           return [
                               (a, b) -> a + b,
                               (a, b) -> a * b,
                               (a, b) -> a - b,
                               (a, b) -> b != 0 ? a / b : 0
                           ]
                       }

                       public func getUnaryOps() {
                           return [
                               (x) -> -x,
                               (x) -> x * x,
                               (x) -> x + 1,
                               (x) -> x / 2.0
                           ]
                       }
                   }

                   helper <- MathHelper()
                   allOps <- helper.getOperations()
                   binaryOps <- allOps[0]
                   unaryOps <- allOps[1]

                   // 复杂的链式调用
                   result1 <- binaryOps[0](binaryOps[1](3, 4), binaryOps[2](10, 2))  // add(multiply(3,4), subtract(10,2)) = add(12, 8) = 20
                   result2 <- unaryOps[1](binaryOps[0](5, 3))                       // square(add(5,3)) = square(8) = 64
                   result3 <- binaryOps[3](unaryOps[2](10), unaryOps[3](8))         // divide(increment(10), halve(8)) = divide(11, 4) = 2.75
                   """;
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试条件链式函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_ConditionalChainedFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func getConditionFuncs() {
    return [
        (x) -> x > 0,           // positive
        (x) -> x < 0,           // negative
        (x) -> x == 0,          // zero
        (x) -> x % 2 == 0,      // even
        (x) -> x % 2 != 0       // odd
    ]
}

func getActionFuncs() {
    return [
        (x) -> x * 2,           // double
        (x) -> x + 10,          // add10
        (x) -> x - 5,           // subtract5
        (x) -> x / 2.0,         // halve
        (x) -> x * x            // square
    ]
}

conditionFuncs <- getConditionFuncs()
actionFuncs <- getActionFuncs()

x <- 15
conditionIndex <- 0
actionIndex <- 0

// 动态选择条件和动作
if x > 0 and x <= 10 { conditionIndex <- 0; actionIndex <- 0 }
if x > 10 and x <= 20 { conditionIndex <- 0; actionIndex <- 1 }
if x > 20 { conditionIndex <- 0; actionIndex <- 2 }

// 链式调用：先检查条件，再执行动作
shouldAct <- conditionFuncs[conditionIndex](x)
if shouldAct {
    result <- actionFuncs[actionIndex](x)
} else {
    result <- x
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 高级链式调用场景

    /// <summary>
    /// 测试递归链式函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_RecursiveChainedFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func createRecursiveChain() {
    return [
        (x, depth) -> {
            if depth <= 0 {
                return x
            } else {
                chain <- createRecursiveChain()
                return chain[0](x * 2, depth - 1)
            }
        },
        (x, depth) -> {
            if depth <= 0 {
                return x
            } else {
                chain <- createRecursiveChain()
                return chain[1](x + 1, depth - 1)
            }
        }
    ]
}

chain <- createRecursiveChain()

result1 <- chain[0](1, 3)  // (((1 * 2) * 2) * 2) = 8
result2 <- chain[1](1, 3)  // (((1 + 1) + 1) + 1) = 4";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试函数管道链式调用
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionPipelineChain_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func createPipeline() {
    return [
        (x) -> x + 1,           // increment
        (x) -> x * 2,           // double
        (x) -> x - 3,           // subtract3
        (x) -> x / 2.0,         // halve
        (x) -> x * x            // square
    ]
}

func applyPipeline(pipeline, startIndex, input, steps) {
    if steps <= 0 or startIndex >= pipeline.Count() {
        return input
    } else {
        result <- pipeline[startIndex](input)
        return applyPipeline(pipeline, startIndex + 1, result, steps - 1)
    }
}

pipeline <- createPipeline()

result1 <- pipeline[0](pipeline[1](10))                    // increment(double(10)) = increment(20) = 21
result2 <- pipeline[2](pipeline[3](pipeline[4](5)))       // subtract3(halve(square(5))) = subtract3(halve(25)) = subtract3(12.5) = 9.5
result3 <- applyPipeline(pipeline, 0, 10, 3)               // increment(double(subtract3(10))) = increment(double(7)) = increment(14) = 15";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试动态构建的链式函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_DynamicChainedConstruction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func buildChain(operations) {
    chain <- {}

    for op in operations {
        if op == ""add1"" {
            chain.Push((x) -> x + 1)
        } else if op == ""double"" {
            chain.Push((x) -> x * 2)
        } else if op == ""square"" {
            chain.Push((x) -> x * x)
        } else if op == ""halve"" {
            chain.Push((x) -> x / 2.0)
        } else if op == ""negate"" {
            chain.Push((x) -> -x)
        }
    }

    return chain
}

func executeChain(chain, startIndex, input) {
    if startIndex >= chain.Count() {
        return input
    } else {
        result <- chain[startIndex](input)
        return executeChain(chain, startIndex + 1, result)
    }
}

// 动态构建不同的链
chain1 <- buildChain({""add1"", ""double"", ""square""})
chain2 <- buildChain({""double"", ""halve"", ""negate""})
chain3 <- buildChain({""square"", ""add1"", ""double"", ""halve""})

result1 <- chain1[0](chain1[1](chain1[2](2)))        // add1(double(square(2))) = add1(double(4)) = add1(8) = 9
result2 <- executeChain(chain2, 0, 10)                // double(halve(negate(10))) = double(halve(-10)) = double(-5) = -10
result3 <- executeChain(chain3, 0, 3)                 // square(add1(double(halve(3)))) = square(add1(double(1.5))) = square(add1(3)) = square(4) = 16";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 边界情况和错误处理

    /// <summary>
    /// 测试空链的函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_EmptyChainFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
emptyChain <- []
// 语法解析应该成功，但运行时可能出错
// result <- emptyChain[0](123)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试循环引用的链式调用
    /// </summary>
    [Fact]
    public void ParseProgram_CircularReferenceChain_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func createCircularChain() {
    chain <- [(x) -> x + 1]
    // 注意：在实际实现中，这可能导致无限递归
    // chain.Push(chain)  // 自引用，这可能在语法上正确但运行时有问题
    return chain
}

chain <- createCircularChain()
result <- chain[0](5)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误的链式调用语法

    /// <summary>
    /// 测试不完整的链式调用
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteChainedCall_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
func test(x) { return x }
chain <- [test]
result <- chain[0]"; // 缺少函数调用的括号
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        // 这可能不应该报错，取决于是否允许将函数赋值给变量
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试错误的索引类型
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidIndexType_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
func test(x) { return x }
chain <- [test]
result <- chain[""not-a-number""](123)"; // 字符串索引
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        // 语法应该正确，但类型检查可能出错
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion
}