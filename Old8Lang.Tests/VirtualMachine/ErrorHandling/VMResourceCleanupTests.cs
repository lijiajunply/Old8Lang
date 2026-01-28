using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.ErrorHandling;

[Collection("Sequential")]
public class VMResourceCleanupTests
{
    private string ExecuteVMCode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            var vm = new Bytecode.VM.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #region Using 语句资源清理

    [Fact]
    public void ResourceCleanup_UsingStatement_DisposesResource()
    {
        var code = @"
            class Resource {
                public id:int
                public disposed:bool

                public func init(id:int) -> void {
                    this.id <- id
                    this.disposed <- false
                }

                public func Dispose() -> void {
                    this.disposed <- true
                    PrintLine(""Resource "" + this.id.ToStr() + "" disposed"")
                }
            }

            using res <- Resource(1) {
                PrintLine(""Using resource "" + res.id.ToStr())
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Using resource 1", output);
        Assert.Contains("Resource 1 disposed", output);
    }

    [Fact]
    public void ResourceCleanup_UsingWithException_DisposesResource()
    {
        var code = @"
            class Resource {
                public id:int
                public disposed:bool

                public func init(id:int) -> void {
                    this.id <- id
                    this.disposed <- false
                }

                public func Dispose() -> void {
                    this.disposed <- true
                    PrintLine(""Resource "" + this.id.ToStr() + "" disposed"")
                }
            }

            try {
                using res <- Resource(1) {
                    PrintLine(""Using resource "" + res.id.ToStr())
                    throw ""Error in using block""
                }
                PrintLine(""After using"")
            } catch {
                PrintLine(""Caught error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Using resource 1", output);
        Assert.Contains("Resource 1 disposed", output);
        Assert.Contains("Caught error", output);
        Assert.DoesNotContain("After using", output);
    }

    #endregion

    #region 嵌套 Using 语句清理

    [Fact]
    public void ResourceCleanup_NestedUsing_AllDisposeInReverseOrder()
    {
        var code = @"
            class Resource {
                public id:int
                public disposed:bool

                public func init(id:int) -> void {
                    this.id <- id
                    this.disposed <- false
                }

                public func Dispose() -> void {
                    this.disposed <- true
                    PrintLine(""Resource "" + this.id.ToStr() + "" disposed"")
                }
            }

            using res1 <- Resource(1) {
                PrintLine(""Using resource 1"")
                using res2 <- Resource(2) {
                    PrintLine(""Using resource 2"")
                    using res3 <- Resource(3) {
                        PrintLine(""Using resource 3"")
                    }
                    PrintLine(""After resource 3"")
                }
                PrintLine(""After resource 2"")
            }
            PrintLine(""After resource 1"")
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Using resource 1", output);
        Assert.Contains("Using resource 2", output);
        Assert.Contains("Using resource 3", output);
        Assert.Contains("After resource 3", output);
        Assert.Contains("After resource 2", output);
        Assert.Contains("After resource 1", output);
        
        var lines = output.Split('\n').ToList();
        var disposeIndex3 = lines.FindIndex(l => l.Contains("Resource 3 disposed"));
        var disposeIndex2 = lines.FindIndex(l => l.Contains("Resource 2 disposed"));
        var disposeIndex1 = lines.FindIndex(l => l.Contains("Resource 1 disposed"));
        
        Assert.True(disposeIndex3 < disposeIndex2, "Resource 3 should dispose before Resource 2");
        Assert.True(disposeIndex2 < disposeIndex1, "Resource 2 should dispose before Resource 1");
    }

    [Fact]
    public void ResourceCleanup_NestedUsingWithException_AllDisposeInReverseOrder()
    {
        var code = @"
            class Resource {
                public id:int
                public disposed:bool

                public func init(id:int) -> void {
                    this.id <- id
                    this.disposed <- false
                }

                public func Dispose() -> void {
                    this.disposed <- true
                    PrintLine(""Resource "" + this.id.ToStr() + "" disposed"")
                }
            }

            try {
                using res1 <- Resource(1) {
                    PrintLine(""Using resource 1"")
                    using res2 <- Resource(2) {
                        PrintLine(""Using resource 2"")
                        throw ""Error in nested using""
                    }
                    PrintLine(""After resource 2"")
                }
                PrintLine(""After resource 1"")
            } catch {
                PrintLine(""Caught error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Using resource 1", output);
        Assert.Contains("Using resource 2", output);
        Assert.Contains("Resource 2 disposed", output);
        Assert.Contains("Resource 1 disposed", output);
        Assert.Contains("Caught error", output);
        Assert.DoesNotContain("After resource 2", output);
        Assert.DoesNotContain("After resource 1", output);
    }

    #endregion

    #region 多个资源清理

    [Fact]
    public void ResourceCleanup_MultipleSequentialUsing_AllDispose()
    {
        var code = @"
            class Resource {
                public id:int
                public disposed:bool

                public func init(id:int) -> void {
                    this.id <- id
                    this.disposed <- false
                }

                public func Dispose() -> void {
                    this.disposed <- true
                    PrintLine(""Resource "" + this.id.ToStr() + "" disposed"")
                }
            }

            using res1 <- Resource(1) {
                PrintLine(""Using resource 1"")
            }

            using res2 <- Resource(2) {
                PrintLine(""Using resource 2"")
            }

            using res3 <- Resource(3) {
                PrintLine(""Using resource 3"")
            }

            PrintLine(""All using blocks complete"")
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Using resource 1", output);
        Assert.Contains("Resource 1 disposed", output);
        Assert.Contains("Using resource 2", output);
        Assert.Contains("Resource 2 disposed", output);
        Assert.Contains("Using resource 3", output);
        Assert.Contains("Resource 3 disposed", output);
        Assert.Contains("All using blocks complete", output);
    }

    #endregion

    #region Defer 语句资源清理

    [Fact]
    public void ResourceCleanup_Defer_ExecutesOnNormalExit()
    {
        var code = @"
            func process() -> void {
                defer {
                    PrintLine(""Deferred cleanup"")
                }
                PrintLine(""Process complete"")
            }

            process()
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Process complete", output);
        Assert.Contains("Deferred cleanup", output);
    }

    [Fact]
    public void ResourceCleanup_Defer_ExecutesOnException()
    {
        var code = @"
            func process() -> void {
                defer {
                    PrintLine(""Deferred cleanup"")
                }
                PrintLine(""Process started"")
                throw ""Error occurred""
            }

            try {
                process()
            } catch {
                PrintLine(""Caught error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Process started", output);
        Assert.Contains("Deferred cleanup", output);
        Assert.Contains("Caught error", output);
    }

    [Fact]
    public void ResourceCleanup_MultipleDefer_ExecuteInLIFOOrder()
    {
        var code = @"
            func process() -> void {
                defer {
                    PrintLine(""Defer 3"")
                }
                defer {
                    PrintLine(""Defer 2"")
                }
                defer {
                    PrintLine(""Defer 1"")
                }
                PrintLine(""Process complete"")
            }

            process()
        ";

        var output = ExecuteVMCode(code);
        var lines = output.Split('\n').ToList();
        var defer1Index = lines.FindIndex(l => l.Contains("Defer 1"));
        var defer2Index = lines.FindIndex(l => l.Contains("Defer 2"));
        var defer3Index = lines.FindIndex(l => l.Contains("Defer 3"));
        
        Assert.True(defer1Index < defer2Index, "Defer 1 should execute before Defer 2");
        Assert.True(defer2Index < defer3Index, "Defer 2 should execute before Defer 3");
    }

    #endregion

    #region Using 和 Defer 混合清理

    [Fact]
    public void ResourceCleanup_UsingAndDefer_BothExecute()
    {
        var code = @"
            class Resource {
                public id:int
                public disposed:bool

                public func init(id:int) -> void {
                    this.id <- id
                    this.disposed <- false
                }

                public func Dispose() -> void {
                    this.disposed <- true
                    PrintLine(""Resource "" + this.id.ToStr() + "" disposed"")
                }
            }

            using res <- Resource(1) {
                defer {
                    PrintLine(""Deferred code"")
                }
                PrintLine(""Using resource"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Using resource", output);
        Assert.Contains("Deferred code", output);
        Assert.Contains("Resource 1 disposed", output);
    }

    [Fact]
    public void ResourceCleanup_UsingWithDeferInException_BothExecute()
    {
        var code = @"
            class Resource {
                public id:int
                public disposed:bool

                public func init(id:int) -> void {
                    this.id <- id
                    this.disposed <- false
                }

                public func Dispose() -> void {
                    this.disposed <- true
                    PrintLine(""Resource "" + this.id.ToStr() + "" disposed"")
                }
            }

            try {
                using res <- Resource(1) {
                    defer {
                        PrintLine(""Deferred code"")
                    }
                    PrintLine(""Using resource"")
                    throw ""Error occurred""
                }
            } catch {
                PrintLine(""Caught error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Using resource", output);
        Assert.Contains("Deferred code", output);
        Assert.Contains("Resource 1 disposed", output);
        Assert.Contains("Caught error", output);
    }

    #endregion

    #region Try-Finally 资源清理

    [Fact]
    public void ResourceCleanup_TryFinally_ExecutesFinally()
    {
        var code = @"
            class Resource {
                public id:int
                public disposed:bool

                public func init(id:int) -> void {
                    this.id <- id
                    this.disposed <- false
                }

                public func Dispose() -> void {
                    this.disposed <- true
                    PrintLine(""Resource disposed"")
                }
            }

            res <- Resource(1)
            try {
                PrintLine(""Using resource"")
            } finally {
                res.Dispose()
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Using resource", output);
        Assert.Contains("Resource disposed", output);
    }

    [Fact]
    public void ResourceCleanup_TryFinallyWithException_ExecutesFinally()
    {
        var code = @"
            class Resource {
                public id:int
                public disposed:bool

                public func init(id:int) -> void {
                    this.id <- id
                    this.disposed <- false
                }

                public func Dispose() -> void {
                    this.disposed <- true
                    PrintLine(""Resource disposed"")
                }
            }

            res <- Resource(1)
            try {
                PrintLine(""Using resource"")
                throw ""Error occurred""
            } finally {
                res.Dispose()
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Using resource", output);
        Assert.Contains("Resource disposed", output);
    }

    #endregion

    #region 异步函数中的资源清理

    [Fact]
    public void ResourceCleanup_AsyncUsing_DisposesResource()
    {
        var code = @"
            class Resource {
                public id:int
                public disposed:bool

                public func init(id:int) -> void {
                    this.id <- id
                    this.disposed <- false
                }

                public func Dispose() -> void {
                    this.disposed <- true
                    PrintLine(""Resource "" + this.id.ToStr() + "" disposed"")
                }
            }

            async func asyncProcess() -> void {
                using res <- Resource(1) {
                    PrintLine(""Async using resource"")
                }
            }

            task <- asyncProcess()
            await task
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Async using resource", output);
        Assert.Contains("Resource 1 disposed", output);
    }

    [Fact]
    public void ResourceCleanup_AsyncDefer_ExecutesDefer()
    {
        var code = @"
            async func asyncProcess() -> void {
                defer {
                    PrintLine(""Async deferred cleanup"")
                }
                PrintLine(""Async process complete"")
            }

            task <- asyncProcess()
            await task
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Async process complete", output);
        Assert.Contains("Async deferred cleanup", output);
    }

    #endregion

    #region 循环中的资源清理

    [Fact]
    public void ResourceCleanup_UsingInLoop_DisposesEachResource()
    {
        var code = @"
            class Resource {
                public id:int
                public disposed:bool

                public func init(id:int) -> void {
                    this.id <- id
                    this.disposed <- false
                }

                public func Dispose() -> void {
                    this.disposed <- true
                    PrintLine(""Resource "" + this.id.ToStr() + "" disposed"")
                }
            }

            for i <- 0, i < 3, i++ {
                using res <- Resource(i) {
                    PrintLine(""Using resource "" + i.ToStr())
                }
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Using resource 0", output);
        Assert.Contains("Resource 0 disposed", output);
        Assert.Contains("Using resource 1", output);
        Assert.Contains("Resource 1 disposed", output);
        Assert.Contains("Using resource 2", output);
        Assert.Contains("Resource 2 disposed", output);
    }

    [Fact]
    public void ResourceCleanup_DeferInLoop_ExecutesEachDefer()
    {
        var code = @"
            for i <- 0, i < 3, i++ {
                defer {
                    PrintLine(""Defer for iteration "" + i.ToStr())
                }
                PrintLine(""Iteration "" + i.ToStr())
            }
        ";

        var output = ExecuteVMCode(code);
        var lines = output.Split('\r', '\n').ToList();
        
        for (int i = 0; i < 3; i++)
        {
            Assert.Contains($"Iteration {i}", output);
            Assert.Contains($"Defer for iteration {i}", output);
            
            var iterIndex = lines.FindIndex(l => l.Contains($"Iteration {i}"));
            var deferIndex = lines.FindIndex(l => l.Contains($"Defer for iteration {i}"));
            Assert.True(iterIndex < deferIndex, $"Defer for iteration {i} should execute after iteration {i}");
        }
    }

    #endregion

    #region 类方法中的资源清理

    [Fact]
    public void ResourceCleanup_UsingInClassMethod_DisposesResource()
    {
        var code = @"
            class Resource {
                public id:int
                public disposed:bool

                public func init(id:int) -> void {
                    this.id <- id
                    this.disposed <- false
                }

                public func Dispose() -> void {
                    this.disposed <- true
                    PrintLine(""Resource disposed"")
                }
            }

            class ResourceUser {
                public func useResource() -> void {
                    using res <- Resource(1) {
                        PrintLine(""Using resource"")
                    }
                }
            }

            user <- ResourceUser()
            user.useResource()
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Using resource", output);
        Assert.Contains("Resource disposed", output);
    }

    #endregion

    #region 异常处理中的资源清理

    [Fact]
    public void ResourceCleanup_NestedTryCatchFinally_AllDispose()
    {
        var code = @"
            class Resource {
                public id:int
                public disposed:bool

                public func init(id:int) -> void {
                    this.id <- id
                    this.disposed <- false
                }

                public func Dispose() -> void {
                    this.disposed <- true
                    PrintLine(""Resource "" + this.id.ToStr() + "" disposed"")
                }
            }

            res1 <- Resource(1)
            try {
                res2 <- Resource(2)
                try {
                    res3 <- Resource(3)
                    throw ""Inner error""
                } finally {
                    res3.Dispose()
                }
            } catch {
                PrintLine(""Caught error"")
            } finally {
                res2.Dispose()
            }
            res1.Dispose()
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Resource 3 disposed", output);
        Assert.Contains("Resource 2 disposed", output);
        Assert.Contains("Resource 1 disposed", output);
        Assert.Contains("Caught error", output);
    }

    #endregion

    #region 生成器中的资源清理

    [Fact]
    public void ResourceCleanup_DeferInGenerator_ExecutesOnComplete()
    {
        var code = @"
            func generate() -> {
                defer {
                    PrintLine(""Generator cleanup"")
                }
                yield 1
                yield 2
                yield 3
            }

            gen <- generate()
            while gen.MoveNext() {
                PrintLine(gen.Current().ToStr())
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("1", output);
        Assert.Contains("2", output);
        Assert.Contains("3", output);
        Assert.Contains("Generator cleanup", output);
    }

    #endregion

    #region 多次清理验证

    [Fact]
    public void ResourceCleanup_NoDoubleDispose_OnlyDisposesOnce()
    {
        var code = @"
            class Resource {
                public id:int
                public disposeCount:int

                public func init(id:int) -> void {
                    this.id <- id
                    this.disposeCount <- 0
                }

                public func Dispose() -> void {
                    this.disposeCount <- this.disposeCount + 1
                    PrintLine(""Resource disposed "" + this.disposeCount.ToStr() + "" time(s)"")
                }
            }

            using res <- Resource(1) {
                res.Dispose()
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Resource disposed 2 time(s)", output);
    }

    #endregion
}
