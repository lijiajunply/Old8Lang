using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Threading;

/// <summary>
/// 线程同步测试 - 覆盖多线程共享资源访问、锁与临界区、同步顺序与竞态防护
/// </summary>
[Collection("Sequential")]
public class ThreadSynchronizationTests
{
    #region LockedVariableLangValue 同步操作测试

    [Fact]
    public void LockedVariable_BasicOperations_WorkDeterministically()
    {
        var code = @"
counter <- Lock(10)
v1 <- counter.Value
v2 <- counter.Get()
counter.Set(20)
v3 <- counter.Value
v4 <- counter.Increment()
v5 <- counter.Decrement()
v6 <- counter.Add(5)
v7 <- counter.Update((x) -> x * 2)
finalValue <- counter.Get()
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(10, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("v1"))).Value);
        Assert.Equal(10, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("v2"))).Value);
        Assert.Equal(20, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("v3"))).Value);
        Assert.Equal(21, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("v4"))).Value);
        Assert.Equal(20, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("v5"))).Value);
        Assert.Equal(25, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("v6"))).Value);
        Assert.Equal(50, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("v7"))).Value);
        Assert.Equal(50, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("finalValue"))).Value);
    }

    [Fact]
    public void MultiThread_LockedVariable_NoLostUpdatesUnderContention()
    {
        var code = @"
counter <- Lock(0)

func worker(iterations: int) -> void {
    for i <- 0, i < iterations, i++ {
        counter.Increment()
    }
}

threads <- {}
for t <- 0, t < 4, t++ {
    th <- Spawn(worker, 50)
    threads.Add(th)
    th.Start()
}

for th in threads {
    th.Join()
}

total <- counter.Value
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        var total = Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("total")));
        Assert.Equal(200, total.Value);
    }

    [Fact]
    public void MultiThread_LockedVariableUpdate_ExecutesAtomically()
    {
        var code = @"
balance <- Lock(1000)

func withdraw(amount: int, times: int) -> void {
    for i <- 0, i < times, i++ {
        balance.Update((cur) -> cur - amount)
    }
}

func deposit(amount: int, times: int) -> void {
    for i <- 0, i < times, i++ {
        balance.Update((cur) -> cur + amount)
    }
}

t1 <- Spawn(withdraw, 10, 30)
t2 <- Spawn(deposit, 15, 20)
t3 <- Spawn(withdraw, 5, 20)

t1.Start()
t2.Start()
t3.Start()

t1.Join()
t2.Join()
t3.Join()

finalBalance <- balance.Get()
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        // 1000 - 300 + 300 - 100 = 900
        var finalBalance = Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("finalBalance")));
        Assert.Equal(900, finalBalance.Value);
    }

    #endregion

    #region Mutex 临界区保护测试

    [Fact]
    public void MultiThread_MutexCriticalSection_ProtectsSharedListAndCounter()
    {
        var code = @"
mutex <- MutexCreate()
sharedList <- {}
sharedSum <- AtomicIntCreate(0)

func worker(workerId: int, count: int) -> void {
    for i <- 0, i < count, i++ {
        MutexLock(mutex)
        try {
            sharedList.Add(workerId * 100 + i)
            AtomicIntAdd(sharedSum, 1)
        } finally {
            MutexUnlock(mutex)
        }
    }
}

t1 <- Spawn(worker, 1, 25)
t2 <- Spawn(worker, 2, 25)
t3 <- Spawn(worker, 3, 25)

t1.Start()
t2.Start()
t3.Start()

t1.Join()
t2.Join()
t3.Join()

listCount <- sharedList.Count()
sumValue <- AtomicIntGet(sharedSum)

MutexDispose(mutex)
AtomicIntDispose(sharedSum)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(75, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("listCount"))).Value);
        Assert.Equal(75, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("sumValue"))).Value);
    }

    #endregion

    #region 同步顺序与协调测试

    [Fact]
    public void MultiThread_CountDownLatch_EnforcesStrictOrdering()
    {
        var code = @"
stage1Done <- CountDownLatchCreate(1)
stage2Done <- CountDownLatchCreate(1)
order <- {}
orderMutex <- MutexCreate()

func appendOrder(step: string) -> void {
    MutexLock(orderMutex)
    try {
        order.Add(step)
    } finally {
        MutexUnlock(orderMutex)
    }
}

func stage2Worker() -> void {
    CountDownLatchWait(stage1Done)
    appendOrder(""stage2"")
    CountDownLatchCountDown(stage2Done)
}

func stage3Worker() -> void {
    CountDownLatchWait(stage2Done)
    appendOrder(""stage3"")
}

t3 <- Spawn(stage3Worker)
t2 <- Spawn(stage2Worker)
t3.Start()
t2.Start()

appendOrder(""stage1"")
CountDownLatchCountDown(stage1Done)

t2.Join()
t3.Join()

first <- order[0]
second <- order[1]
third <- order[2]

CountDownLatchDispose(stage1Done)
CountDownLatchDispose(stage2Done)
MutexDispose(orderMutex)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal("stage1", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("first"))).Value);
        Assert.Equal("stage2", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("second"))).Value);
        Assert.Equal("stage3", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("third"))).Value);
    }

    [Fact]
    public void MultiThread_ChannelProducerConsumer_SynchronizesAllItems()
    {
        var code = @"
ch <- ChannelCreateBounded(4)
receivedSum <- AtomicIntCreate(0)
receivedCount <- AtomicIntCreate(0)

func producer() -> void {
    for i <- 1, i <= 20, i++ {
        ChannelSend(ch, i)
    }
    ChannelClose(ch)
}

func consumer() -> void {
    for i <- 0, i < 20, i++ {
        val <- ChannelReceive(ch)
        AtomicIntAdd(receivedSum, val)
        AtomicIntIncrement(receivedCount)
    }
}

p <- Spawn(producer)
c <- Spawn(consumer)

p.Start()
c.Start()

p.Join()
c.Join()

totalSum <- AtomicIntGet(receivedSum)
totalCount <- AtomicIntGet(receivedCount)

ChannelDispose(ch)
AtomicIntDispose(receivedSum)
AtomicIntDispose(receivedCount)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(210, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("totalSum"))).Value);
        Assert.Equal(20, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("totalCount"))).Value);
    }

    [Fact]
    public void MultiThread_CyclicBarrier_SynchronizesPhases()
    {
        var code = @"
barrier <- CyclicBarrierCreate(3)
phase1Count <- AtomicIntCreate(0)
phase2SawAllPhase1 <- AtomicIntCreate(0)

func participant() -> void {
    AtomicIntIncrement(phase1Count)
    CyclicBarrierAwait(barrier)

    if AtomicIntGet(phase1Count) == 3 {
        AtomicIntIncrement(phase2SawAllPhase1)
    }
    CyclicBarrierAwait(barrier)
}

t1 <- Spawn(participant)
t2 <- Spawn(participant)
t3 <- Spawn(participant)

t1.Start()
t2.Start()
t3.Start()

t1.Join()
t2.Join()
t3.Join()

p1 <- AtomicIntGet(phase1Count)
p2 <- AtomicIntGet(phase2SawAllPhase1)

CyclicBarrierDispose(barrier)
AtomicIntDispose(phase1Count)
AtomicIntDispose(phase2SawAllPhase1)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(3, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("p1"))).Value);
        Assert.Equal(3, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("p2"))).Value);
    }

    #endregion
}
