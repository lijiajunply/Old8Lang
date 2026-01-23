using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Functions;

/// <summary>
/// 编译器模式下的高级函数功能测试 - 高阶函数
/// </summary>
public class HigherOrderTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void MapFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func map(list:list, transform:(int) -> int) -> list {
                result <- {}
                i <- 0
                while i < list.Count() {
                    item <- list[i]
                    result.Add(transform(item))
                    i <- i + 1
                }
                return result
            }
            
            numbers <- {1, 2, 3, 4, 5}
            doubled <- map(numbers, (x:int) -> x * 2)
            Assert.Equal({2, 4, 6, 8, 10}, doubled)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void FilterFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func filter(list:list, predicate:(int) -> bool) -> list {
                result <- {}
                i <- 0
                while i < list.Count() {
                    item <- list[i]
                    if predicate(item) {
                        result.Add(item)
                    }
                    i <- i + 1
                }
                return result
            }
            
            numbers <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            evens <- filter(numbers, (x:int) -> x % 2 == 0)
            Assert.Equal({2, 4, 6, 8, 10}, evens)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ReduceFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func reduce(list:list, initial:int, accumulator:(int, int) -> int) -> int {
                result <- initial
                i <- 0
                while i < list.Count() {
                    item <- list[i]
                    result <- accumulator(result, item)
                    i <- i + 1
                }
                return result
            }
            
            numbers <- {1, 2, 3, 4, 5}
            sum <- reduce(numbers, 0, (acc:int, x:int) -> acc + x)
            product <- reduce(numbers, 1, (acc:int, x:int) -> acc * x)
            
            Assert.Equal(15, sum)      // 0 + 1 + 2 + 3 + 4 + 5 = 15
            Assert.Equal(120, product) // 1 * 1 * 2 * 3 * 4 * 5 = 120
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ComposeFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func compose(f:(int) -> int, g:(int) -> int) -> (int) -> int {
                return (x:int) -> f(g(x))
            }
            
            add5 <- (x:int) -> x + 5
            multiply3 <- (x:int) -> x * 3
            
            add5ThenMultiply3 <- compose(multiply3, add5)
            multiply3ThenAdd5 <- compose(add5, multiply3)
            
            result1 <- add5ThenMultiply3(10)  // (10 + 5) * 3 = 45
            result2 <- multiply3ThenAdd5(10)  // (10 * 3) + 5 = 35
            
            Assert.Equal(45, result1)
            Assert.Equal(35, result2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void CurryFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func curry(func:(int, int) -> int) -> (int) -> (int) -> int {
                return (a:int) -> (b:int) -> func(a, b)
            }
            
            add <- (a:int, b:int) -> a + b
            curriedAdd <- curry(add)
            
            add5 <- curriedAdd(5)
            result <- add5(10)
            Assert.Equal(15, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void PipeFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func pipe(value:int, operations:list) -> int {
                result <- value
                i <- 0
                while i < operations.Count() {
                    op <- operations[i]
                    result <- op(result)
                    i <- i + 1
                }
                return result
            }
            
            double <- (x:int) -> x * 2
            add10 <- (x:int) -> x + 10
            square <- (x:int) -> x * x
            
            ops <- {double, add10, square}
            result <- pipe(3, ops)  // ((3 * 2) + 10)^2 = (6 + 10)^2 = 16^2 = 256
            
            Assert.Equal(256, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void FindFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func find(list:list, predicate:(int) -> bool) -> int? {
                i <- 0
                while i < list.Count() {
                    item <- list[i]
                    if predicate(item) {
                        return item
                    }
                    i <- i + 1
                }
                return null
            }
            
            numbers <- {1, 3, 5, 7, 9, 2, 4, 6, 8, 10}
            
            result1 <- find(numbers, (x:int) -> x % 2 == 0)  // 第一个偶数
            result2 <- find(numbers, (x:int) -> x > 10)   // 不存在的数字
            result3 <- find(numbers, (x:int) -> x == 7)    // 特定数字
            
            Assert.Equal(2, result1)
            Assert.Equal(null, result2)
            Assert.Equal(7, result3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EveryFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func every(list:list, predicate:(int) -> bool) -> bool {
                i <- 0
                while i < list.Count() {
                    item <- list[i]
                    if not predicate(item) {
                        return false
                    }
                    i <- i + 1
                }
                return true
            }
            
            numbers1 <- {2, 4, 6, 8, 10}
            numbers2 <- {1, 2, 3, 4, 5}
            numbers3 <- {}
            
            result1 <- every(numbers1, (x:int) -> x % 2 == 0)  // 全部是偶数
            result2 <- every(numbers2, (x:int) -> x % 2 == 0)  // 不全是偶数
            result3 <- every(numbers3, (x:int) -> x % 2 == 0)  // 空列表
            
            Assert.Equal(true, result1)
            Assert.Equal(false, result2)
            Assert.Equal(true, result3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void SomeFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func some(list:list, predicate:(int) -> bool) -> bool {
                i <- 0
                while i < list.Count() {
                    item <- list[i]
                    if predicate(item) {
                        return true
                    }
                    i <- i + 1
                }
                return false
            }
            
            numbers1 <- {1, 3, 5, 7, 9}
            numbers2 <- {2, 4, 6, 8, 10}
            numbers3 <- {1, 2, 3, 4, 5}
            numbers4 <- {}
            
            result1 <- some(numbers1, (x:int) -> x % 2 == 0)  // 没有偶数
            result2 <- some(numbers2, (x:int) -> x % 2 == 0)  // 有偶数
            result3 <- some(numbers3, (x:int) -> x % 2 == 0)  // 有偶数
            result4 <- some(numbers4, (x:int) -> x % 2 == 0)  // 空列表
            
            Assert.Equal(false, result1)
            Assert.Equal(true, result2)
            Assert.Equal(true, result3)
            Assert.Equal(false, result4)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ZipFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func zip(list1:list, list2:list) -> list {
                result <- {}
                min_len <- list1.Count()
                if list2.Count() < min_len {
                    min_len <- list2.Count()
                }
                
                i <- 0
                while i < min_len {
                    pair <- {list1[i], list2[i]}
                    result.Add(pair)
                    i <- i + 1
                }
                return result
            }
            
            numbers1 <- {1, 2, 3}
            numbers2 <- {10, 20, 30, 40}
            pairs <- zip(numbers1, numbers2)
            
            Assert.Equal({{1, 10}, {2, 20}, {3, 30}}, pairs)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ChainFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func chain(functions:list) -> (int) -> int {
                return (x:int) -> {
                    result <- x
                    i <- 0
                    while i < functions.Count() {
                        func <- functions[i]
                        result <- func(result)
                        i <- i + 1
                    }
                    return result
                }
            }
            
            add5 <- (x:int) -> x + 5
            multiply2 <- (x:int) -> x * 2
            subtract3 <- (x:int) -> x - 3
            
            pipeline <- chain({add5, multiply2, subtract3})
            result <- pipeline(10)  // ((10 + 5) * 2) - 3 = (15 * 2) - 3 = 30 - 3 = 27
            
            Assert.Equal(27, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}