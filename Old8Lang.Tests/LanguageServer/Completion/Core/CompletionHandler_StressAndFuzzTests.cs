using System.Text;
using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Old8Lang.Tests.LanguageServer.Completion.Core;

/// <summary>
/// 补全并发安全性与确定性模糊测试：验证多线程交错编辑/补全无竞态异常，
/// 以及畸形/截断/Unicode/深层嵌套输入下解析与补全零崩溃。
/// </summary>
public class CompletionHandler_StressAndFuzzTests
{
    [Fact]
    public async Task ConcurrentDocumentUpdatesAndCompletions_RemainThreadSafe()
    {
        var dm = new DocumentManager();
        var wi = new WorkspaceIndex(dm);
        dm.AttachWorkspaceIndex(wi);
        var handler = new CompletionHandler(dm, wi);

        var uris = Enumerable.Range(0, 4).Select(i => $"file:///concurrent_{i}.old8").ToArray();
        foreach (var uri in uris)
        {
            dm.UpdateDocument(uri, "func init() -> void { x <- 1 }\n");
        }

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, 8).Select(workerId => Task.Run(async () =>
        {
            try
            {
                for (var iter = 0; iter < 15; iter++)
                {
                    var uri = uris[(workerId + iter) % uris.Length];
                    if (workerId % 2 == 0)
                    {
                        var code =
                            $"class C_{workerId}_{iter} {{\n" +
                            $"    public v_{iter}: int <- {iter}\n" +
                            $"    public func m_{iter}() -> int {{ return this.v_{iter} }}\n" +
                            $"}}\n" +
                            $"obj <- C_{workerId}_{iter}()\n" +
                            "res <- obj.\n";
                        dm.UpdateDocument(uri, code);
                    }
                    else
                    {
                        var list = await handler.Handle(new CompletionParams
                        {
                            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
                            Position = new Position(5, 11)
                        }, CancellationToken.None);
                        Assert.NotNull(list);
                    }
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task DeterministicFuzzInputs_NeverCrashDocumentManagerOrCompletion()
    {
        var rng = new Random(20261127);
        var tokens = new[]
        {
            "func", "class", "extends", "implements", "with", "public", "private", "protected",
            "static", "if", "elif", "else", "for", "while", "match", "case", "return",
            "<-", "->", ".", ":", ",", "(", ")", "{", "}", "[", "]", "|", "&", "?",
            "x", "y", "foo", "Bar", "this", "super", "123", "\"str\"", "$\"a={x}\"",
            "/// doc\n", "// comment\n", "\n", " ", "中文变量", "??", "extern", "import"
        };

        var dm = new DocumentManager();
        var handler = new CompletionHandler(dm);

        for (var caseIndex = 0; caseIndex < 60; caseIndex++)
        {
            var sb = new StringBuilder();
            var tokenCount = rng.Next(1, 50);
            for (var t = 0; t < tokenCount; t++)
            {
                sb.Append(tokens[rng.Next(tokens.Length)]);
                if (rng.Next(3) == 0) sb.Append(' ');
                if (rng.Next(8) == 0) sb.Append('\n');
            }

            var code = sb.ToString();
            var uri = $"file:///fuzz_{caseIndex}.old8";
            var doc = dm.UpdateDocument(uri, code);
            Assert.NotNull(doc);

            var lineCount = Math.Max(1, code.Count(c => c == '\n') + 1);
            var line = rng.Next(0, lineCount + 2);
            var col = rng.Next(0, 40);

            var completion = await handler.Handle(new CompletionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
                Position = new Position(line, col)
            }, CancellationToken.None);
            Assert.NotNull(completion);

            // 同步覆盖 SymbolFinder 与按行索引查找，确认零异常
            _ = SymbolFinder.FindSymbolAtPosition(doc, line, col);
        }
    }
}
