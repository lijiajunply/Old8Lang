using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Threading;

/// <summary>
/// 并发原语测试 - 覆盖 Mutex、Semaphore、AtomicInt、Channel、ReadWriteLock、CountDownLatch、CyclicBarrier
/// </summary>
[Collection("Sequential")]
public class ConcurrentPrimitiveTests
{
    #region Mutex 测试

    [Fact]
    public void Mutex_LockUnlockAndTryLock_WorkCorrectly()
    {
        var code = @"
mutex <- MutexCreate()
locked <- MutexTryLock(mutex, 100)
MutexUnlock(mutex)

MutexLock(mutex)
value <- 42
MutexUnlock(mutex)
MutexDispose(mutex)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("locked"))).Value);
        Assert.Equal(42, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("value"))).Value);
    }

    [Fact]
    public void Mutex_TryLockFromAnotherThreadWhileHeld_TimesOut()
    {
        var code = @"
mutex <- MutexCreate()
holderReady <- CountDownLatchCreate(1)
releaseHolder <- CountDownLatchCreate(1)
tryResult <- Lock(true)

func holder() -> void {
    MutexLock(mutex)
    CountDownLatchCountDown(holderReady)
    CountDownLatchWait(releaseHolder)
    MutexUnlock(mutex)
}

func contender() -> void {
    CountDownLatchWait(holderReady)
    acquired <- MutexTryLock(mutex, 30)
    tryResult.Set(acquired)
    if acquired {
        MutexUnlock(mutex)
    }
}

t1 <- Spawn(holder)
t2 <- Spawn(contender)
t1.Start()
t2.Start()

t2.Join()
CountDownLatchCountDown(releaseHolder)
t1.Join()

acquiredWhileHeld <- tryResult.Value

MutexDispose(mutex)
CountDownLatchDispose(holderReady)
CountDownLatchDispose(releaseHolder)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.False(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("acquiredWhileHeld"))).Value);
    }

    #endregion

    #region Semaphore 测试

    [Fact]
    public void Semaphore_AcquireTryAcquireAndRelease_EnforcesPermits()
    {
        var code = @"
sem <- SemaphoreCreate(2, 2)
a1 <- SemaphoreTryAcquire(sem, 50)
a2 <- SemaphoreTryAcquire(sem, 50)
a3 <- SemaphoreTryAcquire(sem, 20)
SemaphoreRelease(sem)
a4 <- SemaphoreTryAcquire(sem, 50)
SemaphoreRelease(sem)
SemaphoreRelease(sem)
SemaphoreDispose(sem)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("a1"))).Value);
        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("a2"))).Value);
        Assert.False(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("a3"))).Value);
        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("a4"))).Value);
    }

    [Fact]
    public void Semaphore_MultiThread_LimitsConcurrency()
    {
        var code = @"
sem <- SemaphoreCreate(2, 2)
active <- AtomicIntCreate(0)
maxActive <- AtomicIntCreate(0)
completed <- AtomicIntCreate(0)

func worker() -> void {
    SemaphoreAcquire(sem)
    try {
        cur <- AtomicIntIncrement(active)
        // 记录观测到的最大并发数
        prevMax <- AtomicIntGet(maxActive)
        while cur > prevMax {
            if AtomicIntCompareAndSet(maxActive, prevMax, cur) {
                break
            }
            prevMax <- AtomicIntGet(maxActive)
        }
        AtomicIntIncrement(completed)
        AtomicIntDecrement(active)
    } finally {
        SemaphoreRelease(sem)
    }
}

threads <- {}
for i <- 0, i < 6, i++ {
    th <- Spawn(worker)
    threads.Add(th)
    th.Start()
}

for th in threads {
    th.Join()
}

observedMax <- AtomicIntGet(maxActive)
doneCount <- AtomicIntGet(completed)

SemaphoreDispose(sem)
AtomicIntDispose(active)
AtomicIntDispose(maxActive)
AtomicIntDispose(completed)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        var observedMax = Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("observedMax"))).Value;
        Assert.InRange(observedMax, 1, 2);
        Assert.Equal(6, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("doneCount"))).Value);
    }

    #endregion

    #region AtomicInt 测试

    [Fact]
    public void AtomicInt_AllOperationsAndConcurrentIncrements_WorkCorrectly()
    {
        var code = @"
atom <- AtomicIntCreate(10)
initial <- AtomicIntGet(atom)
AtomicIntSet(atom, 20)
afterSet <- AtomicIntGet(atom)
afterInc <- AtomicIntIncrement(atom)
afterDec <- AtomicIntDecrement(atom)
afterAdd <- AtomicIntAdd(atom, 15)
casFail <- AtomicIntCompareAndSet(atom, 999, 100)
casOk <- AtomicIntCompareAndSet(atom, 35, 100)
afterCas <- AtomicIntGet(atom)

func incWorker(times: int) -> void {
    for i <- 0, i < times, i++ {
        AtomicIntIncrement(atom)
    }
}

t1 <- Spawn(incWorker, 100)
t2 <- Spawn(incWorker, 100)
t3 <- Spawn(incWorker, 100)
t1.Start()
t2.Start()
t3.Start()
t1.Join()
t2.Join()
t3.Join()

finalVal <- AtomicIntGet(atom)
AtomicIntDispose(atom)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(10, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("initial"))).Value);
        Assert.Equal(20, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("afterSet"))).Value);
        Assert.Equal(21, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("afterInc"))).Value);
        Assert.Equal(20, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("afterDec"))).Value);
        Assert.Equal(35, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("afterAdd"))).Value);
        Assert.False(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("casFail"))).Value);
        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("casOk"))).Value);
        Assert.Equal(100, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("afterCas"))).Value);
        Assert.Equal(400, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("finalVal"))).Value);
    }

    #endregion

    #region Channel 测试

    [Fact]
    public void Channel_UnboundedSendReceive_PreservesFifoOrder()
    {
        var code = @"
ch <- ChannelCreate()
ChannelSend(ch, ""first"")
ChannelSend(ch, 2)
ChannelSend(ch, true)

r1 <- ChannelReceive(ch)
r2 <- ChannelReceive(ch)
r3 <- ChannelReceive(ch)

ChannelClose(ch)
ChannelDispose(ch)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal("first", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("r1"))).Value);
        Assert.Equal(2, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("r2"))).Value);
        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("r3"))).Value);
    }

    [Fact]
    public void Channel_BoundedTrySendAndTryReceive_HandleCapacityAndTimeout()
    {
        var code = @"
ch <- ChannelCreateBounded(2)
s1 <- ChannelTrySend(ch, 10, 50)
s2 <- ChannelTrySend(ch, 20, 50)
s3 <- ChannelTrySend(ch, 30, 20)

v1 <- ChannelTryReceive(ch, 50)
v2 <- ChannelTryReceive(ch, 50)
vEmpty <- ChannelTryReceive(ch, 20)

ChannelClose(ch)
ChannelDispose(ch)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("s1"))).Value);
        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("s2"))).Value);
        Assert.False(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("s3"))).Value);
        Assert.Equal(10, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("v1"))).Value);
        Assert.Equal(20, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("v2"))).Value);
        Assert.IsType<VoidLangValue>(interpreter.Manager.GetValue(new LangId("vEmpty")));
    }

    #endregion

    #region ReadWriteLock 测试

    [Fact]
    public void ReadWriteLock_ConcurrentReadersAndExclusiveWriter_WorkCorrectly()
    {
        var code = @"
rwLock <- ReadWriteLockCreate()
sharedVal <- Lock(0)
readerCount <- AtomicIntCreate(0)
bothReadersEntered <- CountDownLatchCreate(2)
allowReadersExit <- CountDownLatchCreate(1)

func reader() -> void {
    ReadLockAcquire(rwLock)
    try {
        AtomicIntIncrement(readerCount)
        CountDownLatchCountDown(bothReadersEntered)
        CountDownLatchWait(allowReadersExit)
        _ <- sharedVal.Value
    } finally {
        ReadLockRelease(rwLock)
    }
}

r1 <- Spawn(reader)
r2 <- Spawn(reader)
r1.Start()
r2.Start()

// 两个读锁可以同时持有，不会互相阻塞
bothEntered <- CountDownLatchWaitTimeout(bothReadersEntered, 2000)
// 读锁持有期间，写锁尝试获取应超时失败
writeDuringRead <- WriteLockTryAcquire(rwLock, 30)

CountDownLatchCountDown(allowReadersExit)
r1.Join()
r2.Join()

// 读锁全部释放后，写锁可以成功获取并写入
writeAfterRead <- WriteLockTryAcquire(rwLock, 200)
if writeAfterRead {
    sharedVal.Set(99)
    WriteLockRelease(rwLock)
}

readAfterWrite <- ReadLockTryAcquire(rwLock, 200)
finalVal <- 0
if readAfterWrite {
    finalVal <- sharedVal.Value
    ReadLockRelease(rwLock)
}

ReadWriteLockDispose(rwLock)
AtomicIntDispose(readerCount)
CountDownLatchDispose(bothReadersEntered)
CountDownLatchDispose(allowReadersExit)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("bothEntered"))).Value);
        Assert.False(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("writeDuringRead"))).Value);
        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("writeAfterRead"))).Value);
        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("readAfterWrite"))).Value);
        Assert.Equal(99, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("finalVal"))).Value);
    }

    #endregion

    #region CountDownLatch 测试

    [Fact]
    public void CountDownLatch_CountDownWaitAndTimeout_WorkCorrectly()
    {
        var code = @"
latch <- CountDownLatchCreate(2)
initialCount <- CountDownLatchGetCount(latch)
timedOutEarly <- CountDownLatchWaitTimeout(latch, 20)

func worker() -> void {
    CountDownLatchCountDown(latch)
}

t1 <- Spawn(worker)
t2 <- Spawn(worker)
t1.Start()
t2.Start()

waitedOk <- CountDownLatchWaitTimeout(latch, 2000)
t1.Join()
t2.Join()

finalCount <- CountDownLatchGetCount(latch)
CountDownLatchDispose(latch)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(2, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("initialCount"))).Value);
        Assert.False(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("timedOutEarly"))).Value);
        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("waitedOk"))).Value);
        Assert.Equal(0, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("finalCount"))).Value);
    }

    #endregion

    #region CyclicBarrier 测试

    [Fact]
    public void CyclicBarrier_MetadataAndTimeoutAndReuse_WorkCorrectly()
    {
        var code = @"
barrier <- CyclicBarrierCreate(2)
participants <- CyclicBarrierGetParticipantCount(barrier)
waitingInitial <- CyclicBarrierGetWaitingCount(barrier)

// 单个线程等待 2 人栅栏应超时
singleTimeout <- CyclicBarrierAwaitTimeout(barrier, 20)
CyclicBarrierDispose(barrier)

// 重新创建并验证两轮循环复用
reuseBarrier <- CyclicBarrierCreate(2)
rounds <- AtomicIntCreate(0)

func runner() -> void {
    AtomicIntIncrement(rounds)
    CyclicBarrierAwait(reuseBarrier)
    AtomicIntIncrement(rounds)
    CyclicBarrierAwait(reuseBarrier)
}

t1 <- Spawn(runner)
t2 <- Spawn(runner)
t1.Start()
t2.Start()
t1.Join()
t2.Join()

totalRounds <- AtomicIntGet(rounds)
CyclicBarrierDispose(reuseBarrier)
AtomicIntDispose(rounds)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(2, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("participants"))).Value);
        Assert.Equal(0, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("waitingInitial"))).Value);
        Assert.False(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("singleTimeout"))).Value);
        Assert.Equal(4, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("totalRounds"))).Value);
    }

    #endregion

    #region 并发辅助函数测试

    [Fact]
    public void ConcurrencyUtilities_ThreadIdAndProcessorCount_ReturnPositiveValues()
    {
        var code = @"
tid <- GetCurrentThreadId()
cpuCount <- GetProcessorCount()
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(Environment.CurrentManagedThreadId,
            Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("tid"))).Value);
        Assert.True(Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("cpuCount"))).Value > 0);
    }

    #endregion
}
