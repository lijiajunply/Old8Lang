using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Async;

/// <summary>
/// 虚拟机下的 Task 静态 API 测试
/// </summary>
/// <remarks>
/// 解释器把 <c>Task</c> 注册为全局对象，静态方法分发由 <c>TaskClassLangValue.Dot</c> 完成；
/// 虚拟机改为在编译期把 <c>Task.方法(...)</c> 改写成带限定名的原生调用，运行期由
/// <c>VirtualMachine</c> 分发。这些用例锁住该机制的行为。
/// </remarks>
[Collection("Sequential")]
public class VMTaskStaticApiTests
{
    private static BytecodeFile Compile(string code) => CompileHelper.CompileToBytecode(code);

    private static string ExecuteVMCode(string code)
    {
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            new VM(Compile(code)).Execute();
            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static int ToInt(object? value) => value switch
    {
        int intValue => intValue,
        long longValue => (int)longValue,
        IntLangValue langValue => langValue.Value,
        _ => throw new InvalidOperationException($"期望整数结果，实际为 {value?.GetType().Name ?? "null"}")
    };

    private static string ToText(object? value) => value switch
    {
        string text => text,
        StringLangValue langValue => langValue.Value,
        _ => throw new InvalidOperationException($"期望字符串结果，实际为 {value?.GetType().Name ?? "null"}")
    };

    [Fact]
    public void TaskDelay_Await_CompletesAndReturnsVoid()
    {
        var bytecodeFile = Compile("""
            async func work() -> int {
                await Task.Delay(1)
                return 7
            }
            result <- await work()
            """);

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(7, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void TaskFromResult_Await_ReturnsValue()
    {
        var bytecodeFile = Compile("""
            result <- await Task.FromResult(41 + 1)
            """);

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(42, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void TaskRun_ExecutesLambda_ReturnsValue()
    {
        var bytecodeFile = Compile("""
            result <- await Task.Run(() -> 21 * 2)
            """);

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(42, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void TaskStartNew_ExecutesLambda_ReturnsValue()
    {
        var bytecodeFile = Compile("""
            result <- await Task.StartNew(() -> 100)
            """);

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(100, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void TaskRun_LambdaCapturesOuterVariable()
    {
        // worker 虚拟机共享全局表，闭包需要自带捕获环境
        var bytecodeFile = Compile("""
            func makeAdder(offset) {
                return await Task.Run(() -> offset + 1)
            }
            result <- makeAdder(41)
            """);

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(42, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void TaskWait_BlocksAndReturnsResult()
    {
        var bytecodeFile = Compile("""
            task <- Task.Run(() -> 42)
            result <- task.Wait()
            """);

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(42, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void TaskWhenAll_Await_ReturnsIndexableIterableList()
    {
        // TaskLangValue.WhenAll 的结果是 ListLangValue：虚拟机原本的索引/迭代指令只认原生容器，
        // 这条用例专门锁住 ILangList 分支。
        var code = """
            tasks <- {Task.FromResult(10), Task.FromResult(20), Task.FromResult(30)}
            results <- await Task.WhenAll(tasks)
            first <- results[0]
            last <- results[-1]
            total <- 0
            for item in results {
                total <- total + item
            }
            """;

        var vm = new VM(Compile(code));
        vm.Execute();

        Assert.Equal(10, ToInt(vm.GetGlobalVariable("first")));
        Assert.Equal(30, ToInt(vm.GetGlobalVariable("last")));
        Assert.Equal(60, ToInt(vm.GetGlobalVariable("total")));
    }

    [Fact]
    public void TaskWhenAny_Await_ReturnsFirstCompletedResult()
    {
        // 用一个远未到期的 Delay 与一个"创建即已完成"的 Task 配对，
        // 结果与调度顺序无关，不必依赖墙钟。
        var bytecodeFile = Compile("""
            tasks <- {Task.Delay(5000), Task.FromResult("fast")}
            result <- await Task.WhenAny(tasks)
            """);

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal("fast", ToText(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void TaskFromException_Await_IsCatchable()
    {
        var bytecodeFile = Compile("""
            result <- ""
            try {
                await Task.FromException("boom")
            } catch (e) {
                result <- "caught: " + e.Message
            }
            """);

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal("caught: boom", ToText(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void TaskRun_ThrowingLambda_AwaitIsCatchable()
    {
        var bytecodeFile = Compile("""
            result <- ""
            try {
                await Task.Run(() -> {
                    throw "task failed"
                })
            } catch (e) {
                result <- "caught"
            }
            """);

        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal("caught", ToText(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void TaskDelay_WithSecondArgument_ReportsUnsupported()
    {
        // 虚拟机下无法构造 CancellationToken，第二个参数只能明确拒绝
        var bytecodeFile = Compile("await Task.Delay(1, 2)");
        var vm = new VM(bytecodeFile);

        Assert.Throws<Old8Lang.Error.VmUnsupportedError>(() => vm.Execute());
    }

    [Fact]
    public void TaskStaticApi_InsideClosure_Works()
    {
        var code = """
            doubler <- () -> (await Task.FromResult(21)) * 2
            result <- doubler()
            """;

        var vm = new VM(Compile(code));
        vm.Execute();

        Assert.Equal(42, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void ShadowedTaskVariable_UsesUserDefinition()
    {
        // 用户自己定义了与静态类同名的全局变量时，必须按普通成员访问处理
        var code = """
            class MyTask {
                func Delay(x) {
                    return x + 1
                }
            }
            Task <- MyTask()
            result <- Task.Delay(1)
            """;

        var vm = new VM(Compile(code));
        vm.Execute();

        Assert.Equal(2, ToInt(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void TaskDelay_OutputIsOrdered()
    {
        var output = ExecuteVMCode("""
            async func work() {
                await Task.Delay(1)
                PrintLine("after delay")
            }
            work().Wait()
            PrintLine("done")
            """);

        Assert.Equal("after delay\ndone", output);
    }
}
