using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.ErrorHandling;

/// <summary>
/// 虚拟机求值栈一致性测试：无返回值的调用参与表达式
/// </summary>
/// <remarks>
/// 调用约定要求"一次调用在求值栈上恰好留下一个值"。此前无返回值的函数与原生函数都不压入任何值，
/// 于是把这类调用当作值使用时（`r <- f()`、`print(f())`）消费方会读到空栈，
/// 报出与真实原因无关的 "Stack empty"。
/// 与之相对，语句位置的调用（结果被丢弃）必须补 Pop，否则每次调用都会在栈上多留一个值。
/// </remarks>
[Collection("Sequential")]
public class VMVoidCallStackConsistencyTests
{
    private static string ExecuteVm(string code)
    {
        var interpreter = new LangInterpreter();
        var bytecodeFile = new BytecodeCompiler().Compile(interpreter.Build(code));

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            new VM(bytecodeFile).Execute();
            return writer.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static string ExecuteInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            ast.Run(interpreter.Manager);
            return writer.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void VoidFunction_ResultAssigned_DoesNotCorruptStack()
    {
        var output = ExecuteVm("""
            func nothing() { }
            r <- nothing()
            print("after")
            """);

        Assert.Equal("after", output.Trim());
    }

    [Fact]
    public void VoidFunction_ResultIsVoidValue_NotNull()
    {
        // 与解释器保持一致：无返回值的调用产生 void 值，而不是 null
        const string code = """
            func nothing() { }
            r <- nothing()
            print((r == null).ToStr())
            """;

        Assert.Equal(ExecuteInterpreter(code).Trim(), ExecuteVm(code).Trim());
        Assert.Equal("false", ExecuteVm(code).Trim());
    }

    [Fact]
    public void VoidFunction_ResultPrinted_ProducesNoOutput()
    {
        const string code = """
            func nothing() { }
            print(nothing())
            print("after")
            """;

        Assert.Equal(ExecuteInterpreter(code).Trim(), ExecuteVm(code).Trim());
    }

    [Fact]
    public void VoidFunction_CalledManyTimesAsStatement_KeepsStackBalanced()
    {
        // 若语句位置的调用没有补 Pop，栈会随每次调用增长，随后的取用就会错位
        var output = ExecuteVm("""
            func nothing() { }
            i <- 0
            while i < 200 {
                nothing()
                i <- i + 1
            }
            print(i)
            """);

        Assert.Equal("200", output.Trim());
    }

    [Fact]
    public void VoidFunction_EarlyReturnAndFallthrough_KeepsStackBalanced()
    {
        // 覆盖两条返回路径交替出现：显式 return 与函数体执行到末尾。
        // 帧是池化复用的，返回值标记必须在复用时清零，否则第二次调用会拿到上次的残留状态
        const string code = """
            func early(c) {
                if c { return 1 }
                PrintLine("fallthrough")
            }
            print(early(true))
            print(early(false))
            print(early(true))
            """;

        Assert.Equal(ExecuteInterpreter(code).Trim(), ExecuteVm(code).Trim());
    }

    [Fact]
    public void VoidFunction_FallthroughAfterEarlierReturn_DoesNotReuseStaleValue()
    {
        // 返回值不能从上一次调用残留下来：这里第一次调用有 return，第二次走末尾无 return
        const string code = """
            func f(c) {
                if c { return 42 }
            }
            print(f(true))
            r <- f(false)
            print((r == null).ToStr())
            print("end")
            """;

        Assert.Equal(ExecuteInterpreter(code).Trim(), ExecuteVm(code).Trim());
        // print 不换行，故输出连成一行
        Assert.Equal("42falseend", ExecuteVm(code).Trim());
    }

    [Fact]
    public void VoidFunction_UsedAsArgument_IsPassedAsVoidValue()
    {
        var output = ExecuteVm("""
            func nothing() { }
            func show(v) {
                print("got:" + (v == null).ToStr())
            }
            show(nothing())
            """);

        Assert.Equal("got:false", output.Trim());
    }

    [Fact]
    public void NativeVoidFunction_ResultAssigned_DoesNotCorruptStack()
    {
        var output = ExecuteVm("""
            r <- Sleep(1)
            print("after")
            """);

        Assert.Equal("after", output.Trim());
    }

    [Fact]
    public void Constructor_ResultIsTheInstance_NotTheConstructorsReturnValue()
    {
        // 构造函数自身没有返回值，但实例化表达式的值必须是实例本身
        var output = ExecuteVm("""
            class Point {
                x <- 0
                public init(v) { x <- v }
            }
            p <- Point(7)
            print(p.x)
            print(Point(9).x)
            """);

        Assert.Equal("79", output.Trim());
    }

    [Fact]
    public void Constructor_CalledRepeatedly_KeepsStackBalanced()
    {
        var output = ExecuteVm("""
            class Counter {
                v <- 0
                public init(n) { v <- n }
            }
            total <- 0
            i <- 0
            while i < 100 {
                c <- Counter(i)
                total <- total + c.v
                i <- i + 1
            }
            print(total)
            """);

        Assert.Equal("4950", output.Trim());
    }

    [Fact]
    public void VoidMethod_ResultUsedInExpression_DoesNotCorruptStack()
    {
        var output = ExecuteVm("""
            class Box {
                v <- 0
                public set(n) { v <- n }
                public get() { return v }
            }
            b <- Box()
            r <- b.set(5)
            print(b.get())
            print("after")
            """);

        Assert.Equal("5after", output.Trim());
    }
}
