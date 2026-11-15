using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Threading;

/// <summary>
/// IL 模式下把集合里的资源 ID 传给并发全局函数。
/// </summary>
/// <remarks>
/// 并发函数的 <c>GenerateIlInternal</c> 会把「加载实参 + call ResourceManager.Xxx(int)」
/// 直接发射进调用方。for-in 循环变量的静态类型是 <c>object</c>，不拆箱就调用
/// <c>DisposeXxx(int)</c> 会生成无效 IL（JIT 报 InvalidProgramException）。
/// </remarks>
[Collection("Sequential")]
public class ConcurrencyIdArgumentIlTests
{
    [Fact]
    public void DisposeReadWriteLock_WithObjectTypedId_CompilesAndExecutes()
    {
        const string code = @"
            id <- ReadWriteLockCreate()
            ids <- {id}
            for value in ids {
                ReadWriteLockDispose(value)
            }
            PrintLine(""disposed"")
        ";
        CompileAndRun(code);
    }

    [Fact]
    public void DisposeMutex_WithObjectTypedId_CompilesAndExecutes()
    {
        const string code = @"
            ids <- {MutexCreate()}
            for value in ids {
                MutexDispose(value)
            }
            PrintLine(""disposed"")
        ";
        CompileAndRun(code);
    }

    [Fact]
    public void DisposeCountDownLatchAndBarrier_WithObjectTypedIds_CompilesAndExecutes()
    {
        const string code = @"
            latches <- {CountDownLatchCreate(1)}
            for value in latches {
                CountDownLatchDispose(value)
            }

            barriers <- {CyclicBarrierCreate(1)}
            for value in barriers {
                CyclicBarrierDispose(value)
            }

            PrintLine(""disposed"")
        ";
        CompileAndRun(code);
    }

    private static void CompileAndRun(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        try
        {
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)();
        }
        finally
        {
            Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;
        }
    }
}
