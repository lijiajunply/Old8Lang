using Old8Lang.Bytecode;
using Old8Lang.Bytecode.Debugger;
using Old8Lang.Bytecode.Profiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Integration;

/// <summary>
/// P2（虚拟机模式残留）修复的回归用例。
/// </summary>
/// <remarks>
/// 覆盖：数组解构赋值、异步函数显式 <c>return null</c>、错误信息不泄露 .NET 类型名、
/// <c>Task.Delay</c> 的 CancellationToken、<c>Task.Factory.StartNew</c>、
/// <c>Thread.CurrentThread</c>、静态类方法取值后调用、<c>TestRunner</c> / <c>Mock</c> /
/// <c>TaskScheduler</c> 静态类，以及反汇编 / 性能分析 / 调试器三个工具的接入点。
/// 实测报告见 <c>Reports/2026-10-24-09-45-P2虚拟机模式残留修复测试.md</c>。
/// </remarks>
[Collection("Sequential")]
public class VMP2RegressionTests
{
    private static string ExecuteVm(string code)
    {
        var (vm, bytecodeFile) = Compile(code);
        _ = bytecodeFile;

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            vm.Execute();
            return writer.ToString().Trim().Replace("\r\n", "\n");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static (Old8Lang.Bytecode.VM.VirtualMachine Vm, BytecodeFile File) Compile(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var bytecodeFile = new BytecodeCompiler().Compile(ast);
        return (new Old8Lang.Bytecode.VM.VirtualMachine(bytecodeFile), bytecodeFile);
    }

    private static Old8Exception ExecuteVmExpectingOld8Error(string code)
    {
        var (vm, _) = Compile(code);
        return Assert.ThrowsAny<Old8Exception>(() => vm.Execute());
    }

    // ===== 数组解构赋值 =====

    [Fact]
    public void ArrayDestructuring_AssignsFromListAndArray()
    {
        var output = ExecuteVm("""
            [a, b] <- {1, 2};
            [c, d] <- [3, 4]
            PrintLine(a.ToStr())
            PrintLine(b.ToStr())
            PrintLine(c.ToStr())
            PrintLine(d.ToStr())
            """);

        Assert.Equal("1\n2\n3\n4", output);
    }

    [Fact]
    public void ArrayDestructuring_IgnoresExtraElements()
    {
        var output = ExecuteVm("""
            [a, b] <- [1, 2, 3]
            PrintLine(a.ToStr())
            PrintLine(b.ToStr())
            """);

        Assert.Equal("1\n2", output);
    }

    // ===== 异步函数显式 return null =====

    [Fact]
    public void AsyncFunction_ExplicitNull_KeepsNullResult()
    {
        var output = ExecuteVm("""
            async func f() -> any {
                return null
            }
            t <- f()
            await t
            PrintLine(t.ToString())
            """);

        Assert.Equal("Task(Completed: Null)", output);
    }

    [Fact]
    public void AsyncFunction_WithoutReturn_StillHasVoidResult()
    {
        var output = ExecuteVm("""
            async func f() {
            }
            t <- f()
            await t
            PrintLine(t.ToString())
            """);

        Assert.Equal("Task(Completed: )", output);
    }

    // ===== 错误信息中的类型名 =====

    [Fact]
    public void AttributeError_UsesLanguageTypeNameForList()
    {
        var error = ExecuteVmExpectingOld8Error("""
            l <- {1, 2}
            PrintLine(l.Foo)
            """);

        Assert.Contains("List", error.Message);
        Assert.DoesNotContain("`", error.Message);
    }

    [Fact]
    public void AttributeError_UsesLanguageTypeNameForTuple()
    {
        var error = ExecuteVmExpectingOld8Error("""
            t <- (1, 2)
            PrintLine(t.Foo)
            """);

        Assert.Contains("Tuple", error.Message);
        Assert.DoesNotContain("`", error.Message);
    }

    [Fact]
    public void ArithmeticError_DoesNotLeakDotNetNames()
    {
        var error = ExecuteVmExpectingOld8Error("""
            a <- {1, 2}
            b <- {"x": 1}
            c <- a + b
            """);

        Assert.Contains("List", error.Message);
        Assert.Contains("Dictionary", error.Message);
        Assert.DoesNotContain("`", error.Message);
    }

    // ===== Task / Thread 静态类 =====

    [Fact]
    public void TaskDelay_AcceptsCancellationToken()
    {
        var output = ExecuteVm("""
            cts <- CancellationTokenSource()
            token <- cts.Token
            Task.Delay(1, token)
            PrintLine("ok")
            """);

        Assert.Equal("ok", output);
    }

    [Fact]
    public void TaskFactoryStartNew_RunsFunctionAsTask()
    {
        var output = ExecuteVm("""
            t <- Task.Factory.StartNew(() -> {
                return 40 + 2
            })
            r <- await t
            PrintLine(r.ToStr())
            """);

        Assert.Equal("42", output);
    }

    [Fact]
    public void ThreadCurrentThread_ExposesId()
    {
        var output = ExecuteVm("""
            th <- Thread.CurrentThread
            PrintLine(th.Id.ToStr())
            """);

        Assert.True(int.TryParse(output, out var id));
        Assert.True(id > 0);
    }

    [Fact]
    public void StaticMethodValue_CanBeStoredAndAwaited()
    {
        var output = ExecuteVm("""
            f <- Task.Delay
            await f(1)
            PrintLine("ok")
            """);

        Assert.Equal("ok", output);
    }

    [Fact]
    public void TaskSchedulerDefault_IsAccessible()
    {
        var output = ExecuteVm("""
            s <- TaskScheduler.Default
            PrintLine(s.ToStr())
            """);

        Assert.Equal("TaskScheduler", output);
    }

    // ===== TestRunner / Mock =====

    [Fact]
    public void TestRunner_RunTest_ReportsPassAndFailure()
    {
        var output = ExecuteVm("""
            TestRunner.Clear()
            passed <- TestRunner.RunTest("ok", () -> {
                Assert.Equal(2, 1 + 1)
            })
            failed <- TestRunner.RunTest("bad", () -> {
                Assert.Equal(3, 1 + 1)
            })
            PrintLine(passed["passed"].ToStr())
            PrintLine(failed["passed"].ToStr())
            """);

        Assert.Equal("true\nfalse", output);
    }

    [Fact]
    public void TestRunner_DescribeItRunAll_CountsRegisteredTests()
    {
        var output = ExecuteVm("""
            TestRunner.Clear()
            TestRunner.Describe("suite", () -> {
                TestRunner.It("one", () -> {
                })
                TestRunner.It("two", () -> {
                })
            })
            PrintLine(TestRunner.GetSuiteCount().ToStr())
            PrintLine(TestRunner.GetTestCount().ToStr())
            PrintLine(TestRunner.RunAll().Count().ToStr())
            """);

        Assert.Equal("1\n2\n2", output);
    }

    [Fact]
    public void Mock_CreateStubAndVerify()
    {
        var output = ExecuteVm("""
            m <- Mock.CreateMock("svc")
            Mock.VerifyNotCalled(m, "get")
            Mock.StubReturns(m, {"get": 42})
            PrintLine("ok")
            """);

        Assert.Equal("ok", output);
    }

    // ===== 工具接入点 =====

    [Fact]
    public void Disassembler_ProducesInstructionsForFunctions()
    {
        var (_, bytecodeFile) = Compile("""
            x <- 1 + 2
            PrintLine(x.ToStr())
            """);

        var text = new Disassembler(bytecodeFile.ConstantPool, bytecodeFile.DebugInfo)
            .DisassembleProgram(bytecodeFile.Functions);

        Assert.Contains("Function: <main>", text);
        Assert.Contains("LoadConst", text);
    }

    [Fact]
    public void Profiler_RecordsFunctionCallsAndInstructionCounts()
    {
        var (vm, _) = Compile("""
            func add(a: int, b: int) -> int {
                return a + b
            }
            PrintLine(add(2, 3).ToStr())
            """);

        vm.Profiler = new VMProfiler();

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            vm.Execute();
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var report = vm.Profiler.GenerateReport();
        Assert.Contains("add", report);
        Assert.Contains("LoadLocal", report);
    }

    [Fact]
    public void Debugger_PausesAtLineBreakpointAndCanContinue()
    {
        var code = """
            func add(a: int, b: int) -> int {
                c <- a + b
                return c
            }
            x <- add(2, 3)
            PrintLine(x.ToStr())
            """;

        var (vm, _) = Compile(code);
        var debugger = new VMDebugger();
        debugger.Start();
        vm.Debugger = debugger;

        // 第 3 行是 `return c`
        Assert.NotEmpty(vm.AddBreakpointsAtLine(3));

        var pausedAt = new List<string>();
        vm.DebugPaused = (_, frame, _) =>
        {
            pausedAt.Add(frame.Function.Name);
            debugger.Continue();
        };

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            vm.Execute();
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        // 只应在 `add` 里命中一次；没有按函数限定的话会在别的函数的同名 offset 上误命中
        Assert.Equal(new[] { "add" }, pausedAt);
    }
}
