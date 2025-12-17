using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Threading;

/// <summary>
/// 线程同步测试
/// </summary>
public class ThreadSynchronizationTests
{
    [Fact]
    public void ThreadSync_BasicMutex_HandlesMutexLocking()
    {
        // Arrange
        var code = @"
            mutex <- threading.Mutex()
            sharedResource <- 0
            func incrementResource() -> void {
                mutex.Lock()
                sharedResource <- sharedResource + 1
                mutex.Unlock()
            }
            threads <- []
            for i in 1..5 {
                thread <- spawn incrementResource()
                threads.Add(thread)
            }
            for thread in threads {
                thread.Wait()
            }
            result <- sharedResource
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_MutexWithTimeout_HandlesMutexTimeout()
    {
        // Arrange
        var code = @"
            mutex <- threading.Mutex()
            locked <- false
            func lockWithTimeout() -> bool {
                return mutex.TryLock(100) // 100ms timeout
            }
            func holdLock() -> void {
                mutex.Lock()
                locked <- true
                threading.Sleep(200) // Hold lock for 200ms
                mutex.Unlock()
                locked <- false
            }
            // Start a thread that holds the lock
            holderThread <- spawn holdLock()
            threading.Sleep(50) // Give holder time to acquire lock
            // Try to lock with timeout
            result <- lockWithTimeout()
            holderThread.Wait()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        // Should be false because timeout occurs
        Assert.Equal(false, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_Semaphore_HandlesSemaphoreCounting()
    {
        // Arrange
        var code = @"
            semaphore <- threading.Semaphore(2) // Allow 2 concurrent threads
            activeCount <- 0
            maxActiveCount <- 0
            func worker(id:int) -> void {
                semaphore.Wait()
                activeCount <- activeCount + 1
                if activeCount > maxActiveCount {
                    maxActiveCount <- activeCount
                }
                threading.Sleep(100)
                activeCount <- activeCount - 1
                semaphore.Release()
            }
            threads <- []
            for i in 1..5 {
                thread <- spawn worker(i)
                threads.Add(thread)
            }
            for thread in threads {
                thread.Wait()
            }
            result <- maxActiveCount
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(2, ((IntLangValue)result).Value); // Max 2 concurrent threads
    }

    [Fact]
    public void ThreadSync_ConditionVariable_HandlesConditionSignaling()
    {
        // Arrange
        var code = @"
            mutex <- threading.Mutex()
            condition <- threading.ConditionVariable()
            ready <- false
            func waiter() -> void {
                mutex.Lock()
                while not ready {
                    condition.Wait(mutex)
                }
                mutex.Unlock()
            }
            func signaller() -> void {
                threading.Sleep(50)
                mutex.Lock()
                ready <- true
                condition.Signal()
                mutex.Unlock()
            }
            waiterThread <- spawn waiter()
            signallerThread <- spawn signaller()
            waiterThread.Wait()
            signallerThread.Wait()
            result <- ""condition signaled""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("condition signaled", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_Barrier_HandlesThreadBarrier()
    {
        // Arrange
        var code = @"
            barrier <- threading.Barrier(3)
            arrivals <- 0
            func participant(id:int) -> void {
                // Simulate work
                threading.Sleep(id * 20)
                arrivals <- arrivals + 1
                // Wait at barrier
                barrier.Signal()
                result <- ""all participants ready""
            }
            threads <- []
            for i in 1..3 {
                thread <- spawn participant(i)
                threads.Add(thread)
            }
            for thread in threads {
                thread.Wait()
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("all participants ready", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_ReadWriteLock_HandlesReadWriteLocking()
    {
        // Arrange
        var code = @"
            rwLock <- threading.ReadWriteLock()
            sharedData <- 0
            func reader(id:int) -> void {
                rwLock.ReadLock()
                // Read sharedData
                value <- sharedData
                threading.Sleep(20)
                rwLock.ReadUnlock()
            }
            func writer(value:int) -> void {
                rwLock.WriteLock()
                sharedData <- sharedData + value
                threading.Sleep(50)
                rwLock.WriteUnlock()
            }
            // Start readers and writers
            threads <- []
            threads.Add(spawn writer(10))
            threads.Add(spawn writer(20))
            threads.Add(spawn reader(1))
            threads.Add(spawn reader(2))
            for thread in threads {
                thread.Wait()
            }
            result <- sharedData
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_AtomicOperations_HandlesAtomicOperations()
    {
        // Arrange
        var code = @"
            atomicCounter <- threading.Atomic(0)
            func incrementCounter() -> void {
                atomicCounter.Increment()
            }
            func getCounter() -> int {
                return atomicCounter.Get()
            }
            threads <- []
            for i in 1..10 {
                thread <- spawn incrementCounter()
                threads.Add(thread)
            }
            for thread in threads {
                thread.Wait()
            }
            result <- getCounter()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(10, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_Event_WaitsForEventSignal()
    {
        // Arrange
        var code = @"
            event <- threading.Event()
            result <- ""waiting""
            func waiter() -> void {
                event.Wait()
                result <- ""event received""
            }
            func signaller() -> void {
                threading.Sleep(50)
                event.Signal()
            }
            waiterThread <- spawn waiter()
            signallerThread <- spawn signaller()
            waiterThread.Wait()
            signallerThread.Wait()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("event received", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_Latch_CountsDownToZero()
    {
        // Arrange
        var code = @"
            latch <- threading.CountDownLatch(3)
            completed <- 0
            func worker(id:int) -> void {
                // Simulate work
                threading.Sleep(id * 20)
                completed <- completed + 1
                latch.CountDown()
            }
            // Start workers
            threads <- []
            for i in 1..3 {
                thread <- spawn worker(i)
                threads.Add(thread)
            }
            // Wait for all workers
            latch.Wait()
            result <- completed
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_Future_HandlesFutureResult()
    {
        // Arrange
        var code = @"
            func asyncOperation() -> string {
                threading.Sleep(100)
                return ""future result""
            }
            future <- threading.Future(asyncOperation)
            result <- future.Get() // Wait for result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("future result", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_ProducerConsumer_HandlesProducerConsumerPattern()
    {
        // Arrange
        var code = @"
            buffer <- []
            bufferMutex <- threading.Mutex()
            notEmpty <- threading.ConditionVariable()
            notFull <- threading.ConditionVariable()
            bufferSize <- 5
            producedCount <- 0
            consumedCount <- 0
            func producer(id:int) -> void {
                for i in 1..3 {
                    bufferMutex.Lock()
                    while buffer.Count >= bufferSize {
                        notFull.Wait(bufferMutex)
                    }
                    buffer.Add(""item-"" + id.ToStr() + ""-"" + i.ToStr())
                    producedCount <- producedCount + 1
                    notEmpty.Signal()
                    bufferMutex.Unlock()
                }
            }
            func consumer() -> void {
                for i in 1..6 {
                    bufferMutex.Lock()
                    while buffer.Count = 0 {
                        notEmpty.Wait(bufferMutex)
                    }
                    item <- buffer[0]
                    buffer.Remove(0)
                    consumedCount <- consumedCount + 1
                    notFull.Signal()
                    bufferMutex.Unlock()
                }
            }
            producer1 <- spawn producer(1)
            producer2 <- spawn producer(2)
            consumerThread <- spawn consumer()
            producer1.Wait()
            producer2.Wait()
            consumerThread.Wait()
            result <- producedCount.ToStr() + ""-"" + consumedCount.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("6-6", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_Monitor_HandlesMonitorObject()
    {
        // Arrange
        var code = @"
            monitor <- threading.Monitor()
            sharedValue <- 0
            func incrementWithMonitor() -> void {
                monitor.Enter()
                try {
                    sharedValue <- sharedValue + 1
                } finally {
                    monitor.Exit()
                }
            }
            threads <- []
            for i in 1..5 {
                thread <- spawn incrementWithMonitor()
                threads.Add(thread)
            }
            for thread in threads {
                thread.Wait()
            }
            result <- sharedValue
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_SpinLock_HandlesSpinLock()
    {
        // Arrange
        var code = @"
            spinLock <- threading.SpinLock()
            criticalSectionData <- 0
            func criticalSection() -> void {
                spinLock.Acquire()
                criticalSectionData <- criticalSectionData + 1
                threading.Sleep(10) // Simulate work in critical section
                spinLock.Release()
            }
            threads <- []
            for i in 1..3 {
                thread <- spawn criticalSection()
                threads.Add(thread)
            }
            for thread in threads {
                thread.Wait()
            }
            result <- criticalSectionData
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_ReentrantLock_HandlesReentrantLocking()
    {
        // Arrange
        var code = @"
            reentrantLock <- threading.ReentrantLock()
            nestedCount <- 0
            func nestedFunction(depth:int) -> void {
                reentrantLock.Lock()
                nestedCount <- nestedCount + 1
                if depth < 3 {
                    nestedFunction(depth + 1)
                }
                reentrantLock.Unlock()
            }
            thread <- spawn nestedFunction(1)
            thread.Wait()
            result <- nestedCount
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_DeadlockDetection_HandlesPotentialDeadlocks()
    {
        // Arrange
        var code = @"
            lock1 <- threading.Mutex()
            lock2 <- threading.Mutex()
            deadlockDetected <- false
            func thread1() -> void {
                lock1.Lock()
                threading.Sleep(50)
                try {
                    // Try to acquire lock2 with timeout
                    if not lock2.TryLock(100) {
                        deadlockDetected <- true
                    } else {
                        lock2.Unlock()
                    }
                } finally {
                    lock1.Unlock()
                }
            }
            func thread2() -> void {
                lock2.Lock()
                threading.Sleep(50)
                try {
                    // Try to acquire lock1 with timeout
                    if not lock1.TryLock(100) {
                        deadlockDetected <- true
                    } else {
                        lock1.Unlock()
                    }
                } finally {
                    lock2.Unlock()
                }
            }
            t1 <- spawn thread1()
            t2 <- spawn thread2()
            t1.Wait()
            t2.Wait()
            result <- deadlockDetected
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        // Should detect potential deadlock
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_ThreadLocal_HandlesThreadLocalStorage()
    {
        // Arrange
        var code = @"
            threadLocal <- threading.ThreadLocal()
            func worker(id:int) -> void {
                threadLocal.Set(""thread-"" + id.ToStr())
                threading.Sleep(50)
                result <- threadLocal.Get()
            }
            threads <- []
            results <- []
            for i in 1..3 {
                thread <- spawn worker(i)
                threads.Add(thread)
            }
            for thread in threads {
                thread.Wait()
            }
            result <- ""thread-local completed""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("thread-local completed", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_ThreadPool_HandlesThreadPool()
    {
        // Arrange
        var code = @"
            threadPool <- threading.ThreadPool(3)
            workCompleted <- 0
            func workItem(id:int) -> void {
                threading.Sleep(20)
                workCompleted <- workCompleted + 1
            }
            // Submit work items to thread pool
            futures <- []
            for i in 1..10 {
                future <- threadPool.Submit(() -> workItem(i))
                futures.Add(future)
            }
            // Wait for all work to complete
            for future in futures {
                future.Wait()
            }
            threadPool.Shutdown()
            result <- workCompleted
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(10, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_Timer_HandlesTimedCallbacks()
    {
        // Arrange
        var code = @"
            timerExecuted <- false
            timerCallback <- () -> {
                timerExecuted <- true
            }
            timer <- threading.Timer(100, timerCallback) // Execute after 100ms
            threading.Sleep(150) // Wait for timer to execute
            timer.Stop()
            result <- timerExecuted
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_ThreadJoin_HandlesThreadJoining()
    {
        // Arrange
        var code = @"
            func longRunningTask() -> string {
                threading.Sleep(100)
                return ""task completed""
            }
            thread <- spawn longRunningTask()
            // Wait for thread to complete with timeout
            if thread.Wait(200) {
                result <- thread.GetResult()
            } else {
                result <- ""timeout""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("task completed", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_Priority_HandlesThreadPriority()
    {
        // Arrange
        var code = @"
            highPriorityCompleted <- false
            lowPriorityCompleted <- false
            func highPriorityTask() -> void {
                threading.Sleep(50)
                highPriorityCompleted <- true
            }
            func lowPriorityTask() -> void {
                threading.Sleep(100)
                lowPriorityCompleted <- true
            }
            highThread <- spawn highPriorityTask()
            lowThread <- spawn lowPriorityTask()
            highThread.SetPriority(threading.ThreadPriority.HIGH)
            lowThread.SetPriority(threading.ThreadPriority.LOW)
            highThread.Wait()
            lowThread.Wait()
            result <- highPriorityCompleted and lowPriorityCompleted
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void ThreadSync_ComplexSynchronization_HandlesComplexScenario()
    {
        // Arrange
        var code = @"
            // Producer-Consumer with multiple producers and consumers
            queue <- []
            queueMutex <- threading.Mutex()
            queueNotFull <- threading.ConditionVariable()
            queueNotEmpty <- threading.ConditionVariable()
            queueSize <- 10
            itemsProduced <- 0
            itemsConsumed <- 0
            func producer(id:int) -> void {
                for i in 1..5 {
                    queueMutex.Lock()
                    while queue.Count >= queueSize {
                        queueNotFull.Wait(queueMutex)
                    }
                    queue.Add(""P"" + id.ToStr() + ""-I"" + i.ToStr())
                    itemsProduced <- itemsProduced + 1
                    queueNotEmpty.Signal()
                    queueMutex.Unlock()
                    threading.Sleep(10)
                }
            }
            func consumer(id:int) -> void {
                for i in 1..5 {
                    queueMutex.Lock()
                    while queue.Count = 0 {
                        queueNotEmpty.Wait(queueMutex)
                    }
                    item <- queue[0]
                    queue.Remove(0)
                    itemsConsumed <- itemsConsumed + 1
                    queueNotFull.Signal()
                    queueMutex.Unlock()
                    threading.Sleep(15)
                }
            }
            // Start producers and consumers
            threads <- []
            for i in 1..2 {
                threads.Add(spawn producer(i))
                threads.Add(spawn consumer(i))
            }
            for thread in threads {
                thread.Wait()
            }
            result <- itemsProduced.ToStr() + ""-"" + itemsConsumed.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("10-10", ((StringLangValue)result).Value);
    }
}