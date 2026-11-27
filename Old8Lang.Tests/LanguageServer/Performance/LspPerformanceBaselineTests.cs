using System.Diagnostics;
using System.Text;
using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Performance;

/// <summary>
/// LSP 大文件解析、Token 按行索引查询与实时诊断防抖延迟的性能基线与粗阈值烟雾测试。
/// - <c>Category=PerformanceSmoke</c>：粗阈值（实测 5~10 倍）随默认档运行，拦截灾难性退化；
/// - <c>Category=Performance</c>：在 <c>performance.runsettings</c> 性能档下运行并输出实测基线数据。
/// </summary>
public class LspPerformanceBaselineTests(ITestOutputHelper output)
{
    private static string GenerateLargeOld8Document(int targetLines)
    {
        var sb = new StringBuilder(targetLines * 40);
        var blockIndex = 0;
        var currentLines = 0;

        while (currentLines < targetLines)
        {
            sb.AppendLine($"class Service_{blockIndex} {{");
            sb.AppendLine($"    public count_{blockIndex}: int <- {blockIndex}");
            sb.AppendLine($"    public func compute_{blockIndex}(a: int, b: int) -> int {{");
            sb.AppendLine($"        local_{blockIndex} <- a + b + this.count_{blockIndex}");
            sb.AppendLine($"        return local_{blockIndex}");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine($"func helper_{blockIndex}(n: int) -> int {{");
            sb.AppendLine($"    svc <- Service_{blockIndex}()");
            sb.AppendLine($"    return svc.compute_{blockIndex}(n, 1)");
            sb.AppendLine("}");
            currentLines += 11;
            blockIndex++;
        }

        return sb.ToString();
    }

    [Fact]
    [Trait("Category", "PerformanceSmoke")]
    public void LargeFile2000Lines_ParseAndTokenIndexLookup_MeetsSmokeBudget()
    {
        var code = GenerateLargeOld8Document(2000);
        var dm = new DocumentManager();
        const string uri = "file:///perf-smoke-2000.old8";

        // 预热 JIT
        dm.UpdateDocument("file:///warmup.old8", "x <- 1\n");

        var swParse = Stopwatch.StartNew();
        var doc = dm.UpdateDocument(uri, code);
        swParse.Stop();

        Assert.NotNull(doc.Tokens);
        Assert.NotNull(doc.Ast);
        Assert.True(doc.TokenIndex.IsMonotonic);

        // 1,000 次跨全文位置查找：按行索引应远低于粗阈值
        var sampleTokens = doc.Tokens!
            .Where(t => t.Value.Length > 0 && t.Line > 0)
            .ToArray();
        var step = Math.Max(1, sampleTokens.Length / 1000);

        var swIndex = Stopwatch.StartNew();
        var hits = 0;
        for (var i = 0; i < sampleTokens.Length; i += step)
        {
            var t = sampleTokens[i];
            if (doc.TokenIndex.FindTokenAtPosition(t.Line - 1, t.Column) != null)
            {
                hits++;
            }
        }
        swIndex.Stop();

        output.WriteLine(
            $"[Smoke 2000 lines] tokens={doc.Tokens.Count}, parse+semantic={swParse.ElapsedMilliseconds}ms, " +
            $"indexedLookups={hits} in {swIndex.Elapsed.TotalMilliseconds:F2}ms");

        Assert.True(hits >= 500);
        Assert.True(swParse.ElapsedMilliseconds < 5000,
            $"2000 行文档解析+语义分析耗时过长: {swParse.ElapsedMilliseconds}ms");
        Assert.True(swIndex.ElapsedMilliseconds < 200,
            $"按行索引查找耗时过长: {swIndex.ElapsedMilliseconds}ms");
    }

