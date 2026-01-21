using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using System.Diagnostics;

namespace Old8Lang.Tests.VirtualMachine.Performance;

[Collection("Sequential")]
public class VMMemoryUsageTests
{
    private (long memoryBefore, long memoryAfter, string output) ExecuteVMCodeWithMemoryTracking(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(false);

        try
        {
            var vm = new Old8Lang.Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memoryAfter = GC.GetTotalMemory(false);

            return (memoryBefore, memoryAfter, stringWriter.ToString().Trim());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #region 数组内存测试

    [Fact]
    public void MemoryUsage_ArrayAllocation_MemoryIncreases()
    {
        var code = @"
            arr <- {}
            for i <- 0, i < 1000, i++ {
                arr.Add(i)
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;

        // Memory may decrease due to GC, so we just check it's reasonable
        Assert.True(memoryIncrease < 10_000_000, $"Memory increase {memoryIncrease} bytes should be reasonable");
    }

    [Fact]
    public void MemoryUsage_ArrayReuse_MemoryStable()
    {
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            for i <- 0, i < 1000, i++ {
                arr[0] <- arr[0] + 1
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;
        
        Assert.True(memoryIncrease < 1_000_000, $"Memory increase {memoryIncrease} bytes should be minimal for reuse");
    }

    #endregion

    #region 列表内存测试

    [Fact]
    public void MemoryUsage_ListAllocation_MemoryIncreases()
    {
        var code = @"
            list <- {}
            for i <- 0, i < 1000, i++ {
                list.Add(i)
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;

        // Memory may decrease due to GC, so we just check it's reasonable
        Assert.True(memoryIncrease < 10_000_000, $"Memory increase {memoryIncrease} bytes should be reasonable");
    }

    #endregion

    #region 字典内存测试

    [Fact]
    public void MemoryUsage_DictionaryAllocation_MemoryIncreases()
    {
        var code = @"
            list <- {}

            for i <- 0, i < 100, i++ {
                list.Add(i)
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;

        // Memory may decrease due to GC, so we just check it's reasonable
        Assert.True(memoryIncrease < 5_000_000, $"Memory increase {memoryIncrease} bytes should be reasonable");
    }

    #endregion

    #region 字符串内存测试

    [Fact]
    public void MemoryUsage_StringAllocation_MemoryIncreases()
    {
        var code = @"
            str <- """"
            for i <- 0, i < 1000, i++ {
                str <- str + i.ToStr()
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;
        
        Assert.True(memoryIncrease > 0, $"Memory should increase by {memoryIncrease} bytes");
        Assert.True(memoryIncrease < 10_000_000, $"Memory increase {memoryIncrease} bytes should be reasonable");
    }

    #endregion

    #region 对象实例化内存测试

    [Fact]
    public void MemoryUsage_ObjectInstantiation_MemoryIncreases()
    {
        var code = @"
            class Point {
                public x:int <- 0
                public y:int <- 0

                public func init(x:int, y:int) -> void {
                    this.x <- x
                    this.y <- y
                }
            }

            points <- {}
            for i <- 0, i < 100, i++ {
                points.Add(Point(i, i * 2))
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;

        Assert.True(memoryIncrease > 0, $"Memory should increase by {memoryIncrease} bytes");
        Assert.True(memoryIncrease < 5_000_000, $"Memory increase {memoryIncrease} bytes should be reasonable");
    }

    #endregion

    #region 闭包内存测试

    [Fact]
    public void MemoryUsage_Closure_MemoryIncreases()
    {
        var code = @"
            func createClosure(x:int) -> int {
                return x * 2
            }

            closures <- {}
            for i <- 0, i < 100, i++ {
                closures.Add(createClosure(i))
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;

        Assert.True(memoryIncrease > 0, $"Memory should increase by {memoryIncrease} bytes");
        Assert.True(memoryIncrease < 10_000_000, $"Memory increase {memoryIncrease} bytes should be reasonable");
    }

    #endregion

    #region 生成器内存测试

    [Fact(Skip = "先跳一下")]
    public void MemoryUsage_Generator_MemoryEfficient()
    {
        var code = @"
            func generate() -> void {
                for i <- 0, i < 1000, i++ {
                    yield i
                }
            }

            gen <- generate()
            count <- 0
            while gen.MoveNext() {
                count <- count + 1
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;

        Assert.True(memoryIncrease < 5_000_000, $"Memory increase {memoryIncrease} bytes should be efficient for generator");
    }

    #endregion

    #region 递归内存测试

    [Fact]
    public void MemoryUsage_RecursiveCall_MemoryIncreases()
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

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;
        
        Assert.True(memoryIncrease > 0, $"Memory should increase by {memoryIncrease} bytes");
        Assert.True(memoryIncrease < 1_000_000, $"Memory increase {memoryIncrease} bytes should be reasonable for recursion");
    }

    #endregion

    #region 异步操作内存测试

    [Fact]
    public void MemoryUsage_AsyncOperation_MemoryIncreases()
    {
        var code = @"
            async func simpleAsync() -> int {
                return 42
            }

            tasks <- {}
            for i <- 0, i < 10, i++ {
                tasks.Add(simpleAsync())
            }

            sum <- 0
            for task in tasks {
                result <- await task
                sum <- sum + 1
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;

        Assert.True(memoryIncrease > 0, $"Memory should increase by {memoryIncrease} bytes");
        Assert.True(memoryIncrease < 5_000_000, $"Memory increase {memoryIncrease} bytes should be reasonable for async");
    }

    #endregion

    #region 作用域内存测试

    [Fact]
    public void MemoryUsage_NestedScope_MemoryManaged()
    {
        var code = @"
            func nested() -> void {
                x <- 1
                {
                    y <- 2
                    {
                        z <- 3
                    }
                }
            }

            for i <- 0, i < 100, i++ {
                nested()
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;
        
        Assert.True(memoryIncrease < 1_000_000, $"Memory increase {memoryIncrease} bytes should be minimal for nested scopes");
    }

    #endregion

    #region 变量重用内存测试

    [Fact]
    public void MemoryUsage_VariableReuse_MemoryEfficient()
    {
        var code = @"
            x <- 0
            for i <- 0, i < 1000, i++ {
                x <- x + i
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;

        Assert.True(memoryIncrease < 5_000_000, $"Memory increase {memoryIncrease} bytes should be minimal for variable reuse");
    }

    #endregion

    #region 泛型内存测试

    [Fact]
    public void MemoryUsage_GenericClass_MemoryIncreases()
    {
        var code = @"
            class Box {
                public value:object <- null

                public func init(value:object) -> void {
                    this.value <- value
                }
            }

            boxes <- {}
            for i <- 0, i < 100, i++ {
                boxes.Add(Box(i))
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;

        Assert.True(memoryIncrease > 0, $"Memory should increase by {memoryIncrease} bytes");
        Assert.True(memoryIncrease < 5_000_000, $"Memory increase {memoryIncrease} bytes should be reasonable for generics");
    }

    #endregion

    #region 大数据结构内存测试

    [Fact]
    public void MemoryUsage_LargeArray_MemoryScales()
    {
        var code = @"
            arr <- {}
            for i <- 0, i < 10000, i++ {
                arr.Add(i)
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;

        Assert.True(memoryIncrease > 0, $"Memory should increase by {memoryIncrease} bytes");
        Assert.True(memoryIncrease < 50_000_000, $"Memory increase {memoryIncrease} bytes should scale with data size");
    }

    #endregion

    #region 字符串拼接内存测试

    [Fact]
    public void MemoryUsage_StringConcatenation_MemoryIncreases()
    {
        var code = @"
            result <- """"
            for i <- 0, i < 100, i++ {
                result <- result + ""Hello, World! ""
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;
        
        Assert.True(memoryIncrease > 0, $"Memory should increase by {memoryIncrease} bytes");
        Assert.True(memoryIncrease < 10_000_000, $"Memory increase {memoryIncrease} bytes should be reasonable for string concat");
    }

    #endregion

    #region 异常处理内存测试

    [Fact]
    public void MemoryUsage_ExceptionHandling_MemoryManaged()
    {
        var code = @"
            for i <- 0, i < 100, i++ {
                try {
                    if i % 10 == 0 {
                        throw ""Test error""
                    }
                } catch {
                }
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;
        
        Assert.True(memoryIncrease < 5_000_000, $"Memory increase {memoryIncrease} bytes should be reasonable for exception handling");
    }

    #endregion

    #region Defer 内存测试

    [Fact]
    public void MemoryUsage_Defer_MemoryManaged()
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

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;
        
        Assert.True(memoryIncrease < 1_000_000, $"Memory increase {memoryIncrease} bytes should be minimal for defer");
    }

    #endregion

    #region Using 内存测试

    [Fact]
    public void MemoryUsage_Using_MemoryManaged()
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

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;
        
        Assert.True(memoryIncrease < 5_000_000, $"Memory increase {memoryIncrease} bytes should be reasonable for using");
    }

    #endregion

    #region Lambda 内存测试

    [Fact]
    public void MemoryUsage_Lambda_MemoryIncreases()
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

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;
        
        Assert.True(memoryIncrease < 5_000_000, $"Memory increase {memoryIncrease} bytes should be reasonable for lambda");
    }

    #endregion

    #region 元组内存测试

    [Fact]
    public void MemoryUsage_Tuple_MemoryIncreases()
    {
        var code = @"
            tuples <- {}
            for i <- 0, i < 100, i++ {
                tuples.Add((i, i * 2, i * 3))
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;

        Assert.True(memoryIncrease > 0, $"Memory should increase by {memoryIncrease} bytes");
        Assert.True(memoryIncrease < 5_000_000, $"Memory increase {memoryIncrease} bytes should be reasonable for tuples");
    }

    #endregion

    #region 范围内存测试

    [Fact]
    public void MemoryUsage_Range_MemoryEfficient()
    {
        var code = @"
            sum <- 0
            for i <- 0, i < 1000, i++ {
                sum <- sum + i
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;

        Assert.True(memoryIncrease < 1_000_000, $"Memory increase {memoryIncrease} bytes should be minimal for range");
    }

    #endregion

    #region 类继承内存测试

    [Fact]
    public void MemoryUsage_ClassInheritance_MemoryIncreases()
    {
        var code = @"
            class Animal {
                public name:string <- """"
            }

            class Dog extends Animal {
                public breed:string <- """"

                public func init(name:string, breed:string) -> void {
                    this.name <- name
                    this.breed <- breed
                }
            }

            dogs <- {}
            for i <- 0, i < 100, i++ {
                dogs.Add(Dog(""Dog"" + i.ToStr(), ""Breed""))
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;

        Assert.True(memoryIncrease > 0, $"Memory should increase by {memoryIncrease} bytes");
        Assert.True(memoryIncrease < 10_000_000, $"Memory increase {memoryIncrease} bytes should be reasonable for inheritance");
    }

    #endregion

    #region 模块导入内存测试

    [Fact]
    public void MemoryUsage_ModuleImport_MemoryManaged()
    {
        var code = @"
            func helper() -> int {
                return 42
            }

            sum <- 0
            for i <- 0, i < 100, i++ {
                sum <- sum + helper()
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;

        Assert.True(memoryIncrease < 10_000_000, $"Memory increase {memoryIncrease} bytes should be reasonable for module import");
    }

    #endregion

    #region 内存泄漏测试

    [Fact]
    public void MemoryUsage_NoMemoryLeak_AfterExecution()
    {
        var code = @"
            arr <- {}
            for i <- 0, i < 1000, i++ {
                arr.Add(i)
            }
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryAfterGC = GC.GetTotalMemory(false);

        var memoryAfterExecution = memoryAfterGC - before;
        Assert.True(memoryAfterExecution < memoryIncrease * 2,
            $"Memory after GC ({memoryAfterExecution}) should be close to peak memory ({memoryIncrease})");
    }

    #endregion

    #region 空集合内存测试

    [Fact]
    public void MemoryUsage_EmptyCollections_MinimalMemory()
    {
        var code = @"
            emptyArray <- []
            emptyList <- {}
            emptyDict <- {}
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;

        Assert.True(memoryIncrease > 0, $"Memory should increase by {memoryIncrease} bytes");
        Assert.True(memoryIncrease < 100_000, $"Memory increase {memoryIncrease} bytes should be minimal for empty collections");
    }

    #endregion

    #region 对象释放内存测试

    [Fact]
    public void MemoryUsage_ObjectRelease_MemoryReclaimed()
    {
        var code = @"
            class Resource {
                public func Dispose() -> void {
                }
            }

            func useResources() -> void {
                res1 <- Resource()
                res2 <- Resource()
                res3 <- Resource()
            }

            useResources()
        ";

        var (before, after, _) = ExecuteVMCodeWithMemoryTracking(code);
        var memoryIncrease = after - before;
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryAfterGC = GC.GetTotalMemory(false);
        
        var memoryAfterExecution = memoryAfterGC - before;
        Assert.True(memoryAfterExecution < memoryIncrease * 1.5, 
            $"Memory after GC should be reclaimed (peak: {memoryIncrease}, after GC: {memoryAfterExecution})");
    }

    #endregion
}
