using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Functions;

/// <summary>
/// 编译器模式下的高级函数功能测试 - 闭包
/// </summary>
public class ClosureTests
{
    private readonly ITestOutputHelper _output;

    public ClosureTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BasicClosure_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func makeAdder(n:int) -> function {
                return (x:int) -> x + n
            }
            
            add5 <- makeAdder(5)
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
    public void ClosureWithVariableCapture_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            multiplier <- 3
            
            func createMultiplier() -> function {
                // 捕获外部变量 multiplier
                return (x:int) -> x * multiplier
            }
            
            times3 <- createMultiplier()
            result <- times3(4)
            Assert.Equal(12, result)
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
    public void MultipleClosureInstances_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func makeCounter() -> function {
                count <- 0
                return () -> {
                    count <- count + 1
                    return count
                }
            }
            
            counter1 <- makeCounter()
            counter2 <- makeCounter()
            
            result1 <- counter1()
            result2 <- counter1()
            result3 <- counter2()
            result4 <- counter1()
            result5 <- counter2()
            
            Assert.Equal(1, result1)
            Assert.Equal(2, result2)
            Assert.Equal(1, result3)
            Assert.Equal(3, result4)
            Assert.Equal(2, result5)
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
    public void ClosureWithFunctionParameter_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func applyOperation(x:int, operation:function) -> int {
                return operation(x)
            }
            
            func makeDoubler() -> function {
                return (x:int) -> x * 2
            }
            
            doubler <- makeDoubler()
            result <- applyOperation(5, doubler)
            Assert.Equal(10, result)
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
    public void ClosureCapturingLocalVariables_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func createAccumulator(initial:int) -> function {
                sum <- initial
                return (value:int) -> {
                    sum <- sum + value
                    return sum
                }
            }
            
            acc1 <- createAccumulator(10)
            acc2 <- createAccumulator(100)
            
            result1 <- acc1(5)   // 10 + 5 = 15
            result2 <- acc1(3)   // 15 + 3 = 18
            result3 <- acc2(50)  // 100 + 50 = 150
            result4 <- acc2(25)  // 150 + 25 = 175
            result5 <- acc1(2)   // 18 + 2 = 20
            
            Assert.Equal(15, result1)
            Assert.Equal(18, result2)
            Assert.Equal(150, result3)
            Assert.Equal(175, result4)
            Assert.Equal(20, result5)
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
    public void ClosureWithLoop_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            functions <- {}
            i <- 0
            while i < 3 {
                // 创建捕获循环变量的闭包
                value <- i
                functions.Add((x:int) -> x + value)
                i <- i + 1
            }
            
            result1 <- functions[0](10)  // 10 + 0 = 10
            result2 <- functions[1](10)  // 10 + 1 = 11
            result3 <- functions[2](10)  // 10 + 2 = 12
            
            Assert.Equal(10, result1)
            Assert.Equal(11, result2)
            Assert.Equal(12, result3)
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
    public void ClosureReturningClosure_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func makeMultiplierFactory(factor:int) -> function {
                return () -> {
                    // 内层闭包捕获 factor
                    return (x:int) -> x * factor
                }
            }
            
            factory <- makeMultiplierFactory(7)
            multiplier7 <- factory()
            result <- multiplier7(8)
            Assert.Equal(56, result)
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
    public void ClosureWithMultipleCaptures_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            base_value <- 10
            multiplier <- 3
            
            func createComplexOperation() -> function {
                offset <- 5
                return (x:int) -> (x + offset) * multiplier + base_value
            }
            
            operation <- createComplexOperation()
            result <- operation(4)  // (4 + 5) * 3 + 10 = 9 * 3 + 10 = 27 + 10 = 37
            Assert.Equal(37, result)
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
    public void ClosureModifyingCapturedVariable_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func createToggle() -> function {
                state <- false
                return () -> {
                    state <- !state
                    return state
                }
            }
            
            toggle1 <- createToggle()
            toggle2 <- createToggle()
            
            result1 <- toggle1()  // true
            result2 <- toggle1()  // false
            result3 <- toggle2()  // true
            result4 <- toggle1()  // true
            result5 <- toggle2()  // false
            
            Assert.Equal(true, result1)
            Assert.Equal(false, result2)
            Assert.Equal(true, result3)
            Assert.Equal(true, result4)
            Assert.Equal(false, result5)
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
    public void ClosureWithArrayOperations_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func createArrayProcessor() -> function {
                return (input:array) -> {
                    result <- {}
                    i <- 0
                    while i < input.Length {
                        // 处理每个元素
                        value <- input[i]
                        if value > 0 {
                            result.Add(value * 2)
                        }
                        i <- i + 1
                    }
                    return result
                }
            }
            
            processor <- createArrayProcessor()
            input <- [1, -2, 3, -4, 5]
            output <- processor(input)
            
            Assert.Equal([2, 6, 10], output)
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
    public void ClosureCapturingThis_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Counter {
                private count
                
                func init() {
                    this.count <- 0
                }
                
                func createIncrementer() -> function {
                    return () -> {
                        this.count <- this.count + 1
                        return this.count
                    }
                }
                
                func getCount() -> int {
                    return this.count
                }
            }
            
            counter <- Counter()
            incrementer <- counter.createIncrementer()
            
            result1 <- incrementer()
            result2 <- incrementer()
            count <- counter.getCount()
            
            Assert.Equal(1, result1)
            Assert.Equal(2, result2)
            Assert.Equal(2, count)
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
    public void ClosureWithHigherOrderFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func map(list:list, transform:function) -> list {
                result <- {}
                i <- 0
                while i < list.Count() {
                    item <- list[i]
                    result.Add(transform(item))
                    i <- i + 1
                }
                return result
            }
            
            func createMapper(factor:int) -> function {
                return (input:list) -> map(input, (x:int) -> x * factor)
            }
            
            doubler <- createMapper(2)
            tripler <- createMapper(3)
            
            input <- {1, 2, 3, 4}
            result1 <- doubler(input)  // [2, 4, 6, 8]
            result2 <- tripler(input)  // [3, 6, 9, 12]
            
            Assert.Equal({2, 4, 6, 8}, result1)
            Assert.Equal({3, 6, 9, 12}, result2)
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