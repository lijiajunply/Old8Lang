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

    /// <summary>
    /// 执行代码并捕获控制台输出
    /// </summary>
    private static string RunAndCaptureOutput(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            new VM(new BytecodeCompiler().Compile(ast)).Execute();
            return writer.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Closure_AssigningOuterLocal_WritesBackToEnclosingScope()
    {
        // 闭包写回外层局部变量由共享单元（UpValueCell）支持：闭包与外层帧共用同一个盒子，
        // 因此闭包内的赋值对外层可见。此前是按值快照捕获，这条只能报 VM_UNSUPPORTED_ERROR。
        var output = RunAndCaptureOutput("""
            func make() {
                c <- 0
                bump <- () -> {
                    c <- c + 1
                    return c
                }
                print(bump())
                print(bump())
                print(c)
            }
            make()
            """);

        Assert.Equal("122", output);
    }

    [Fact]
    public void Closure_AssigningOuterLocal_SharedBetweenSiblingClosures()
    {
        // 两个闭包捕获同一个外层局部变量时共享同一个盒子，能看到对方写入的值
        var output = RunAndCaptureOutput("""
            func make() {
                c <- 0
                inc <- () -> { c <- c + 1 }
                read <- () -> c
                inc()
                inc()
                print(read())
            }
            make()
            """);

        Assert.Equal("2", output);
    }

    [Fact]
    public void Closure_OuterAssignmentAfterCreation_IsVisibleToClosure()
    {
        // 闭包创建后再由外层赋值，闭包读到的必须是最新值（快照捕获会读到旧值）
        var output = RunAndCaptureOutput("""
            func make() {
                c <- 0
                read <- () -> c
                c <- 42
                print(read())
            }
            make()
            """);

        Assert.Equal("42", output);
    }

    [Fact]
    public void NestedNamedFunction_CapturingOuterLocal_ReadsCurrentValue()
    {
        // 嵌套具名函数此前完全不做捕获分析：外层局部变量被当成全局名查找，运行时报
        // 「名称 'base' 未定义」，且位置退化成 0:0。
        var output = RunAndCaptureOutput("""
            func outer() {
                base <- 100
                func inner() -> int {
                    return base + 1
                }
                print(inner())
            }
            outer()
            """);

        Assert.Equal("101", output);
    }

    [Fact]
    public void NestedNamedFunction_AssigningOuterLocal_WritesBack()
    {
        var output = RunAndCaptureOutput("""
            func outer() {
                total <- 0
                func bump() -> int {
                    total <- total + 10
                    return total
                }
                bump()
                bump()
                print(total)
            }
            outer()
            """);

        Assert.Equal("20", output);
    }

    [Fact]
    public void Closure_ReadOnlyCapture_IsStillSupported()
    {
        // 只读捕获不受影响，必须继续可用
        var output = RunAndCaptureOutput("""
            func makeAdder(n) {
                return (x) -> { return x + n }
            }
            print(makeAdder(5)(3))
            """);

        Assert.Equal("8", output);
    }

    [Fact]
    public void Closure_DeclaringNewLocal_IsStillSupported()
    {
        // 闭包内声明自己的新局部变量不应被当成外层变量捕获
        var output = RunAndCaptureOutput("""
            func make() {
                return () -> {
                    local <- 7
                    return local * 2
                }
            }
            print(make()())
            """);

        Assert.Equal("14", output);
    }

    [Fact]
    public void Closure_AssigningGlobal_IsStillSupported()
    {
        // 全局变量由闭包与外层共享同一张全局表，写回本来就生效
        var output = RunAndCaptureOutput("""
            counter <- 0
            bump <- () -> {
                counter <- counter + 1
            }
            bump()
            bump()
            print(counter)
            """);

        Assert.Equal("2", output);
    }
}
