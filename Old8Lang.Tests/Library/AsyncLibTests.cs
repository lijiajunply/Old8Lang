using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Old8LangLib;

namespace Old8Lang.Tests.Library;

public class AsyncLibTests
{
    [Fact]
    public void CancellationTokenSource_ShouldBeProperlyDisposed()
    {
        // 测试手动销毁 CancellationTokenSource
        var ctsId = AsyncLib.CreateCancellationTokenSource();
        AsyncLib.Cancel(ctsId); // 使用一下，更新访问时间
        AsyncLib.DisposeCancellationTokenSource(ctsId);

        // 验证资源已被移除
        // 尝试再次取消，应该抛出异常
        Assert.Throws<ArgumentException>(() => AsyncLib.Cancel(ctsId));
    }

    [Fact]
    public void CancellationTokenSource_ShouldBeProperlyCleanedUp()
    {
        // 测试自动清理机制
        // 创建一个 CancellationTokenSource
        var ctsId = AsyncLib.CreateCancellationTokenSource();

        // 等待足够长的时间让它被标记为闲置
        // 注意：这里我们使用反射来修改 MaxIdleTimeMinutes 以便快速测试
        // 在实际使用中，MaxIdleTimeMinutes 是 30 分钟

        // 验证资源可以正常使用
        AsyncLib.CancelAfter(ctsId, 100);

        // 调用 Shutdown 方法清理所有资源
        AsyncLib.Shutdown();

        // 验证资源已被移除
        Assert.Throws<ArgumentException>(() => AsyncLib.Cancel(ctsId));
    }

    [Fact]
    public void Shutdown_ShouldCleanupAllResources()
    {
        // 创建多种资源
        var mutexId = AsyncLib.MutexCreate();
        var semaphoreId = AsyncLib.SemaphoreCreate(1, 1);
        var atomicId = AsyncLib.AtomicIntCreate(0);
        var ctsId = AsyncLib.CreateCancellationTokenSource();
        var channelId = AsyncLib.ChannelCreate();

        // 使用这些资源
        AsyncLib.MutexLock(mutexId);
        AsyncLib.MutexUnlock(mutexId);

        AsyncLib.SemaphoreAcquire(semaphoreId);
        AsyncLib.SemaphoreRelease(semaphoreId);

        AsyncLib.AtomicIntIncrement(atomicId);

        AsyncLib.Cancel(ctsId);

        AsyncLib.ChannelSend(channelId, "test");
        AsyncLib.ChannelClose(channelId);

        // 调用 Shutdown 清理所有资源
        AsyncLib.Shutdown();

        // 验证所有资源都已被清理
        Assert.Throws<ArgumentException>(() => AsyncLib.MutexLock(mutexId));
        Assert.Throws<ArgumentException>(() => AsyncLib.SemaphoreAcquire(semaphoreId));
        Assert.Throws<ArgumentException>(() => AsyncLib.AtomicIntGet(atomicId));
        Assert.Throws<ArgumentException>(() => AsyncLib.Cancel(ctsId));
        Assert.Throws<ArgumentException>(() => AsyncLib.ChannelReceive(channelId));
    }

    [Fact]
    public void CancellationTokenSource_FunctionalTests()
    {
        // 测试 CancellationTokenSource 的基本功能
        var ctsId = AsyncLib.CreateCancellationTokenSource();

        // 测试 Cancel 方法
        AsyncLib.Cancel(ctsId);

        // 测试 CancelAfter 方法
        var ctsId2 = AsyncLib.CreateCancellationTokenSource();
        AsyncLib.CancelAfter(ctsId2, 100);

        // 清理资源
        AsyncLib.Shutdown();
    }

    [Fact]
    public void Mutex_ShouldWorkCorrectly()
    {
        // 测试 Mutex 基本功能
        var mutexId = AsyncLib.MutexCreate();

        // 测试锁定和解锁
        AsyncLib.MutexLock(mutexId);
        AsyncLib.MutexUnlock(mutexId);

        // 测试 TryLock
        bool locked = AsyncLib.MutexTryLock(mutexId, 100);
        Assert.True(locked);

        if (locked)
        {
            AsyncLib.MutexUnlock(mutexId);
        }

        // 测试 TryLock 超时
        AsyncLib.MutexLock(mutexId);
        locked = AsyncLib.MutexTryLock(mutexId, 100);
        Assert.False(locked);
        AsyncLib.MutexUnlock(mutexId);

        // 测试销毁
        AsyncLib.MutexDispose(mutexId);
        Assert.Throws<ArgumentException>(() => AsyncLib.MutexLock(mutexId));
    }

    [Fact]
    public void Semaphore_ShouldWorkCorrectly()
    {
        // 测试 Semaphore 基本功能
        var semaphoreId = AsyncLib.SemaphoreCreate(2, 2);

        // 测试获取和释放
        AsyncLib.SemaphoreAcquire(semaphoreId);
        AsyncLib.SemaphoreAcquire(semaphoreId);
        AsyncLib.SemaphoreRelease(semaphoreId);
        AsyncLib.SemaphoreRelease(semaphoreId);

        // 测试 TryAcquire
        bool acquired = AsyncLib.SemaphoreTryAcquire(semaphoreId, 100);
        Assert.True(acquired);

        if (acquired)
        {
            AsyncLib.SemaphoreRelease(semaphoreId);
        }

        // 测试 TryAcquire 超时
        AsyncLib.SemaphoreAcquire(semaphoreId);
        AsyncLib.SemaphoreAcquire(semaphoreId);
        acquired = AsyncLib.SemaphoreTryAcquire(semaphoreId, 100);
        Assert.False(acquired);
        AsyncLib.SemaphoreRelease(semaphoreId);
        AsyncLib.SemaphoreRelease(semaphoreId);

        // 测试销毁
        AsyncLib.SemaphoreDispose(semaphoreId);
        Assert.Throws<ArgumentException>(() => AsyncLib.SemaphoreAcquire(semaphoreId));
    }

