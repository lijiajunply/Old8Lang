using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Functions;

/// <summary>
/// 编译器模式下的高级函数功能测试 - 泛型函数
/// </summary>
public class GenericFunctionTests(ITestOutputHelper output)
{
    public ITestOutputHelper Output { get; } = output;

    [Fact(Skip = "Generic function instantiation not supported in compiler mode")]
    public void BasicGenericFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func identity<T>(value:T) -> T {
                return value
            }
            
            intResult <- identity<int>(42)
            stringResult <- identity<string>(""hello"")
            doubleResult <- identity<double>(3.14)
            
            Assert.Equal(42, intResult)
            Assert.Equal(""hello"", stringResult)
            Assert.Equal(3.14, doubleResult)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact(Skip = "Generic function instantiation not supported in compiler mode")]
    public void MultipleTypeParameters_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func createPair<K, V>(key:K, value:V) -> string {
                return key.ToStr() + "":"" + value.ToStr()
            }
            
            result1 <- createPair<string, int>(""age"", 25)
            result2 <- createPair<int, string>(1, ""first"")
            result3 <- createPair<double, bool>(3.14, true)
            
            Assert.Equal(""age:25"", result1)
            Assert.Equal(""1:first"", result2)
            Assert.Equal(""3.14:true"", result3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact(Skip = "Generic function instantiation not supported in compiler mode")]
    public void GenericFunctionWithConstraints_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func processComparable<T>(value:T) -> T where T: IComparable {
                return value
            }
            
            func processMultiple<T, U>(first:T, second:U) -> string 
                where T: IComparable, U: ISerializable {
                return first.ToStr() + ""|"" + second.ToStr()
            }
            
            // 注意：实际测试中可能需要调整，取决于接口支持情况
            result1 <- processComparable<int>(10)
            result2 <- processComparable<string>(""test"")
            
            Assert.Equal(10, result1)
            Assert.Equal(""test"", result2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact(Skip = "Generic function instantiation not supported in compiler mode")]
    public void GenericArrayOperations_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func getFirst<T>(array:array<T>) -> T? {
                if array.Length == 0 {
                    return null
                }
                return array[0]
            }
            
            func getLast<T>(array:array<T>) -> T? {
                if array.Length == 0 {
                    return null
                }
                return array[array.Length - 1]
            }
            
            intArray <- [1, 2, 3, 4, 5]
            stringArray <- [""a"", ""b"", ""c""]
            emptyArray <- []
            
            intFirst <- getFirst<int>(intArray)
            intLast <- getLast<int>(intArray)
            stringFirst <- getFirst<string>(stringArray)
            stringLast <- getLast<string>(stringArray)
            emptyFirst <- getFirst<int>(emptyArray)
            emptyLast <- getLast<int>(emptyArray)
            
            Assert.Equal(1, intFirst)
            Assert.Equal(5, intLast)
            Assert.Equal(""a"", stringFirst)
            Assert.Equal(""c"", stringLast)
            Assert.Equal(null, emptyFirst)
            Assert.Equal(null, emptyLast)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact(Skip = "Generic function instantiation not supported in compiler mode")]
    public void GenericListOperations_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func filterList<T>(list:list<T>, predicate:(T) -> bool) -> list<T> {
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
            
            func mapList<T, U>(list:list<T>, transform:(T) -> U) -> list<U> {
                result <- {}
                i <- 0
                while i < list.Count() {
                    item <- list[i]
                    result.Add(transform(item))
                    i <- i + 1
                }
                return result
            }
            
            numbers <- {1, 2, 3, 4, 5, 6}
            evens <- filterList<int>(numbers, (x:int) -> x % 2 == 0)
            squares <- mapList<int, int>(evens, (x:int) -> x * x)
            
            Assert.Equal({2, 4, 6}, evens)
            Assert.Equal({4, 16, 36}, squares)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact(Skip = "Generic function instantiation not supported in compiler mode")]
    public void GenericComparator_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func max<T>(a:T, b:T) -> T where T: IComparable {
                if a > b {
                    return a
                } else {
                    return b
                }
            }
            
            func min<T>(a:T, b:T) -> T where T: IComparable {
                if a < b {
                    return a
                } else {
                    return b
                }
            }
            
            intMax <- max<int>(10, 20)
            intMin <- min<int>(10, 20)
            doubleMax <- max<double>(3.14, 2.71)
            doubleMin <- min<double>(3.14, 2.71)
            
            Assert.Equal(20, intMax)
            Assert.Equal(10, intMin)
            Assert.Equal(3.14, doubleMax)
            Assert.Equal(2.71, doubleMin)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact(Skip = "Generic function instantiation not supported in compiler mode")]
    public void GenericNullableType_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func safeDivide<T>(numerator:T, denominator:T) -> T? 
                where T: IComparable {
                if denominator == 0 {
                    return null
                }
                return numerator / denominator
            }
            
            result1 <- safeDivide<int>(10, 2)
            result2 <- safeDivide<int>(10, 0)
            result3 <- safeDivide<double>(5.0, 2.0)
            result4 <- safeDivide<double>(5.0, 0.0)
            
            Assert.Equal(5, result1)
            Assert.Equal(null, result2)
            Assert.Equal(2.5, result3)
            Assert.Equal(null, result4)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact(Skip = "Generic function instantiation not supported in compiler mode")]
    public void GenericRecursiveFunction_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func factorial<T>(n:T) -> T where T: IComparable {
                if n <= 1 {
                    return 1
                }
                return n * factorial<T>(n - 1)
            }
            
            func sumRange<T>(start:T, end:T) -> T where T: IComparable {
                if start > end {
                    return 0
                }
                return start + sumRange<T>(start + 1, end)
            }
            
            fact5 <- factorial<int>(5)
            sum1to5 <- sumRange<int>(1, 5)
            
            Assert.Equal(120, fact5)  // 5! = 120
            Assert.Equal(15, sum1to5)  // 1+2+3+4+5 = 15
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact(Skip = "Generic function instantiation not supported in compiler mode")]
    public void GenericFunctionComposition_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func compose<T, U, V>(f:function, g:function) -> function {
                return (x:T) -> f(g(x))
            }
            
            func pipe<T, U, V>(value:T, first:function, second:function) -> V {
                return second(first(value))
            }
            
            add5 <- (x:int) -> x + 5
            multiply2 <- (x:int) -> x * 2
            toString <- (x:int) -> x.ToStr()
            
            add5ThenMultiply2 <- compose<int, int, int>(multiply2, add5)
            multiply2ThenToString <- compose<string, int, string>(toString, multiply2)
            
            result1 <- add5ThenMultiply2(10)  // (10 + 5) * 2 = 30
            result2 <- multiply2ThenToString(10)  // (10 * 2).ToStr() = ""20""
            
            Assert.Equal(30, result1)
            Assert.Equal(""20"", result2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact(Skip = "Generic function instantiation not supported in compiler mode")]
    public void GenericTypeInference_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func swap<T>(a:T, b:T) -> list<T> {
                return {b, a}
            }
            
            func createTriple<T>(first:T, second:T, third:T) -> array<T> {
                return [first, second, third]
            }
            
            result1 <- swap(10, 20)  // 推断为 int
            result2 <- swap(""a"", ""b"")  // 推断为 string
            result3 <- createTriple(1, 2, 3)  // 推断为 int
            result4 <- createTriple(""x"", ""y"", ""z"")  // 推断为 string
            
            Assert.Equal({20, 10}, result1)
            Assert.Equal({""b"", ""a""}, result2)
            Assert.Equal([1, 2, 3], result3)
            Assert.Equal([""x"", ""y"", ""z""], result4)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact(Skip = "Generic function instantiation not supported in compiler mode")]
    public void GenericFunctionOverloading_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func process<T>(value:T) -> string {
                return ""generic: "" + value.ToStr()
            }
            
            func process(value:int) -> string {
                return ""int: "" + value.ToStr()
            }
            
            func process(value:string) -> string {
                return ""string: "" + value
            }
            
            result1 <- process<int>(42)        // 调用泛型版本
            result2 <- process(42)              // 调用 int 专用版本
            result3 <- process<string>(""test"")  // 调用泛型版本
            result4 <- process(""test"")         // 调用 string 专用版本
            
            Assert.Equal(""generic: 42"", result1)
            Assert.Equal(""int: 42"", result2)
            Assert.Equal(""generic: test"", result3)
            Assert.Equal(""string: test"", result4)
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