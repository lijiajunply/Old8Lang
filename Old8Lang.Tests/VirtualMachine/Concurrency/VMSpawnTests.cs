using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 spawn（创建线程）生命周期测试
/// </summary>
/// <remarks>
/// `spawn` 只创建线程，不启动；必须调用 `Start()` 线程才会执行，之后用 `Join()` 等待结束
/// 并取回线程函数的返回值。语法文档 §5.9 已按此说明。
/// </remarks>
[Collection("Sequential")]
public class VMSpawnTests
{
    private static readonly string[] IgnorableOutput = ["Time", "Total", "Parser Build", "Bytecode", "VM Exec"];

    private static (string Output, System.Exception? Error) ExecuteVm(string code)
    {
        var interpreter = new LangInterpreter();
        var bytecodeFile = new BytecodeCompiler().Compile(interpreter.Build(code));

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            new VM(bytecodeFile).Execute();
            return (Clean(writer.ToString()), null);
        }
        catch (System.Exception ex)
        {
            return (Clean(writer.ToString()), ex);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static string Clean(string text)
    {
        var lines = text
            .Replace("\r", string.Empty)
            .Split('\n')
            .Where(l => l.Trim().Length > 0 && !IgnorableOutput.Any(k => l.Contains(k)));
        return string.Join("\n", lines).Trim();
    }

    [Fact]
    public void Spawn_StartThenJoin_ExecutesThreadFunctionAndReturnsValue()
    {
        var (output, error) = ExecuteVm("""
            func compute(x) { return x * 2 }
            t <- spawn(compute, 21)
            t.Start()
            result <- t.Join()
            PrintLine("result: " + result.ToStr())
            """);

        Assert.Null(error);
        Assert.Equal("result: 42", output);
    }

    [Fact]
    public void Spawn_StartThenJoin_ExecutesSideEffects()
    {
        var (output, error) = ExecuteVm("""
            func worker(id) { PrintLine("worker " + id.ToStr()) }
            t <- spawn(worker, 7)
            t.Start()
            t.Join()
            PrintLine("done")
            """);

        Assert.Null(error);
        Assert.Equal("worker 7\ndone", output);
    }

    [Fact]
    public void Spawn_AcceptsClosure()
    {
        var (output, error) = ExecuteVm("""
            t <- spawn(() -> { PrintLine("closure ran") })
            t.Start()
            t.Join()
            """);

        Assert.Null(error);
        Assert.Equal("closure ran", output);
    }

    [Fact]
    public void Spawn_IsAliveIsFalseUntilStart()
    {
        var (output, error) = ExecuteVm("""
            func worker() { return 1 }
            t <- spawn(worker)
            PrintLine("before: " + t.IsAlive().ToStr())
            t.Start()
            t.Join()
            PrintLine("after: " + t.IsAlive().ToStr())
            """);

        Assert.Null(error);
        Assert.Equal("before: false\nafter: false", output);
    }

    [Fact]
    public void Spawn_WithoutStart_ThreadFunctionDoesNotRun()
    {
        // 只创建不启动：线程函数不应执行
        var (output, error) = ExecuteVm("""
            func worker() { PrintLine("should not run") }
            t <- spawn(worker)
            PrintLine("main done")
            """);

        Assert.Null(error);
        Assert.Equal("main done", output);
    }

    [Fact]
    public void Spawn_JoinWithoutStart_ReportsThreadNotStarted()
    {
        var (_, error) = ExecuteVm("""
            func worker() { return 1 }
            t <- spawn(worker)
            t.Join()
            """);

        Assert.NotNull(error);
        var text = error!.ToString();
        // 报错必须指出"线程尚未启动"，而不是反射包装后的 "Exception has been thrown by
        // the target of an invocation."（那样等于丢失了真实原因）
        Assert.DoesNotContain("target of an invocation", text);
        Assert.True(text.Contains("not been started") || text.Contains("尚未启动"),
            $"报错应指出线程未启动，实际为：{text}");
    }

    [Fact]
    public void Spawn_TwoThreads_RunIndependently()
    {
        var (output, error) = ExecuteVm("""
            func worker(id) { PrintLine("worker " + id.ToStr()) }
            t1 <- spawn(worker, 1)
            t2 <- spawn(worker, 2)
            t1.Start()
            t2.Start()
            t1.Join()
            t2.Join()
            PrintLine("done")
            """);

        Assert.Null(error);
        // 两个线程各自的输出顺序由调度决定，最后一行固定为主线程的输出
        var lines = output.Split('\n');
        Assert.Equal("done", lines[^1]);
        Assert.Contains("worker 1", lines);
        Assert.Contains("worker 2", lines);
    }
}
