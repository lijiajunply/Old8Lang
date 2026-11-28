using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.Compiler.Async;

public class AsyncLambdaAndDecoratorTests
{
    [Fact]
    public void AsyncLambda_BasicAndClosureCapture_WorksAcrossAllThreeModes()
    {
        const string code = @"
            factor <- 3
            fn <- async (x:int) -> {
                await Task.Delay(5)
                return x * factor
            }
            result <- await fn(7)
        ";

        // Interpreter
        var interp = new LangInterpreter();
        interp.Build(code).Run(interp.Manager);
        Assert.Equal(21, ((IntLangValue)interp.Manager.GetValue(new LangId("result"))!).Value);

        // IL
        var ilInterp = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(
            ilInterp.Build(code + "\nAssert.Equal(21, result)"), "test", ilInterp)();

        // VM
        var vm = new VM(VirtualMachine.CompileHelper.CompileToBytecode(code));
        vm.Execute();
        Assert.Equal(21, vm.GetGlobalVariable("result"));
    }

    [Fact]
    public void AsyncLambda_ReturnedFromFunction_CapturesParameterAcrossAllThreeModes()
    {
        const string code = @"
            func makeAsyncAdder(n:int) -> function {
                return async (x:int) -> {
                    await Task.Delay(5)
                    return x + n
                }
            }

            add5 <- makeAsyncAdder(5)
            result <- await add5(12)
        ";

        // Interpreter
        var interp = new LangInterpreter();
        interp.Build(code).Run(interp.Manager);
        Assert.Equal(17, ((IntLangValue)interp.Manager.GetValue(new LangId("result"))!).Value);

        // IL
        var ilInterp = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(
            ilInterp.Build(code + "\nAssert.Equal(17, result)"), "test", ilInterp)();

        // VM
        var vm = new VM(VirtualMachine.CompileHelper.CompileToBytecode(code));
        vm.Execute();
        Assert.Equal(17, vm.GetGlobalVariable("result"));
    }

    [Fact]
    public void DecoratedAsyncFunction_SimpleParameterizedAndStacked_WorksAcrossAllThreeModes()
    {
        const string code = @"
            func addOne(f) {
                return async (x:int) -> {
                    v <- await f(x)
                    return v + 1
                }
            }

            func scale(factor:int) {
                return (f) -> async (x:int) -> {
                    v <- await f(x)
                    return v * factor
                }
            }

            @addOne
            @scale(3)
            async func compute(x:int) -> int {
                await Task.Delay(5)
                return x + 2
            }

            result <- await compute(4)
        ";

        // (4 + 2) * 3 + 1 = 19

        // Interpreter
        var interp = new LangInterpreter();
        interp.Build(code).Run(interp.Manager);
        Assert.Equal(19, ((IntLangValue)interp.Manager.GetValue(new LangId("result"))!).Value);

        // IL
        var ilInterp = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(
            ilInterp.Build(code + "\nAssert.Equal(19, result)"), "test", ilInterp)();

        // VM (including bytecode serialization roundtrip)
        var bytecode = VirtualMachine.CompileHelper.CompileToBytecode(code);
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            bytecode.WriteTo(writer);
        ms.Position = 0;
        BytecodeFile reloaded;
        using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            reloaded = BytecodeFile.ReadFrom(reader);

        var vm = new VM(reloaded);
        vm.Execute();
        Assert.Equal(19, vm.GetGlobalVariable("result"));
    }
}
