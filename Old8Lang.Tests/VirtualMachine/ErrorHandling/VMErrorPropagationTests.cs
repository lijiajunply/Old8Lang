using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.ErrorHandling;

[Collection("Sequential")]
public class VMErrorPropagationTests
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

    #region 函数调用中的异常传播

    [Fact]
    public void ErrorPropagation_FunctionCall_PropagatesException()
    {
        var code = @"
            func throwError() -> void {
                throw ""Error from function""
            }

            try {
                throwError()
                PrintLine(""No error"")
            } catch {
                PrintLine(""Caught function error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("Caught function error", output);
    }

    [Fact]
    public void ErrorPropagation_NestedFunctionCall_PropagatesException()
    {
        var code = @"
            func inner() -> void {
                throw ""Error from inner""
            }

            func outer() -> void {
                inner()
            }

            try {
                outer()
                PrintLine(""No error"")
            } catch {
                PrintLine(""Caught nested error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("Caught nested error", output);
    }

    [Fact]
    public void ErrorPropagation_FunctionCallWithReturn_ExceptionPropagates()
    {
        var code = @"
            func mightThrow(shouldThrow:bool) -> int {
                if shouldThrow {
                    throw ""Error occurred""
                }
                return 42
            }

            try {
                result <- mightThrow(true)
                PrintLine(""No error: "" + result.ToStr())
            } catch {
                PrintLine(""Caught error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("Caught error", output);
    }

    #endregion

    #region 异步函数中的异常传播

    [Fact]
    public void ErrorPropagation_AsyncFunction_PropagatesException()
    {
        var code = @"
            async func asyncError() -> void {
                throw ""Async error""
            }

            try {
                task <- asyncError()
                await task
                PrintLine(""No error"")
            } catch {
                PrintLine(""Caught async error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("Caught async error", output);
    }

    [Fact]
    public void ErrorPropagation_AsyncFunctionInTry_CatchesException()
    {
        var code = @"
            async func asyncWork() -> void {
                throw ""Async work error""
            }

            try {
                task <- asyncWork()
                await task
            } catch (e) {
                PrintLine(""Caught: "" + e.Message)
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("Caught: Async work error", output);
    }

    [Fact]
    public void ErrorPropagation_NestedAsyncCalls_PropagatesException()
    {
        var code = @"
            async func innerAsync() -> void {
                throw ""Inner async error""
            }

            async func outerAsync() -> void {
                task <- innerAsync()
                await task
            }

            try {
                task <- outerAsync()
                await task
                PrintLine(""No error"")
            } catch {
                PrintLine(""Caught nested async error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("Caught nested async error", output);
    }

    #endregion

    #region 嵌套作用域中的异常传播

    [Fact]
    public void ErrorPropagation_NestedScope_PropagatesToOuterCatch()
    {
        var code = @"
            try {
                try {
                    throw ""Inner error""
                } catch {
                    PrintLine(""Inner catch"")
                    throw ""Re-throw""
                }
                PrintLine(""After inner catch"")
            } catch {
                PrintLine(""Outer catch"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Inner catch", output);
        Assert.Contains("Outer catch", output);
    }

    [Fact]
    public void ErrorPropagation_MultipleNestedScopes_PropagatesCorrectly()
    {
        var code = @"
            try {
                try {
                    try {
                        throw ""Deepest error""
                    } catch {
                        PrintLine(""Level 1 catch"")
                        throw ""Level 1 re-throw""
                    }
                } catch {
                    PrintLine(""Level 2 catch"")
                }
                PrintLine(""After level 2"")
            } catch {
                PrintLine(""Level 3 catch"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Level 1 catch", output);
        Assert.Contains("Level 2 catch", output);
        Assert.DoesNotContain("Level 3 catch", output);
    }

    #endregion

    #region Try-Catch-Finally 中的异常传播

    [Fact]
    public void ErrorPropagation_TryCatchFinally_FinallyExecutes()
    {
        var code = @"
            try {
                throw ""Error""
            } catch {
                PrintLine(""Catch block"")
            } finally {
                PrintLine(""Finally block"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Catch block", output);
        Assert.Contains("Finally block", output);
    }

    [Fact]
    public void ErrorPropagation_TryFinally_FinallyExecutes()
    {
        var code = @"
            try {
                try {
                    throw ""Error""
                } finally {
                    PrintLine(""Finally block"")
                }
            } catch {
                // 外层捕获异常，防止异常传播到测试框架
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("Finally block", output);
    }

    [Fact]
    public void ErrorPropagation_FinallyReThrow_ExceptionPropagates()
    {
        var code = @"
            try {
                try {
                    throw ""Original error""
                } catch {
                    PrintLine(""Catch block"")
                    throw ""123""
                } finally {
                    PrintLine(""Finally block"")
                }
            } catch {
                // 外层捕获重新抛出的异常
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Catch block", output);
        Assert.Contains("Finally block", output);
    }

    [Fact]
    public void ErrorPropagation_FinallyWithNewException_NewExceptionPropagates()
    {
        var code = @"
            try {
                try {
                    throw ""Original error""
                } catch {
                    PrintLine(""Catch block"")
                    throw ""New error""
                } finally {
                    PrintLine(""Finally block"")
                }
            } catch {
                // 外层捕获新异常
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Catch block", output);
        Assert.Contains("Finally block", output);
    }

    #endregion

    #region 循环中的异常传播

    [Fact]
    public void ErrorPropagation_ForLoop_ExceptionBreaksLoop()
    {
        var code = @"
            try {
                for i <- 0, i < 10, i++ {
                    if i == 5 {
                        throw ""Loop error""
                    }
                    PrintLine(i.ToStr())
                }
                PrintLine(""After loop"")
            } catch {
                PrintLine(""Caught loop error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Caught loop error", output);
        Assert.DoesNotContain("After loop", output);
    }

    [Fact]
    public void ErrorPropagation_WhileLoop_ExceptionBreaksLoop()
    {
        var code = @"
            count <- 0
            try {
                while true {
                    if count == 5 {
                        throw ""While loop error""
                    }
                    PrintLine(count.ToStr())
                    count <- count + 1
                }
                PrintLine(""After while loop"")
            } catch {
                PrintLine(""Caught while error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Caught while error", output);
        Assert.DoesNotContain("After while loop", output);
    }

    [Fact]
    public void ErrorPropagation_ForInLoop_ExceptionBreaksLoop()
    {
        var code = @"
            try {
                for x in [1, 2, 3, 4, 5] {
                    if x == 3 {
                        throw ""For-in loop error""
                    }
                    PrintLine(x.ToStr())
                }
                PrintLine(""After for-in loop"")
            } catch {
                PrintLine(""Caught for-in error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Caught for-in error", output);
        Assert.DoesNotContain("After for-in loop", output);
    }

    #endregion

    #region 类方法中的异常传播

    [Fact]
    public void ErrorPropagation_ClassMethod_PropagatesException()
    {
        var code = @"
            class Calculator {
                public func divide(a:int, b:int) -> int {
                    if b == 0 {
                        throw ""Division by zero""
                    }
                    return a / b
                }
            }

            calc <- Calculator()
            try {
                result <- calc.divide(10, 0)
                PrintLine(""No error"")
            } catch {
                PrintLine(""Caught method error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("Caught method error", output);
    }

    [Fact]
    public void ErrorPropagation_Constructor_PropagatesException()
    {
        var code = @"
            class ValidatedBox {
                public value:int

                public func init(value:int) -> void {
                    if value < 0 {
                        throw ""Value must be non-negative""
                    }
                    this.value <- value
                }
            }

            try {
                box <- ValidatedBox(-1)
                PrintLine(""No error"")
            } catch {
                PrintLine(""Caught constructor error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("Caught constructor error", output);
    }

    #endregion

    #region Lambda 中的异常传播

    [Fact]
    public void ErrorPropagation_Lambda_PropagatesException()
    {
        var code = @"
            lambda <- () -> {
                throw ""Lambda error""
            }

            try {
                lambda()
                PrintLine(""No error"")
            } catch {
                PrintLine(""Caught lambda error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("Caught lambda error", output);
    }

    [Fact]
    public void ErrorPropagation_LambdaInFunctionCall_PropagatesException()
    {
        var code = @"
            func execute(callback:function) -> void {
                callback()
            }

            func throwCallback() -> void {
                throw ""Callback error""
            }

            try {
                execute(throwCallback)
                PrintLine(""No error"")
            } catch {
                PrintLine(""Caught callback error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Equal("Caught callback error", output);
    }

    #endregion

    #region 生成器中的异常传播

    [Fact]
    public void ErrorPropagation_Generator_PropagatesException()
    {
        var code = @"
            func generate() -> {
                yield 1
                yield 2
                throw ""Generator error""
                yield 3
            }

            gen <- generate()
            try {
                while gen.MoveNext() {
                    PrintLine(gen.Current().ToStr())
                }
                PrintLine(""After generator"")
            } catch {
                PrintLine(""Caught generator error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("1", output);
        Assert.Contains("2", output);
        Assert.Contains("Caught generator error", output);
    }

    [Fact]
    public void ErrorPropagation_AsyncGenerator_PropagatesException()
    {
        var code = @"
            async func asyncGenerate() -> {
                yield 1
                yield 2
                throw ""Async generator error""
                yield 3
            }

            gen <- asyncGenerate()
            try {
                while gen.MoveNext() {
                    PrintLine(gen.Current().ToStr())
                }
                PrintLine(""After async generator"")
            } catch {
                PrintLine(""Caught async generator error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("1", output);
        Assert.Contains("2", output);
        Assert.Contains("Caught async generator error", output);
    }

    #endregion

    #region Defer 语句中的异常传播

    [Fact]
    public void ErrorPropagation_Defer_ExecutesOnException()
    {
        var code = @"
            func process() -> void {
                defer {
                    PrintLine(""Deferred code"")
                }
                throw ""Function error""
            }

            try {
                process()
                PrintLine(""No error"")
            } catch {
                PrintLine(""Caught error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Deferred code", output);
        Assert.Contains("Caught error", output);
    }

    [Fact]
    public void ErrorPropagation_MultipleDefer_AllExecuteOnException()
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
                throw ""Function error""
            }

            try {
                process()
                PrintLine(""No error"")
            } catch {
                PrintLine(""Caught error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Defer 1", output);
        Assert.Contains("Defer 2", output);
        Assert.Contains("Defer 3", output);
        Assert.Contains("Caught error", output);
    }

    #endregion

    #region Using 语句中的异常传播

    [Fact]
    public void ErrorPropagation_Using_DisposesOnException()
    {
        var code = @"
            class Resource {
                public id:int
                public func init(id:int) -> void {
                    this.id <- id
                }
                public func Dispose() -> void {
                    PrintLine(""Resource "" + this.id.ToStr() + "" disposed"")
                }
            }

            try {
                using res <- Resource(1) {
                    throw ""Error in using block""
                }
                PrintLine(""After using"")
            } catch {
                PrintLine(""Caught using error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Resource 1 disposed", output);
        Assert.Contains("Caught using error", output);
        Assert.DoesNotContain("After using", output);
    }

    [Fact]
    public void ErrorPropagation_NestedUsing_AllDisposeOnException()
    {
        var code = @"
            class Resource {
                public id:int
                public func init(id:int) -> void {
                    this.id <- id
                }
                public func Dispose() -> void {
                    PrintLine(""Resource "" + this.id.ToStr() + "" disposed"")
                }
            }

            try {
                using res1 <- Resource(1) {
                    using res2 <- Resource(2) {
                        throw ""Error in nested using""
                    }
                    PrintLine(""After inner using"")
                }
                PrintLine(""After outer using"")
            } catch {
                PrintLine(""Caught nested using error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Resource 2 disposed", output);
        Assert.Contains("Resource 1 disposed", output);
        Assert.Contains("Caught nested using error", output);
        Assert.DoesNotContain("After inner using", output);
        Assert.DoesNotContain("After outer using", output);
    }

    #endregion

    #region Select 语句中的异常传播

    [Fact]
    public void ErrorPropagation_SelectCase_PropagatesException()
    {
        var code = @"
            ch <- ChannelCreate()
            ChannelSend(ch, 1)

            try {
                select {
                    case val from ch -> {
                        throw ""Select case error""
                    }
                    default -> {
                        PrintLine(""Default case"")
                    }
                }
                PrintLine(""After select"")
            } catch {
                PrintLine(""Caught select error"")
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Caught select error", output);
        Assert.DoesNotContain("After select", output);
    }

    #endregion

    #region 异常链测试

    [Fact]
    public void ErrorPropagation_ExceptionChain_PreservesOriginalError()
    {
        var code = @"
            func level3() -> void {
                throw ""Level 3 error""
            }

            func level2() -> void {
                try {
                    level3()
                } catch (e) {
                    throw ""Level 2: "" + e.ToStr()
                }
            }

            func level1() -> void {
                try {
                    level2()
                } catch (e) {
                    throw ""Level 1: "" + e.ToStr()
                }
            }

            try {
                level1()
            } catch (e) {
                PrintLine(e.ToStr())
            }
        ";

        var output = ExecuteVMCode(code);
        Assert.Contains("Level 1: Level 2: Level 3 error", output);
    }

    #endregion
}