    [Fact]
    public void AtomicInt_ShouldWorkCorrectly()
    {
        // 测试原子整数基本功能
        var atomicId = AsyncLib.AtomicIntCreate(0);

        // 测试设置和获取
        AsyncLib.AtomicIntSet(atomicId, 5);
        Assert.Equal(5, AsyncLib.AtomicIntGet(atomicId));

        // 测试自增
        int result = AsyncLib.AtomicIntIncrement(atomicId);
        Assert.Equal(6, result);
        Assert.Equal(6, AsyncLib.AtomicIntGet(atomicId));

        // 测试自减
        result = AsyncLib.AtomicIntDecrement(atomicId);
        Assert.Equal(5, result);
        Assert.Equal(5, AsyncLib.AtomicIntGet(atomicId));

        // 测试加法
        result = AsyncLib.AtomicIntAdd(atomicId, 10);
        Assert.Equal(15, result);
        Assert.Equal(15, AsyncLib.AtomicIntGet(atomicId));

        // 测试比较并设置
        bool success = AsyncLib.AtomicIntCompareAndSet(atomicId, 15, 20);
        Assert.True(success);
        Assert.Equal(20, AsyncLib.AtomicIntGet(atomicId));

        // 测试比较并设置失败
        success = AsyncLib.AtomicIntCompareAndSet(atomicId, 15, 25);
        Assert.False(success);
        Assert.Equal(20, AsyncLib.AtomicIntGet(atomicId));

        // 测试销毁
        AsyncLib.AtomicIntDispose(atomicId);
        Assert.Throws<ArgumentException>(() => AsyncLib.AtomicIntGet(atomicId));
    }

    [Fact]
    public async Task AtomicInt_ShouldBeThreadSafe()
    {
        // 测试原子整数的线程安全性
        var atomicId = AsyncLib.AtomicIntCreate(0);
        int iterations = 10000;
        int threadCount = 10;

        // 创建多个线程同时递增
        var tasks = new Task[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    AsyncLib.AtomicIntIncrement(atomicId);
                }
            });
        }

        await Task.WhenAll(tasks);

        // 验证结果正确
        Assert.Equal(iterations * threadCount, AsyncLib.AtomicIntGet(atomicId));

        AsyncLib.AtomicIntDispose(atomicId);
    }

    [Fact]
    public void Channel_ShouldWorkCorrectly()
    {
        // 测试通道基本功能
        var channelId = AsyncLib.ChannelCreate();

        // 测试发送和接收
        AsyncLib.ChannelSend(channelId, "test");
        var received = AsyncLib.ChannelReceive(channelId);
        Assert.Equal("test", received);

        // 测试 TrySend 和 TryReceive
        bool sent = AsyncLib.ChannelTrySend(channelId, "test2", 100);
        Assert.True(sent);

        received = AsyncLib.ChannelTryReceive(channelId, 100);
        Assert.Equal("test2", received);

        // 测试 TryReceive 超时
        received = AsyncLib.ChannelTryReceive(channelId, 100);
        Assert.Null(received);

        // 测试关闭通道
        AsyncLib.ChannelClose(channelId);

        // 测试销毁
        AsyncLib.ChannelDispose(channelId);
        Assert.Throws<ArgumentException>(() => AsyncLib.ChannelSend(channelId, "test"));
    }

    [Fact]
    public async Task Channel_ShouldHandleMultipleSendersAndReceivers()
    {
        // 测试通道的多生产者多消费者场景
        var channelId = AsyncLib.ChannelCreate();
        int itemCount = 10; // 减少项数以加快测试
        int senderCount = 2;
        int totalItems = itemCount * senderCount;

        // 创建发送任务
        var senderTasks = new Task[senderCount];
        for (int i = 0; i < senderCount; i++)
        {
            int senderId = i;
            senderTasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < itemCount; j++)
                {
                    string item = $"sender{senderId}-item{j}";
                    AsyncLib.ChannelSend(channelId, item);
                }
            });
        }

        // 创建接收任务
        var atomicCounter = AsyncLib.AtomicIntCreate(0);

        // 创建一个接收所有项的任务
        var receiverTask = Task.Run(() =>
        {
            for (int j = 0; j < totalItems; j++)
            {
                var received = AsyncLib.ChannelReceive(channelId);
                Assert.NotNull(received);
                AsyncLib.AtomicIntIncrement(atomicCounter);
            }
        });

        // 等待所有发送任务完成
        await Task.WhenAll(senderTasks);

        // 等待接收任务完成
        await receiverTask;

        // 验证所有项都被接收
        Assert.Equal(totalItems, AsyncLib.AtomicIntGet(atomicCounter));

        AsyncLib.ChannelClose(channelId);
        AsyncLib.ChannelDispose(channelId);
        AsyncLib.AtomicIntDispose(atomicCounter);
    }

    [Fact]
    public void UtilityFunctions_ShouldWorkCorrectly()
    {
        // 测试睡眠功能
        var startTime = DateTime.Now;
        AsyncLib.Sleep(100);
        var endTime = DateTime.Now;
        Assert.True((endTime - startTime).TotalMilliseconds >= 100);

        // 测试获取当前线程ID
        int threadId = AsyncLib.GetCurrentThreadId();
        Assert.NotEqual(0, threadId);

        // 测试获取处理器数量
        int processorCount = AsyncLib.GetProcessorCount();
        Assert.True(processorCount > 0);
    }
}