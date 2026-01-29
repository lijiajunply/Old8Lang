using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机模式下的多线程测试
/// </summary>
public class VMThreadTests
{
    /// <summary>
    /// 辅助方法：从全局变量中获取整数值
    /// </summary>
    private static int GetIntValue(object? value)
    {
        return value switch
        {
            int i => i,
            long l => (int)l,
            IntLangValue ilv => ilv.Value,
            _ => Convert.ToInt32(value)
        };
    }

    [Fact]
    public void TestSimpleThreadCreationAndJoin()
    {
        // 测试简单的线程创建和等待
        var code = @"
result <- 0

func worker() -> int {
    return 42
}

thread <- Spawn(worker)
thread.Start()
result <- thread.Join()
";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);
        var vm = new Bytecode.VM.VirtualMachine(bytecodeFile);

        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        Assert.Equal(42, GetIntValue(result));
    }

    [Fact]
    public void TestThreadWithParameters()
    {
        // 测试带参数的线程
        var code = @"
result <- 0

func add(a:int, b:int) -> int {
    return a + b
}

thread <- Spawn(add, 10, 20)
thread.Start()
result <- thread.Join()
";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);
        var vm = new Bytecode.VM.VirtualMachine(bytecodeFile);

        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        Assert.Equal(30, GetIntValue(result));
    }

    [Fact]
    public void TestMultipleThreads()
    {
        // 测试多个线程
        var code = @"
result <- 0

func worker(id:int) -> int {
    return id * 2
}

t1 <- Spawn(worker, 1)
t2 <- Spawn(worker, 2)
t3 <- Spawn(worker, 3)

t1.Start()
t2.Start()
t3.Start()

r1 <- t1.Join()
r2 <- t2.Join()
r3 <- t3.Join()

result <- r1 + r2 + r3
";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);
        var vm = new Bytecode.VM.VirtualMachine(bytecodeFile);

        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        // 1*2 + 2*2 + 3*2 = 2 + 4 + 6 = 12
        Assert.Equal(12, GetIntValue(result));
    }
}
