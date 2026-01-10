using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

/// <summary>
/// 并发原语补全测试
/// 测试所有 Old8Lang 并发原语函数的补全功能
/// </summary>
public class CompletionHandler_ConcurrencyTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    /// <summary>
    /// 测试 Mutex 相关函数补全
    /// </summary>
    [Fact]
    public async Task TestMutexFunctionsCompletion()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var functions = result.Items.Where(item => item.Kind == CompletionItemKind.Function).ToList();

        // Mutex 相关函数：5个
        var mutexFunctions = new[] {
            "MutexCreate",
            "MutexLock",
            "MutexTryLock",
            "MutexUnlock",
            "MutexDispose"
        };

        foreach (var func in mutexFunctions)
        {
            Assert.Contains(functions, item => item.Label == func);
            _output.WriteLine($"✓ 找到 Mutex 函数: {func}");
        }
    }

    /// <summary>
    /// 测试 Semaphore 相关函数补全
    /// </summary>
    [Fact]
    public async Task TestSemaphoreFunctionsCompletion()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var functions = result.Items.Where(item => item.Kind == CompletionItemKind.Function).ToList();

        // Semaphore 相关函数：5个
        var semaphoreFunctions = new[] {
            "SemaphoreCreate",
            "SemaphoreAcquire",
            "SemaphoreTryAcquire",
            "SemaphoreRelease",
            "SemaphoreDispose"
        };

        foreach (var func in semaphoreFunctions)
        {
            Assert.Contains(functions, item => item.Label == func);
            _output.WriteLine($"✓ 找到 Semaphore 函数: {func}");
        }
    }

    /// <summary>
    /// 测试 AtomicInt 相关函数补全
    /// </summary>
    [Fact]
    public async Task TestAtomicIntFunctionsCompletion()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var functions = result.Items.Where(item => item.Kind == CompletionItemKind.Function).ToList();

        // AtomicInt 相关函数：8个
        var atomicIntFunctions = new[] {
            "AtomicIntCreate",
            "AtomicIntGet",
            "AtomicIntSet",
            "AtomicIntIncrement",
            "AtomicIntDecrement",
            "AtomicIntAdd",
            "AtomicIntCompareAndSet",
            "AtomicIntDispose"
        };

        foreach (var func in atomicIntFunctions)
        {
            Assert.Contains(functions, item => item.Label == func);
            _output.WriteLine($"✓ 找到 AtomicInt 函数: {func}");
        }
    }

    /// <summary>
    /// 测试 Channel 相关函数补全
    /// </summary>
    [Fact]
    public async Task TestChannelFunctionsCompletion()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var functions = result.Items.Where(item => item.Kind == CompletionItemKind.Function).ToList();

        // Channel 相关函数：8个
        var channelFunctions = new[] {
            "ChannelCreate",
            "ChannelCreateBounded",
            "ChannelSend",
            "ChannelTrySend",
            "ChannelReceive",
            "ChannelTryReceive",
            "ChannelClose",
            "ChannelDispose"
        };

        foreach (var func in channelFunctions)
        {
            Assert.Contains(functions, item => item.Label == func);
            _output.WriteLine($"✓ 找到 Channel 函数: {func}");
        }
    }

    /// <summary>
    /// 测试 ReadWriteLock 相关函数补全
    /// </summary>
    [Fact]
    public async Task TestReadWriteLockFunctionsCompletion()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var functions = result.Items.Where(item => item.Kind == CompletionItemKind.Function).ToList();

        // ReadWriteLock 相关函数：8个
        var rwLockFunctions = new[] {
            "ReadWriteLockCreate",
            "ReadLockAcquire",
            "ReadLockRelease",
            "WriteLockAcquire",
            "WriteLockRelease",
            "ReadLockTryAcquire",
            "WriteLockTryAcquire",
            "ReadWriteLockDispose"
        };

        foreach (var func in rwLockFunctions)
        {
            Assert.Contains(functions, item => item.Label == func);
            _output.WriteLine($"✓ 找到 ReadWriteLock 函数: {func}");
        }
    }

    /// <summary>
    /// 测试 CountDownLatch 相关函数补全
    /// </summary>
    [Fact]
    public async Task TestCountDownLatchFunctionsCompletion()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var functions = result.Items.Where(item => item.Kind == CompletionItemKind.Function).ToList();

        // CountDownLatch 相关函数：6个
        var latchFunctions = new[] {
            "CountDownLatchCreate",
            "CountDownLatchCountDown",
            "CountDownLatchWait",
            "CountDownLatchWaitTimeout",
            "CountDownLatchGetCount",
            "CountDownLatchDispose"
        };

        foreach (var func in latchFunctions)
        {
            Assert.Contains(functions, item => item.Label == func);
            _output.WriteLine($"✓ 找到 CountDownLatch 函数: {func}");
        }
    }

    /// <summary>
    /// 测试 CyclicBarrier 相关函数补全
    /// </summary>
    [Fact]
    public async Task TestCyclicBarrierFunctionsCompletion()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var functions = result.Items.Where(item => item.Kind == CompletionItemKind.Function).ToList();

        // CyclicBarrier 相关函数：6个
        var barrierFunctions = new[] {
            "CyclicBarrierCreate",
            "CyclicBarrierAwait",
            "CyclicBarrierAwaitTimeout",
            "CyclicBarrierGetParticipantCount",
            "CyclicBarrierGetWaitingCount",
            "CyclicBarrierDispose"
        };

        foreach (var func in barrierFunctions)
        {
            Assert.Contains(functions, item => item.Label == func);
            _output.WriteLine($"✓ 找到 CyclicBarrier 函数: {func}");
        }
    }

    /// <summary>
    /// 测试 CancellationTokenSource 相关函数补全
    /// </summary>
    [Fact]
    public async Task TestCancellationTokenSourceFunctionsCompletion()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var functions = result.Items.Where(item => item.Kind == CompletionItemKind.Function).ToList();

        // CancellationTokenSource 相关函数：4个
        var ctsFunctions = new[] {
            "CreateCancellationTokenSource",
            "Cancel",
            "CancelAfter",
            "DisposeCancellationTokenSource"
        };

        foreach (var func in ctsFunctions)
        {
            Assert.Contains(functions, item => item.Label == func);
            _output.WriteLine($"✓ 找到 CancellationTokenSource 函数: {func}");
        }
    }

    /// <summary>
    /// 测试工具函数补全（Sleep, GetCurrentThreadId, GetProcessorCount）
    /// </summary>
    [Fact]
    public async Task TestUtilityFunctionsCompletion()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var functions = result.Items.Where(item => item.Kind == CompletionItemKind.Function).ToList();

        // 工具函数：3个
        var utilityFunctions = new[] {
            "Sleep",
            "GetCurrentThreadId",
            "GetProcessorCount"
        };

        foreach (var func in utilityFunctions)
        {
            Assert.Contains(functions, item => item.Label == func);
            _output.WriteLine($"✓ 找到工具函数: {func}");
        }
    }

    /// <summary>
    /// 测试所有并发原语函数的完整性
    /// </summary>
    [Fact]
    public async Task TestAllConcurrencyFunctionsPresent()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var functions = result.Items.Where(item => item.Kind == CompletionItemKind.Function).ToList();

        // 所有并发原语函数（总共50个）
        var allConcurrencyFunctions = new[] {
            // Mutex (5)
            "MutexCreate", "MutexLock", "MutexTryLock", "MutexUnlock", "MutexDispose",
            // Semaphore (5)
            "SemaphoreCreate", "SemaphoreAcquire", "SemaphoreTryAcquire", "SemaphoreRelease", "SemaphoreDispose",
            // AtomicInt (8)
            "AtomicIntCreate", "AtomicIntGet", "AtomicIntSet", "AtomicIntIncrement",
            "AtomicIntDecrement", "AtomicIntAdd", "AtomicIntCompareAndSet", "AtomicIntDispose",
            // Channel (8)
            "ChannelCreate", "ChannelCreateBounded", "ChannelSend", "ChannelTrySend",
            "ChannelReceive", "ChannelTryReceive", "ChannelClose", "ChannelDispose",
            // ReadWriteLock (8)
            "ReadWriteLockCreate", "ReadLockAcquire", "ReadLockRelease", "WriteLockAcquire",
            "WriteLockRelease", "ReadLockTryAcquire", "WriteLockTryAcquire", "ReadWriteLockDispose",
            // CountDownLatch (6)
            "CountDownLatchCreate", "CountDownLatchCountDown", "CountDownLatchWait",
            "CountDownLatchWaitTimeout", "CountDownLatchGetCount", "CountDownLatchDispose",
            // CyclicBarrier (6)
            "CyclicBarrierCreate", "CyclicBarrierAwait", "CyclicBarrierAwaitTimeout",
            "CyclicBarrierGetParticipantCount", "CyclicBarrierGetWaitingCount", "CyclicBarrierDispose",
            // CancellationTokenSource (4)
            "CreateCancellationTokenSource", "Cancel", "CancelAfter", "DisposeCancellationTokenSource",
            // Utility (3)
            "Sleep", "GetCurrentThreadId", "GetProcessorCount"
        };

        _output.WriteLine($"总共应有 {allConcurrencyFunctions.Length} 个并发原语函数");
        _output.WriteLine($"实际找到 {functions.Count} 个函数补全");

        var foundFunctions = functions.Select(f => f.Label).ToHashSet();
        var missingFunctions = allConcurrencyFunctions.Where(f => !foundFunctions.Contains(f)).ToList();

        if (missingFunctions.Any())
        {
            _output.WriteLine("\n缺少的并发原语函数:");
            foreach (var missing in missingFunctions)
            {
                _output.WriteLine($"  - {missing}");
            }
        }

        // 验证所有并发原语函数都存在
        foreach (var func in allConcurrencyFunctions)
        {
            Assert.Contains(functions, item => item.Label == func);
        }
    }

    /// <summary>
    /// 测试并发原语函数补全的详细信息
    /// </summary>
    [Fact]
    public async Task TestConcurrencyFunctionCompletionDetails()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // 检查几个代表性函数的详细信息
        var mutexCreate = result.Items.FirstOrDefault(item => item.Label == "MutexCreate");
        var channelSend = result.Items.FirstOrDefault(item => item.Label == "ChannelSend");
        var atomicIntAdd = result.Items.FirstOrDefault(item => item.Label == "AtomicIntAdd");

        if (mutexCreate != null)
        {
            Assert.Equal(CompletionItemKind.Function, mutexCreate.Kind);
            Assert.Equal(InsertTextFormat.Snippet, mutexCreate.InsertTextFormat);
            Assert.Contains("($0)", mutexCreate.InsertText!);

            _output.WriteLine("MutexCreate 补全详情:");
            _output.WriteLine($"  Label: {mutexCreate.Label}");
            _output.WriteLine($"  Detail: {mutexCreate.Detail}");
            _output.WriteLine($"  InsertText: {mutexCreate.InsertText}");
        }

        if (channelSend != null)
        {
            _output.WriteLine("\nChannelSend 补全详情:");
            _output.WriteLine($"  Label: {channelSend.Label}");
            _output.WriteLine($"  Detail: {channelSend.Detail}");
            _output.WriteLine($"  InsertText: {channelSend.InsertText}");
        }

        if (atomicIntAdd != null)
        {
            _output.WriteLine("\nAtomicIntAdd 补全详情:");
            _output.WriteLine($"  Label: {atomicIntAdd.Label}");
            _output.WriteLine($"  Detail: {atomicIntAdd.Detail}");
            _output.WriteLine($"  InsertText: {atomicIntAdd.InsertText}");
        }
    }
}
