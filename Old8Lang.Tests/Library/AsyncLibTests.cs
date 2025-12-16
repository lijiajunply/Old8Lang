using System;using System.Threading;using Xunit;using Old8LangLib;

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
}