    [Fact]
    [Trait("Category", "PerformanceSmoke")]
    public async Task RealtimeDiagnosticBurst_NonBlockingScheduleAndCoalesce_MeetsSmokeBudget()
    {
        var baseCode = GenerateLargeOld8Document(1000);
        var dm = new DocumentManager();
        using var scheduler = new DiagnosticsScheduler(dm, new DiagnosticsSchedulerOptions
        {
            DebounceDelay = TimeSpan.FromMilliseconds(40),
            RunInBackground = true
        });

        const string uri = "file:///perf-debounce-burst.old8";
        var publishCount = 0;
        var tcs = new TaskCompletionSource<DocumentParseResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        var swSchedule = Stopwatch.StartNew();
        for (var i = 0; i < 15; i++)
        {
            var edited = baseCode + $"burst_{i} <- {i}\n";
            var isLast = i == 14;
            await scheduler.ScheduleAsync(uri, edited, (_, r) =>
            {
                Interlocked.Increment(ref publishCount);
                if (isLast)
                {
                    tcs.TrySetResult(r);
                }
            });
        }
        swSchedule.Stop();

        var swTotal = Stopwatch.StartNew();
        var finalDoc = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        swTotal.Stop();

        output.WriteLine(
            $"[Smoke Debounce 1000 lines x15 edits] scheduleAll={swSchedule.Elapsed.TotalMilliseconds:F2}ms, " +
            $"settledAfterLastEdit={swTotal.ElapsedMilliseconds}ms, publishCount={publishCount}");

        Assert.NotNull(finalDoc);
        Assert.True(swSchedule.ElapsedMilliseconds < 500,
            $"15 次防抖 ScheduleAsync 应非阻塞快速返回，实测 {swSchedule.ElapsedMilliseconds}ms");
        Assert.True(publishCount <= 3,
            $"防抖应合并连续编辑，实际发布了 {publishCount} 次");
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task LargeFile5000Lines_FullBaseline_ParseIndexCompletionAndDiagnostics()
    {
        var dm = new DocumentManager();
        var wi = new WorkspaceIndex(dm);
        dm.AttachWorkspaceIndex(wi);
        var completionHandler = new CompletionHandler(dm, wi);

        // 预热
        dm.UpdateDocument("file:///perf-warmup.old8", "func w() -> int { return 1 }\n");

        foreach (var lineTarget in new[] { 1000, 2000, 5000 })
        {
            var code = GenerateLargeOld8Document(lineTarget);
            var uri = $"file:///perf-baseline-{lineTarget}.old8";

            var parseTimes = new List<double>();
            DocumentParseResult? doc = null;
            for (var run = 0; run < 3; run++)
            {
                var sw = Stopwatch.StartNew();
                doc = dm.UpdateDocument(uri, code);
                sw.Stop();
                parseTimes.Add(sw.Elapsed.TotalMilliseconds);
            }

            Assert.NotNull(doc?.Tokens);
            var tokens = doc!.Tokens!;
            var sampleTokens = tokens.Where(t => t.Value.Length > 0 && t.Line > 0).ToArray();
            var step = Math.Max(1, sampleTokens.Length / 500);

            var swLinear = Stopwatch.StartNew();
            for (var i = 0; i < sampleTokens.Length; i += step)
            {
                var t = sampleTokens[i];
                _ = SymbolFinder.FindTokenAtPosition(tokens, t.Line - 1, t.Column);
            }
            swLinear.Stop();

            var swIndexed = Stopwatch.StartNew();
            for (var i = 0; i < sampleTokens.Length; i += step)
            {
                var t = sampleTokens[i];
                _ = doc.TokenIndex.FindTokenAtPosition(t.Line - 1, t.Column);
            }
            swIndexed.Stop();

            var swCompletion = Stopwatch.StartNew();
            var completion = await completionHandler.Handle(new CompletionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
                Position = new Position(lineTarget / 2, 8)
            }, CancellationToken.None);
            swCompletion.Stop();

            Assert.NotNull(completion);
            var meanParseMs = parseTimes.Average();
            var speedup = swLinear.Elapsed.TotalMilliseconds / Math.Max(0.001, swIndexed.Elapsed.TotalMilliseconds);

            output.WriteLine(
                $"[Baseline {lineTarget} lines] tokens={tokens.Count}, meanParse+Diag={meanParseMs:F1}ms, " +
                $"linear500={swLinear.Elapsed.TotalMilliseconds:F2}ms, indexed500={swIndexed.Elapsed.TotalMilliseconds:F2}ms " +
                $"(speedup={speedup:F1}x), completion={swCompletion.Elapsed.TotalMilliseconds:F2}ms");

            Assert.True(swIndexed.Elapsed.TotalMilliseconds <= swLinear.Elapsed.TotalMilliseconds + 5.0);
        }
    }
}
