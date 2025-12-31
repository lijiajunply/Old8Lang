using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.StandardLibrary;

/// <summary>
/// AsyncLib 库测试 - 测试异步和多线程功能
/// </summary>
public class AsyncLibTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_Async_ShouldWorkCorrectly()
    {
        var code = @"
import Async

PrintLine(""Async library imported"")
";
        CreateTempModuleFile("./StandardLibrary/async_test.old8", code);
        var (interpreter, exception) = ExecuteCodeFile("./StandardLibrary/async_test.old8");

        Assert.Null(exception);
        var asyncLib = interpreter.Manager.GetValue(new LangId("Async"));
        Assert.NotNull(asyncLib);
        Assert.IsAssignableFrom<IModuleValueType>(asyncLib);
    }

    [Fact]
    public void Sleep_ShouldPauseExecution()
    {
        var code = @"
import Async

PrintLine(""Before sleep"")
Async.Sleep(100)
PrintLine(""After sleep"")
";
        CreateTempModuleFile("./StandardLibrary/async_sleep_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/async_sleep_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void GetCurrentThreadId_ShouldReturnThreadId()
    {
        var code = @"
import Async

threadId <- Async.GetCurrentThreadId()
PrintLine($""Thread ID: {threadId}"")
";
        CreateTempModuleFile("./StandardLibrary/async_threadid_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/async_threadid_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void GetProcessorCount_ShouldReturnProcessorCount()
    {
        var code = @"
import Async

count <- Async.GetProcessorCount()
PrintLine($""Processor count: {count}"")
";
        CreateTempModuleFile("./StandardLibrary/async_processor_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/async_processor_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void MutexCreate_And_MutexDispose_ShouldWorkCorrectly()
    {
        var code = @"
import Async

mutexId <- Async.MutexCreate()
PrintLine($""Created mutex with ID: {mutexId}"")
Async.MutexDispose(mutexId)
PrintLine(""Mutex disposed"")
";
        CreateTempModuleFile("./StandardLibrary/async_mutex_create_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/async_mutex_create_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void SemaphoreCreate_And_SemaphoreDispose_ShouldWorkCorrectly()
    {
        var code = @"
import Async

semId <- Async.SemaphoreCreate(1, 5)
PrintLine($""Created semaphore with ID: {semId}"")
Async.SemaphoreDispose(semId)
PrintLine(""Semaphore disposed"")
";
        CreateTempModuleFile("./StandardLibrary/async_semaphore_create_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/async_semaphore_create_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void AtomicIntCreate_And_Operations_ShouldWorkCorrectly()
    {
        var code = @"
import Async

atomicId <- Async.AtomicIntCreate(0)
PrintLine($""Created atomic int with ID: {atomicId}"")

// Get initial value
value <- Async.AtomicIntGet(atomicId)
PrintLine($""Initial value: {value}"")

// Increment
newValue <- Async.AtomicIntIncrement(atomicId)
PrintLine($""After increment: {newValue}"")

// Set value
Async.AtomicIntSet(atomicId, 100)
value <- Async.AtomicIntGet(atomicId)
PrintLine($""After set to 100: {value}"")

// Add
newValue <- Async.AtomicIntAdd(atomicId, 50)
PrintLine($""After add 50: {newValue}"")

// Decrement
newValue <- Async.AtomicIntDecrement(atomicId)
PrintLine($""After decrement: {newValue}"")

Async.AtomicIntDispose(atomicId)
PrintLine(""Atomic int disposed"")
";
        CreateTempModuleFile("./StandardLibrary/async_atomicint_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/async_atomicint_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void AtomicIntCompareAndSet_ShouldWorkCorrectly()
    {
        var code = @"
import Async

atomicId <- Async.AtomicIntCreate(100)
PrintLine($""Created atomic int with value 100"")

// Compare and set - should succeed
success <- Async.AtomicIntCompareAndSet(atomicId, 100, 200)
PrintLine($""CAS (100->200): {success}"")

value <- Async.AtomicIntGet(atomicId)
PrintLine($""Current value: {value}"")

// Compare and set - should fail
success <- Async.AtomicIntCompareAndSet(atomicId, 100, 300)
PrintLine($""CAS (100->300): {success}"")

Async.AtomicIntDispose(atomicId)
";
        CreateTempModuleFile("./StandardLibrary/async_cas_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/async_cas_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void ChannelCreate_And_ChannelDispose_ShouldWorkCorrectly()
    {
        var code = @"
import Async

channelId <- Async.ChannelCreate()
PrintLine($""Created unbounded channel with ID: {channelId}"")
Async.ChannelDispose(channelId)
PrintLine(""Channel disposed"")
";
        CreateTempModuleFile("./StandardLibrary/async_channel_create_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/async_channel_create_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void ChannelCreateBounded_ShouldWorkCorrectly()
    {
        var code = @"
import Async

channelId <- Async.ChannelCreateBounded(10)
PrintLine($""Created bounded channel (capacity 10) with ID: {channelId}"")
Async.ChannelDispose(channelId)
PrintLine(""Channel disposed"")
";
        CreateTempModuleFile("./StandardLibrary/async_channel_bounded_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/async_channel_bounded_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void CreateCancellationTokenSource_And_Dispose_ShouldWorkCorrectly()
    {
        var code = @"
import Async

ctsId <- Async.CreateCancellationTokenSource()
PrintLine($""Created CancellationTokenSource with ID: {ctsId}"")
Async.DisposeCancellationTokenSource(ctsId)
PrintLine(""CancellationTokenSource disposed"")
";
        CreateTempModuleFile("./StandardLibrary/async_cts_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/async_cts_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void ReadWriteLockCreate_And_Dispose_ShouldWorkCorrectly()
    {
        var code = @"
import Async

lockId <- Async.ReadWriteLockCreate()
PrintLine($""Created ReadWriteLock with ID: {lockId}"")
Async.ReadWriteLockDispose(lockId)
PrintLine(""ReadWriteLock disposed"")
";
        CreateTempModuleFile("./StandardLibrary/async_rwlock_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/async_rwlock_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void CountDownLatchCreate_And_Dispose_ShouldWorkCorrectly()
    {
        var code = @"
import Async

latchId <- Async.CountDownLatchCreate(3)
PrintLine($""Created CountDownLatch with count 3"")

count <- Async.CountDownLatchGetCount(latchId)
PrintLine($""Initial count: {count}"")

Async.CountDownLatchCountDown(latchId)
count <- Async.CountDownLatchGetCount(latchId)
PrintLine($""Count after countdown: {count}"")

Async.CountDownLatchDispose(latchId)
PrintLine(""CountDownLatch disposed"")
";
        CreateTempModuleFile("./StandardLibrary/async_latch_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/async_latch_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void CyclicBarrierCreate_And_Dispose_ShouldWorkCorrectly()
    {
        var code = @"
import Async

barrierId <- Async.CyclicBarrierCreate(2)
PrintLine($""Created CyclicBarrier with 2 participants"")

participantCount <- Async.CyclicBarrierGetParticipantCount(barrierId)
PrintLine($""Participant count: {participantCount}"")

waitingCount <- Async.CyclicBarrierGetWaitingCount(barrierId)
PrintLine($""Waiting count: {waitingCount}"")

Async.CyclicBarrierDispose(barrierId)
PrintLine(""CyclicBarrier disposed"")
";
        CreateTempModuleFile("./StandardLibrary/async_barrier_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/async_barrier_test.old8");

        Assert.Null(exception);
    }
}
