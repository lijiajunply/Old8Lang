using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using System.Diagnostics;

namespace Old8Lang.Tests.VirtualMachine.Performance;

[Collection("Sequential")]
public class VMExecutionPerformanceTests
{
    private (TimeSpan elapsed, string output) ExecuteVMCodeWithTiming(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var vm = new Old8Lang.Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            stopwatch.Stop();

            return (stopwatch.Elapsed, stringWriter.ToString().Trim());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #region 循环性能测试

    [Fact]
    public void Performance_Loop_SimpleForLoop()
    {
        var code = @"
            sum <- 0
            for i <- 0, i < 1000, i++ {
                sum <- sum + i
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 1000, $"Simple loop took {elapsed.TotalMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public void Performance_Loop_NestedForLoop()
    {
        var code = @"
            count <- 0
            for i <- 0, i < 50, i++ {
                for j <- 0, j < 50, j++ {
                    count <- count + 1
                }
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 1000, $"Nested loop took {elapsed.TotalMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public void Performance_Loop_WhileLoop()
    {
        var code = @"
            sum <- 0
            i <- 0
            while i < 1000 {
                sum <- sum + i
                i <- i + 1
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 1000, $"While loop took {elapsed.TotalMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public void Performance_Loop_ForInLoop()
    {
        var code = @"
            sum <- 0
            arr <- []
            for i <- 0, i < 100, i++ {
                arr <- arr + [i]
            }
            for x in arr {
                sum <- sum + x
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 1000, $"For-in loop took {elapsed.TotalMilliseconds}ms, expected < 1000ms");
    }

    #endregion

    #region 函数调用性能测试

    [Fact]
    public void Performance_FunctionCall_SimpleCall()
    {
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }

            sum <- 0
            for i <- 0, i < 1000, i++ {
                sum <- add(sum, i)
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 1000, $"Function calls took {elapsed.TotalMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public void Performance_FunctionCall_NestedCalls()
    {
        var code = @"
            func add1(x:int) -> int {
                return x + 1
            }

            func add2(x:int) -> int {
                return add1(add1(x))
            }

            sum <- 0
            for i <- 0, i < 100, i++ {
                sum <- add2(sum)
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 1000, $"Nested function calls took {elapsed.TotalMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public void Performance_FunctionCall_RecursiveCall()
    {
        var code = @"
            func factorial(n:int) -> int {
                if n <= 1 {
                    return 1
                }
                return n * factorial(n - 1)
            }

            result <- factorial(10)
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Recursive call took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region 集合操作性能测试

    [Fact]
    public void Performance_Collection_ArrayAccess()
    {
        var code = @"
            arr <- []
            for i <- 0, i < 1000, i++ {
                arr <- arr + [i]
            }

            sum <- 0
            for i <- 0, i < 1000, i++ {
                sum <- sum + arr[i]
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 1000, $"Array access took {elapsed.TotalMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public void Performance_Collection_ListOperations()
    {
        var code = @"
            list <- []
            for i <- 0, i < 1000, i++ {
                list <- list + [i]
            }

            sum <- 0
            for x in list {
                sum <- sum + x
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 1000, $"List operations took {elapsed.TotalMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public void Performance_Collection_DictionaryAccess()
    {
        var code = @"
            dict <- dict()
            for i <- 0, i < 100, i++ {
                dict[""key"" + i.ToStr()] <- i
            }

            sum <- 0
            for i <- 0, i < 100, i++ {
                sum <- sum + dict[""key"" + i.ToStr()]
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Dictionary access took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region 字符串操作性能测试

    [Fact]
    public void Performance_String_Concatenation()
    {
        var code = @"
            result <- """"
            for i <- 0, i < 100, i++ {
                result <- result + i.ToStr()
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"String concatenation took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    [Fact]
    public void Performance_String_Comparison()
    {
        var code = @"
            str1 <- ""Hello, World!""
            str2 <- ""Hello, World!""
            equal <- false
            for i <- 0, i < 1000, i++ {
                equal <- str1 == str2
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"String comparison took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    [Fact]
    public void Performance_String_Methods()
    {
        var code = @"
            str <- ""Hello, World!""
            for i <- 0, i < 100, i++ {
                upper <- str.ToUpper()
                lower <- str.ToLower()
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"String methods took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region 算术运算性能测试

    [Fact]
    public void Performance_Arithmetic_Addition()
    {
        var code = @"
            sum <- 0
            for i <- 0, i < 10000, i++ {
                sum <- sum + i
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Addition took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    [Fact]
    public void Performance_Arithmetic_Multiplication()
    {
        var code = @"
            product <- 1
            for i <- 1, i < 100, i++ {
                product <- product * i
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Multiplication took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    [Fact]
    public void Performance_Arithmetic_ComplexExpression()
    {
        var code = @"
            sum <- 0
            for i <- 0, i < 1000, i++ {
                result <- (i * 2 + 3) / (i + 1) - i
                sum <- sum + result
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Complex arithmetic took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region 类操作性能测试

    [Fact]
    public void Performance_Class_InstanceCreation()
    {
        var code = @"
            class Point {
                public x:int
                public y:int

                public func init(x:int, y:int) -> void {
                    this.x <- x
                    this.y <- y
                }
            }

            points <- []
            for i <- 0, i < 100, i++ {
                points <- points + [Point(i, i * 2)]
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Class instantiation took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    [Fact]
    public void Performance_Class_MethodCalls()
    {
        var code = @"
            class Calculator {
                public func add(a:int, b:int) -> int {
                    return a + b
                }
            }

            calc <- Calculator()
            sum <- 0
            for i <- 0, i < 1000, i++ {
                sum <- calc.add(sum, i)
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Method calls took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region Lambda 表达式性能测试

    [Fact]
    public void Performance_Lambda_SimpleLambda()
    {
        var code = @"
            add <- (a:int, b:int) -> {
                return a + b
            }

            sum <- 0
            for i <- 0, i < 1000, i++ {
                sum <- add(sum, i)
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 1000, $"Lambda calls took {elapsed.TotalMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public void Performance_Lambda_HigherOrderFunction()
    {
        var code = @"
            func map(arr, callback) -> void {
                for i <- 0, i < len(arr), i++ {
                    arr[i] <- callback(arr[i])
                }
            }

            arr <- []
            for i <- 0, i < 100, i++ {
                arr <- arr + [i]
            }

            map(arr, (x:int) -> {
                return x * 2
            })
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Higher-order function took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region 异步操作性能测试

    [Fact]
    public void Performance_Async_AwaitOperation()
    {
        var code = @"
            async func simpleAsync() -> int {
                return 42
            }

            sum <- 0
            for i <- 0, i < 100, i++ {
                task <- simpleAsync()
                result <- await task
                sum <- sum + result
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 1000, $"Async await took {elapsed.TotalMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public void Performance_Async_MultipleAwait()
    {
        var code = @"
            async func getValue(v:int) -> int {
                return v
            }

            task1 <- getValue(1)
            task2 <- getValue(2)
            task3 <- getValue(3)

            result1 <- await task1
            result2 <- await task2
            result3 <- await task3

            sum <- result1 + result2 + result3
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Multiple await took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region 控制流性能测试

    [Fact]
    public void Performance_ControlFlow_IfStatement()
    {
        var code = @"
            count <- 0
            for i <- 0, i < 1000, i++ {
                if i % 2 == 0 {
                    count <- count + 1
                }
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"If statement took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    [Fact]
    public void Performance_ControlFlow_SwitchStatement()
    {
        var code = @"
            sum <- 0
            for i <- 0, i < 100, i++ {
                result <- match i % 3 {
                    case 0 -> 0
                    case 1 -> 1
                    default -> 2
                }
                sum <- sum + result
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Switch statement took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    [Fact]
    public void Performance_ControlFlow_TernaryOperator()
    {
        var code = @"
            sum <- 0
            for i <- 0, i < 1000, i++ {
                sum <- sum + (i % 2 == 0 ? i : 0)
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Ternary operator took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region 泛型操作性能测试

    [Fact]
    public void Performance_Generic_GenericFunction()
    {
        var code = @"
            func identity<T>(value:T) -> T {
                return value
            }

            sum <- 0
            for i <- 0, i < 1000, i++ {
                sum <- identity<int>(sum) + i
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 1000, $"Generic function took {elapsed.TotalMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public void Performance_Generic_GenericClass()
    {
        var code = @"
            class Box<T> {
                public value:T

                public func init(value:T) -> void {
                    this.value <- value
                }
            }

            boxes <- []
            for i <- 0, i < 100, i++ {
                boxes <- boxes + [Box<int>(i)]
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Generic class took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region 综合性能测试

    [Fact]
    public void Performance_Complex_Algorithm()
    {
        var code = @"
            func fibonacci(n:int) -> int {
                if n <= 1 {
                    return n
                }
                return fibonacci(n - 1) + fibonacci(n - 2)
            }

            result <- fibonacci(15)
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Fibonacci took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    [Fact]
    public void Performance_Complex_Sorting()
    {
        var code = @"
            arr <- [64, 34, 25, 12, 22, 11, 90, 88]
            n <- 8

            for i <- 0, i < n - 1, i++ {
                for j <- 0, j < n - i - 1, j++ {
                    if arr[j] > arr[j + 1] {
                        temp <- arr[j]
                        arr[j] <- arr[j + 1]
                        arr[j + 1] <- temp
                    }
                }
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Bubble sort took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    [Fact]
    public void Performance_Complex_MatrixMultiplication()
    {
        var code = @"
            matrix1 <- [[1, 2], [3, 4]]
            matrix2 <- [[5, 6], [7, 8]]

            result <- [[0, 0], [0, 0]]
            for i <- 0, i < 2, i++ {
                for j <- 0, j < 2, j++ {
                    sum <- 0
                    for k <- 0, k < 2, k++ {
                        sum <- sum + matrix1[i][k] * matrix2[k][j]
                    }
                    result[i][j] <- sum
                }
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Matrix multiplication took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region 生成器性能测试

    [Fact]
    public void Performance_Generator_YieldOperation()
    {
        var code = @"
            func generate() -> {
                for i <- 0, i < 100, i++ {
                    yield i
                }
            }

            gen <- generate()
            sum <- 0
            while gen.MoveNext() {
                sum <- sum + gen.Current
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Generator took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region 异常处理性能测试

    [Fact]
    public void Performance_ExceptionHandling_TryCatch()
    {
        var code = @"
            sum <- 0
            for i <- 0, i < 100, i++ {
                try {
                    sum <- sum + i
                } catch {
                }
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Try-catch took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region Defer 语句性能测试

    [Fact]
    public void Performance_Defer_DeferExecution()
    {
        var code = @"
            func process() -> void {
                defer {
                }
            }

            for i <- 0, i < 100, i++ {
                process()
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Defer took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region Using 语句性能测试

    [Fact]
    public void Performance_Using_ResourceManagement()
    {
        var code = @"
            class Resource {
                public func Dispose() -> void {
                }
            }

            for i <- 0, i < 100, i++ {
                using res <- Resource() {
                }
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Using took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region 选择语句性能测试

    [Fact(Skip = "先跳过")]
    public void Performance_Select_ChannelSelect()
    {
        var code = @"
            ch1 <- ChannelCreate()
            ch2 <- ChannelCreate()

            ChannelSend(ch1, 1)
            ChannelSend(ch2, 2)

            result <- 0
            for i <- 0, i < 10, i++ {
                select {
                    case val from ch1 -> {
                        result <- result + val
                    }
                    case val from ch2 -> {
                        result <- result + val
                    }
                }
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 1000, $"Select took {elapsed.TotalMilliseconds}ms, expected < 1000ms");
    }

    #endregion

    #region 类型转换性能测试

    [Fact]
    public void Performance_TypeConversion_Conversions()
    {
        var code = @"
            sum <- 0
            for i <- 0, i < 1000, i++ {
                d <- double(i)
                ii <- int(d)
                sum <- sum + ii
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Type conversion took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region 范围操作性能测试

    [Fact]
    public void Performance_Range_Iteration()
    {
        var code = @"
            sum <- 0
            for i in [0~1000] {
                sum <- sum + i
            }
        ";

        var (elapsed, _) = ExecuteVMCodeWithTiming(code);
        Assert.True(elapsed.TotalMilliseconds < 500, $"Range iteration took {elapsed.TotalMilliseconds}ms, expected < 500ms");
    }

    #endregion
}
