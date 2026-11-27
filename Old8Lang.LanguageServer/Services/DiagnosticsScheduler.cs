using System.Collections.Concurrent;
using Old8Lang.LanguageServer.Models;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 实时诊断调度器配置
/// </summary>
public sealed class DiagnosticsSchedulerOptions
{
    /// <summary>编辑防抖延迟（默认 150ms；设置为 TimeSpan.Zero 时不等待）</summary>
    public TimeSpan DebounceDelay { get; set; } = TimeSpan.FromMilliseconds(150);

    /// <summary>是否在后台线程池执行解析与诊断发布（默认 true）</summary>
    public bool RunInBackground { get; set; } = true;
}

/// <summary>
/// 按文档 URI 隔离的实时诊断防抖与后台调度器。
/// 连续快速编辑时取消先前尚未完成的分析任务，仅对最终稳定版本执行解析并发布诊断，
/// 避免大文件编辑阻塞 LSP 消息循环。
/// </summary>
public sealed class DiagnosticsScheduler(
    DocumentManager documentManager,
    DiagnosticsSchedulerOptions? options = null) : IDisposable
{
    private sealed class PendingState
    {
        public CancellationTokenSource? Cts;
        public long Generation;
    }

    private readonly DiagnosticsSchedulerOptions _options = options ?? new DiagnosticsSchedulerOptions();
    private readonly ConcurrentDictionary<string, PendingState> _states = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();
    private bool _disposed;

    /// <summary>当前生效的防抖延迟</summary>
    public TimeSpan DebounceDelay => _options.DebounceDelay;

    /// <summary>是否在后台线程运行</summary>
    public bool RunInBackground => _options.RunInBackground;

    /// <summary>
    /// 调度一次防抖的后台诊断更新（用于 didChange）。
    /// 立即暂存最新文本并取消该 URI 上之前的挂起任务，避免阻塞消息循环。
    /// </summary>
    public Task ScheduleAsync(
        string uri,
        string text,
        Action<string, DocumentParseResult> publishDiagnostics,
        CancellationToken requestCancellation = default)
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        // 先以 O(1) 暂存最新文本，保证若补全/悬停立即到达能按需拿到最新内容
        var version = documentManager.StageDocumentText(uri, text);

        if (!_options.RunInBackground && _options.DebounceDelay <= TimeSpan.Zero)
        {
            CancelInternal(uri);
            var immediateResult = documentManager.UpdateDocument(uri, text, requestCancellation);
            publishDiagnostics(uri, immediateResult);
            return Task.CompletedTask;
        }

        CancellationTokenSource cts;
        long generation;
        lock (_gate)
        {
            var state = _states.GetOrAdd(uri, _ => new PendingState());
            CancelAndDispose(state.Cts);
            cts = new CancellationTokenSource();
            state.Cts = cts;
            generation = ++state.Generation;
        }

        var token = cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                if (_options.DebounceDelay > TimeSpan.Zero)
                {
                    await Task.Delay(_options.DebounceDelay, token).ConfigureAwait(false);
                }

                if (token.IsCancellationRequested || !IsLatestGeneration(uri, generation))
                {
                    return;
                }

                var result = documentManager.EnsureUpToDate(uri, token);
                if (result == null || token.IsCancellationRequested || !IsLatestGeneration(uri, generation))
                {
                    return;
                }

                // 确认版本号未落后于最新暂存版本
                if (result.Version < version)
                {
                    return;
                }

                publishDiagnostics(uri, result);
            }
            catch (OperationCanceledException)
            {
                // 被后续编辑或文档关闭取消，正常忽略
            }
            catch
            {
                // 后台诊断异常不影响服务器生命周期
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 立即取消该 URI 的挂起防抖并同步解析发布（用于 didOpen / didSave）。
    /// </summary>
    public DocumentParseResult? FlushNow(
        string uri,
        string? text,
        Action<string, DocumentParseResult> publishDiagnostics,
        CancellationToken cancellationToken = default)
    {
        CancelInternal(uri);

        DocumentParseResult? result;
        if (text != null)
        {
            result = documentManager.UpdateDocument(uri, text, cancellationToken);
        }
        else
        {
            result = documentManager.EnsureUpToDate(uri, cancellationToken);
        }

        if (result != null)
        {
            publishDiagnostics(uri, result);
        }

        return result;
    }

    /// <summary>
    /// 取消指定 URI 的挂起防抖任务（用于 didClose）。
    /// </summary>
    public void Cancel(string uri)
    {
        CancelInternal(uri);
    }

    private void CancelInternal(string uri)
    {
        lock (_gate)
        {
            if (_states.TryGetValue(uri, out var state))
            {
                state.Generation++;
                CancelAndDispose(state.Cts);
                state.Cts = null;
            }
        }
    }

    private bool IsLatestGeneration(string uri, long generation)
    {
        lock (_gate)
        {
            return _states.TryGetValue(uri, out var state) && state.Generation == generation;
        }
    }

    private static void CancelAndDispose(CancellationTokenSource? cts)
    {
        if (cts == null)
        {
            return;
        }

        try
        {
            cts.Cancel();
        }
        catch
        {
            // 忽略已释放竞态
        }
        finally
        {
            cts.Dispose();
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var state in _states.Values)
            {
                CancelAndDispose(state.Cts);
                state.Cts = null;
            }
            _states.Clear();
        }
    }
}
