using Old8Lang.Bytecode;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.ErrorHandling;

/// <summary>
/// 虚拟机模式下不支持特性的报错测试
/// </summary>
/// <remarks>
/// 这些特性在文档支持矩阵中一度被标为虚拟机可用，实际却是空实现。
/// 空实现（VisitXxx 直接返回 null）不产出任何字节码，栈随之失衡，
/// 最终在无关指令处报出 "Stack empty" 或 "名称 'X' 未定义"，
/// 报错原因和位置都与真实问题无关。本组用例确保报出的是真实原因。
/// </remarks>
[Collection("Sequential")]
public class VMUnsupportedFeatureTests
{
    private static BytecodeFile Compile(string code)
    {
        var interpreter = new LangInterpreter();
        return new BytecodeCompiler().Compile(interpreter.Build(code));
    }

    [Fact]
    public void TaskApi_BareReference_ReportsVmUnsupported()
    {
        var exception = Assert.Throws<VmUnsupportedError>(() =>
        {
            var vm = new VM(Compile("""
                func work() {
                    value <- Task.Delay
                }
                work()
                """));
            vm.Execute();
        });

        Assert.Contains("Task", exception.Message);
        Assert.Equal(VmUnsupportedError.ErrorCode, ((Old8Exception)exception).ErrorCode);
        // 位置必须指向真实出错的那一行
        Assert.Equal(2, exception.Position.Line);
    }

    [Fact]
    public void AssertApi_UnsupportedMethod_ReportsVmUnsupported()
    {
        var exception = Assert.Throws<VmUnsupportedError>(() =>
        {
            var vm = new VM(Compile("Assert.Bogus(1, 1)"));
            vm.Execute();
        });

        Assert.Contains("Assert.Bogus", exception.Message);
    }

    [Fact]
    public void ThreadApi_CurrentThread_ReportsVmUnsupported()
    {
        var exception = Assert.Throws<VmUnsupportedError>(() =>
        {
            var vm = new VM(Compile("value <- Thread.CurrentThread"));
            vm.Execute();
        });

        Assert.Contains("Thread", exception.Message);
    }

    [Fact]
    public void StaticClassMethod_NamedArgument_ReportsVmUnsupported()
    {
        var exception = Assert.Throws<VmUnsupportedError>(() =>
        {
            var vm = new VM(Compile("Task.Delay(milliseconds: 1)"));
            vm.Execute();
        });

        Assert.Contains("命名参数", exception.Message);
    }

    [Fact]
    public void TaskSchedulerApi_ReportsVmUnsupported()
    {
        var exception = Assert.Throws<VmUnsupportedError>(() =>
        {
            var vm = new VM(Compile("value <- TaskScheduler"));
            vm.Execute();
        });

        Assert.Contains("TaskScheduler", exception.Message);
    }

    [Fact]
    public void Closure_AssigningOuterLocal_ReportsVmUnsupported()
    {
        // 闭包按值快照捕获，写回外层局部变量无法支持；
        // 此前报的是 "名称 'c' 未定义"，与真实原因无关
        var exception = Assert.Throws<VmUnsupportedError>(() =>
        {
            var vm = new VM(Compile("""
                func make() {
                    c <- 0
                    f <- () -> {
                        c <- c + 1
                        return c
                    }
                }
                make()
                """));
            vm.Execute();
        });

        Assert.Contains("闭包", exception.Message);
        Assert.Contains("c", exception.Message);
    }

    [Fact]
    public void Closure_ReadOnlyCapture_IsStillSupported()
    {
        // 只读捕获不受影响，必须继续可用
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build("""
            func makeAdder(n) {
                return (x) -> { return x + n }
            }
            print(makeAdder(5)(3))
            """);

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            new VM(new BytecodeCompiler().Compile(ast)).Execute();
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.Equal("8", writer.ToString().Trim());
    }

    [Fact]
    public void Closure_DeclaringNewLocal_IsStillSupported()
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build("""
            func make() {
                return () -> {
                    local <- 7
                    return local * 2
                }
            }
            print(make()())
            """);

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            new VM(new BytecodeCompiler().Compile(ast)).Execute();
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.Equal("14", writer.ToString().Trim());
    }

    [Fact]
    public void Closure_AssigningGlobal_IsStillSupported()
    {
        // 全局变量由闭包与外层共享同一张全局表，写回本来就生效，不应被拦截
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build("""
            counter <- 0
            bump <- () -> {
                counter <- counter + 1
            }
            bump()
            bump()
            print(counter)
            """);

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(originalOut);

        try
        {
            Console.SetOut(writer);
            new VM(new BytecodeCompiler().Compile(ast)).Execute();
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.Equal("2", writer.ToString().Trim());
    }
}
