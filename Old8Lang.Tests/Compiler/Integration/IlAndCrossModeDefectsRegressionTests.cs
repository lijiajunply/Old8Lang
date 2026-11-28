using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Integration;

/// <summary>
/// 2026-11-28 IL 与跨模式缺陷修复回归测试：
/// - 表达式内部 await 提升
/// - IL object 操作数动态算术与循环回写
/// - IL 值类型变量在 catch 中赋异常时保留成功分支值
/// - 三模式线程异常口径统一
/// </summary>
[Collection("Sequential")]
public class IlAndCrossModeDefectsRegressionTests
{
    [Fact]
    public void IlAsync_AwaitInsideComplexExpressions_CompilesAndExecutesCorrectly()
    {
        var code = @"
async func fetch(n:int) -> int {
    await Task.Delay(5)
    return n * 10
}

async func compute() -> int {
    a <- 1 + await fetch(2)
    b <- (await fetch(3)) + (await fetch(4))
    arr <- [await fetch(1), await fetch(2)]
    d <- {""x"": await fetch(5)}
    flag <- true && ((await fetch(1)) == 10)
    if flag {
        return a + b + arr[0] + arr[1] + d[""x""]
    }
    return -1
}

res <- await compute()
Assert.Equal(171, res)
";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test_expr_await", interpreter);
        Assert.Null(Record.Exception(() => action()));
    }

    [Fact]
    public void IlDynamicArithmetic_ObjectOperandsAndLoopAccumulation_WorkCorrectly()
    {
        var code = @"
func process(v) {
    return v + 1
}

func combine(a, b) {
    return a + b
}

class Calc {
    func mul(a, b) {
        return a * b
    }
}

Assert.Equal(4.5, process(3.5))
Assert.Equal(11, process(10))
Assert.Equal(""ab"", combine(""a"", ""b""))
Assert.Equal(7.5, combine(2.5, 5))

c <- Calc()
Assert.Equal(7.5, c.mul(2.5, 3))

items <- {1, 2, 3, 4, 5}
sum <- 0
for item in items {
    sum <- sum + item
}
Assert.Equal(15, sum)
";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test_dyn_arith", interpreter);
        Assert.Null(Record.Exception(() => action()));
    }

    [Fact]
    public void IlCatchAssignment_ValueTypeLocalPreservedOnSuccessAndUpdatedOnCatch()
    {
        var code = @"
func runCase(shouldThrow:bool) -> object {
    x <- 1
    try {
        if shouldThrow {
            throw ""boom""
        }
        x <- x + 41
    } catch (e) {
        x <- e
    }
    return x
}

okVal <- runCase(false)
errVal <- runCase(true)
Assert.Equal(42, okVal)
Assert.Equal(""boom"", errVal)

// 仅在 try 前赋值、catch 内赋异常（try 内不赋值）的值类型变量也必须保留初值
y <- 7
try {
    z <- 1 + 1
} catch (e) {
    y <- e
}
Assert.Equal(7, y)
";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test_catch_val", interpreter);
        Assert.Null(Record.Exception(() => action()));
    }

    [Fact]
    public void ThreadJoinException_UnifiedAcrossInterpreterIlAndVm()
    {
        var code = @"
func failingTask() -> int {
    throw ""Task failed""
}

t <- spawn(failingTask)
t.Start()

caught <- """"
try {
    t.Join()
} catch (e) {
    caught <- e.ToStr()
}

Assert.Equal(""Task failed"", caught)
";
        // 1. 解释模式
        var interp = new LangInterpreter();
        interp.Build(code).Run(interp.Manager);
        Assert.Equal("Task failed", ((StringLangValue)interp.Manager.GetValue(new LangId("caught"))!).Value);

        // 2. IL 模式
        var ilInterp = new LangInterpreter();
        var compiled = Old8Lang.Compiler.Compiler.Compile(ilInterp.Build(code), "test_thread_ex_il", ilInterp);
        Assert.Null(Record.Exception(() => compiled()));

        // 3. VM 模式
        var vmInterp = new LangInterpreter();
        var bytecode = new BytecodeCompiler().Compile(vmInterp.Build(code));
        var vm = new Old8Lang.Bytecode.VM.VirtualMachine(bytecode);
        Assert.Null(Record.Exception(() => vm.Execute()));
    }
}
