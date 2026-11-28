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

    [Fact]
    public void AllConcurrencyPrimitives_WithObjectTypedIdsAndNumericArgs_CompileAndExecute()
    {
        const string code = @"
            timeouts <- {10, 20}
            for t in timeouts {
                Sleep(t)
                break
            }

            // Mutex
            mutexIds <- {MutexCreate()}
            for m in mutexIds {
                MutexLock(m)
                MutexUnlock(m)
                ok <- MutexTryLock(m, t)
                Assert.True(ok)
                MutexUnlock(m)
                MutexDispose(m)
            }

            // Semaphore
            counts <- {2}
            for c in counts {
                semId <- SemaphoreCreate(c, c)
                semIds <- {semId}
                for s in semIds {
                    SemaphoreAcquire(s)
                    SemaphoreRelease(s)
                    ok <- SemaphoreTryAcquire(s, t)
                    Assert.True(ok)
                    SemaphoreRelease(s)
                    SemaphoreDispose(s)
                }
            }

            // AtomicInt
            atomicIds <- {AtomicIntCreate(0)}
            vals <- {10}
            deltas <- {5}
            expected <- {15}
            replacement <- {20}
            for a in atomicIds {
                for v in vals {
                    AtomicIntSet(a, v)
                }
                Assert.Equal(10, AtomicIntGet(a))
                Assert.Equal(11, AtomicIntIncrement(a))
                Assert.Equal(10, AtomicIntDecrement(a))
                for d in deltas {
                    Assert.Equal(15, AtomicIntAdd(a, d))
                }
                for e in expected {
                    for r in replacement {
                        Assert.True(AtomicIntCompareAndSet(a, e, r))
                    }
                }
                Assert.Equal(20, AtomicIntGet(a))
                AtomicIntDispose(a)
            }

            // ReadWriteLock
            rwIds <- {ReadWriteLockCreate()}
            for rw in rwIds {
                ReadLockAcquire(rw)
                ReadLockRelease(rw)
                WriteLockAcquire(rw)
                WriteLockRelease(rw)
                Assert.True(ReadLockTryAcquire(rw, t))
                ReadLockRelease(rw)
                Assert.True(WriteLockTryAcquire(rw, t))
                WriteLockRelease(rw)
                ReadWriteLockDispose(rw)
            }

            // CountDownLatch
            latchIds <- {CountDownLatchCreate(1)}
            for l in latchIds {
                Assert.Equal(1, CountDownLatchGetCount(l))
                CountDownLatchCountDown(l)
                CountDownLatchWait(l)
                Assert.True(CountDownLatchWaitTimeout(l, t))
                CountDownLatchDispose(l)
            }

            // CyclicBarrier
            barrierIds <- {CyclicBarrierCreate(1)}
            for b in barrierIds {
                Assert.Equal(1, CyclicBarrierGetParticipantCount(b))
                Assert.Equal(0, CyclicBarrierGetWaitingCount(b))
                CyclicBarrierAwait(b)
                Assert.True(CyclicBarrierAwaitTimeout(b, t))
                CyclicBarrierDispose(b)
            }

            // Channel
            chanIds <- {ChannelCreateBounded(2)}
            for ch in chanIds {
                ChannelSend(ch, ""hello"")
                Assert.Equal(""hello"", ChannelReceive(ch))
                Assert.True(ChannelTrySend(ch, 42, t))
                rec <- ChannelTryReceive(ch, t)
                Assert.Equal(42, rec)
                ChannelClose(ch)
                ChannelDispose(ch)
            }

            // CancellationTokenSource
            ctsIds <- {CreateCancellationTokenSource()}
            for cts in ctsIds {
                CancelAfter(cts, t)
                Cancel(cts)
                DisposeCancellationTokenSource(cts)
            }
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
