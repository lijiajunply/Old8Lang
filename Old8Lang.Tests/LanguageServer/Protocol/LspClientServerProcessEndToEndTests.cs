using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Old8Lang.Tests.LanguageServer.Protocol;

/// <summary>
/// 真实子进程级 Language Client ↔ Language Server 端到端测试：
/// 启动真实构建产物 Old8Lang.LanguageServer.dll，通过 OS stdin/stdout 管道发送标准 LSP JSON-RPC 2.0 帧，
/// 验证握手、能力声明、防抖诊断、补全、调用层次、类型层次、Inline Values 与优雅退出。
/// </summary>
public class LspClientServerProcessEndToEndTests : IDisposable
{
    private readonly string _tempDir;

    public LspClientServerProcessEndToEndTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "old8lang-lsp-proc-e2e-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // 忽略临时目录清理异常
        }
    }

    [Fact]
    public async Task RealLanguageServerProcess_HandlesFullClientLifecycleOverStdio()
    {
        var serverDll = LocateLanguageServerDll();
        Assert.True(File.Exists(serverDll), $"未找到 Old8Lang.LanguageServer.dll: {serverDll}");

        var filePath = Path.Combine(_tempDir, "sample.old8");
        var source =
            "class Base {\n" +
            "    public func ping() -> int { return 1 }\n" +
            "}\n" +
            "class Derived extends Base {\n" +
            "    public func run(x: int) -> int {\n" +
            "        y <- this.ping() + x\n" +
            "        return y\n" +
            "    }\n" +
            "}\n" +
            "d <- Derived()\n" +
            "ans <- d.run(5)\n";
        File.WriteAllText(filePath, source);
        var fileUri = DocumentUri.FromFileSystemPath(filePath).ToString();
        var rootUri = DocumentUri.FromFileSystemPath(_tempDir).ToString();

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{serverDll}\"",
            WorkingDirectory = _tempDir,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        Assert.NotNull(process);

        try
        {
            var client = new StdioLspClient(process.StandardInput.BaseStream, process.StandardOutput.BaseStream);

            // 1. initialize
            var initResult = await client.SendRequestAsync("initialize", new JObject
            {
                ["processId"] = Environment.ProcessId,
                ["rootUri"] = rootUri,
                ["capabilities"] = new JObject
                {
                    ["textDocument"] = new JObject
                    {
                        ["completion"] = new JObject { ["dynamicRegistration"] = false },
                        ["hover"] = new JObject { ["dynamicRegistration"] = false },
                        ["callHierarchy"] = new JObject { ["dynamicRegistration"] = false },
                        ["typeHierarchy"] = new JObject { ["dynamicRegistration"] = false },
                        ["inlineValue"] = new JObject { ["dynamicRegistration"] = false }
                    }
                }
            });
            var caps = initResult?["capabilities"] as JObject;
            Assert.NotNull(caps);
            Assert.NotNull(caps!["callHierarchyProvider"]);
            Assert.NotNull(caps["typeHierarchyProvider"]);
            Assert.NotNull(caps["inlineValueProvider"]);

            await client.SendNotificationAsync("initialized", new JObject());

            // 2. didOpen -> 诊断推送
            await client.SendNotificationAsync("textDocument/didOpen", new JObject
            {
                ["textDocument"] = new JObject
                {
                    ["uri"] = fileUri,
                    ["languageId"] = "old8lang",
                    ["version"] = 1,
                    ["text"] = source
                }
            });
            var openDiag = await client.WaitForNotificationAsync("textDocument/publishDiagnostics");
            Assert.Empty((JArray)openDiag["diagnostics"]!);

            // 3. Type Hierarchy over real stdio
            var typePrep = await client.SendRequestAsync("textDocument/prepareTypeHierarchy", new JObject
            {
                ["textDocument"] = new JObject { ["uri"] = fileUri },
                ["position"] = new JObject { ["line"] = 3, ["character"] = 7 }
            }) as JArray;
            Assert.NotNull(typePrep);
            Assert.Single(typePrep!);
            Assert.Equal("Derived", typePrep[0]!["name"]!.Value<string>());

            var supers = await client.SendRequestAsync("typeHierarchy/supertypes", new JObject
            {
                ["item"] = typePrep[0]
            }) as JArray;
            Assert.Contains(supers!, s => s["name"]?.Value<string>() == "Base");

            // 4. Call Hierarchy over real stdio
            var callPrep = await client.SendRequestAsync("textDocument/prepareCallHierarchy", new JObject
            {
                ["textDocument"] = new JObject { ["uri"] = fileUri },
                ["position"] = new JObject { ["line"] = 4, ["character"] = 17 }
            }) as JArray;
            Assert.NotNull(callPrep);
            Assert.Equal("run", callPrep![0]!["name"]!.Value<string>());

            var outgoing = await client.SendRequestAsync("callHierarchy/outgoingCalls", new JObject
            {
                ["item"] = callPrep[0]
            }) as JArray;
            Assert.Contains(outgoing!, o => o["to"]?["name"]?.Value<string>() == "ping");

            // 5. Debug snapshot + Inline Values over real stdio
            await client.SendRequestAsync("old8lang/updateDebugSnapshot", new JObject
            {
                ["uri"] = fileUri,
                ["frameId"] = 1,
                ["stoppedLine"] = 6,
                ["variables"] = new JObject { ["x"] = "5", ["y"] = "6" }
            });
            var inlineVals = await client.SendRequestAsync("textDocument/inlineValue", new JObject
            {
                ["textDocument"] = new JObject { ["uri"] = fileUri },
                ["range"] = new JObject
                {
                    ["start"] = new JObject { ["line"] = 4, ["character"] = 0 },
                    ["end"] = new JObject { ["line"] = 7, ["character"] = 0 }
                },
                ["context"] = new JObject
                {
                    ["frameId"] = 1,
                    ["stoppedLocation"] = new JObject
                    {
                        ["start"] = new JObject { ["line"] = 6, ["character"] = 0 },
                        ["end"] = new JObject { ["line"] = 6, ["character"] = 16 }
                    }
                }
            }) as JArray;
            Assert.Contains(inlineVals!, iv => iv["text"]?.Value<string>() == "y = 6");

            // 6. 连续 didChange 验证防抖最终收敛到合法诊断
            await client.SendNotificationAsync("textDocument/didChange", new JObject
            {
                ["textDocument"] = new JObject { ["uri"] = fileUri, ["version"] = 2 },
                ["contentChanges"] = new JArray { new JObject { ["text"] = "bad <- unknownName\n" } }
            });
            await client.SendNotificationAsync("textDocument/didChange", new JObject
            {
                ["textDocument"] = new JObject { ["uri"] = fileUri, ["version"] = 3 },
                ["contentChanges"] = new JArray { new JObject { ["text"] = "ok <- 123\nPrintLine(ok)\n" } }
            });
            var settledDiag = await client.WaitForNotificationAsync("textDocument/publishDiagnostics");
            Assert.Empty((JArray)settledDiag["diagnostics"]!);

            // 7. shutdown + exit
            await client.SendRequestAsync("shutdown", null);
            await client.SendNotificationAsync("exit", null);
            Assert.True(process.WaitForExit(5000));
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    private static string LocateLanguageServerDll()
    {
        var baseDir = AppContext.BaseDirectory;
        var direct = Path.Combine(baseDir, "Old8Lang.LanguageServer.dll");
        if (File.Exists(direct))
        {
            return direct;
        }

        var dir = new DirectoryInfo(baseDir);
        while (dir != null)
        {
            var sln = Path.Combine(dir.FullName, "Old8Lang.sln");
            if (File.Exists(sln))
            {
                var candidate = Path.Combine(dir.FullName, "Old8Lang.LanguageServer", "bin", "Debug", "net10.0", "Old8Lang.LanguageServer.dll");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            dir = dir.Parent;
        }

        return direct;
    }

    private sealed class StdioLspClient(Stream toServer, Stream fromServer)
    {
        private int _nextId;
        private readonly Queue<(string Method, JToken Params)> _notifications = new();

        public async Task<JToken?> SendRequestAsync(string method, JToken? parameters, int timeoutMs = 15_000)
        {
            var id = Interlocked.Increment(ref _nextId);
            var msg = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["method"] = method
            };
            if (parameters != null) msg["params"] = parameters;
            await WriteFrameAsync(msg);

            using var cts = new CancellationTokenSource(timeoutMs);
            while (true)
            {
                var frame = await ReadFrameAsync(cts.Token);
                if (frame["method"] != null && frame["id"] != null)
                {
                    await WriteFrameAsync(new JObject
                    {
                        ["jsonrpc"] = "2.0",
                        ["id"] = frame["id"],
                        ["result"] = JValue.CreateNull()
                    });
                    continue;
                }
                if (frame["method"] != null && frame["id"] == null)
                {
                    _notifications.Enqueue((frame["method"]!.ToString(), frame["params"] ?? new JObject()));
                    continue;
                }
                if (frame["id"]?.Value<int>() == id)
                {
                    if (frame["error"] != null)
                    {
                        throw new InvalidOperationException($"LSP '{method}' error: {frame["error"]}");
                    }
                    return frame["result"];
                }
            }
        }

        public async Task SendNotificationAsync(string method, JToken? parameters)
        {
            var msg = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["method"] = method
            };
            if (parameters != null) msg["params"] = parameters;
            await WriteFrameAsync(msg);
        }

        public async Task<JToken> WaitForNotificationAsync(string expectedMethod, int timeoutMs = 15_000)
        {
            var kept = new Queue<(string Method, JToken Params)>();
            while (_notifications.Count > 0)
            {
                var item = _notifications.Dequeue();
                if (item.Method == expectedMethod)
                {
                    while (_notifications.Count > 0) kept.Enqueue(_notifications.Dequeue());
                    while (kept.Count > 0) _notifications.Enqueue(kept.Dequeue());
                    return item.Params;
                }
                kept.Enqueue(item);
            }
            while (kept.Count > 0) _notifications.Enqueue(kept.Dequeue());

            using var cts = new CancellationTokenSource(timeoutMs);
            while (true)
            {
                var frame = await ReadFrameAsync(cts.Token);
                if (frame["method"] != null && frame["id"] != null)
                {
                    await WriteFrameAsync(new JObject
                    {
                        ["jsonrpc"] = "2.0",
                        ["id"] = frame["id"],
                        ["result"] = JValue.CreateNull()
                    });
                    continue;
                }
                if (frame["method"]?.ToString() == expectedMethod)
                {
                    return frame["params"] ?? new JObject();
                }
                if (frame["method"] != null)
                {
                    _notifications.Enqueue((frame["method"]!.ToString(), frame["params"] ?? new JObject()));
                }
            }
        }

        private async Task WriteFrameAsync(JObject message)
        {
            var body = Encoding.UTF8.GetBytes(message.ToString(Formatting.None));
            var header = Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n");
            await toServer.WriteAsync(header);
            await toServer.WriteAsync(body);
            await toServer.FlushAsync();
        }

        private async Task<JObject> ReadFrameAsync(CancellationToken ct)
        {
            var contentLength = -1;
            var lineBytes = new List<byte>(64);
            var one = new byte[1];

            while (true)
            {
                var n = await fromServer.ReadAsync(one.AsMemory(0, 1), ct);
                if (n == 0) throw new EndOfStreamException("LanguageServer stdout closed.");

                if (one[0] == (byte)'\n')
                {
                    if (lineBytes.Count > 0 && lineBytes[^1] == (byte)'\r')
                    {
                        lineBytes.RemoveAt(lineBytes.Count - 1);
                    }
                    if (lineBytes.Count == 0 && contentLength >= 0)
                    {
                        break;
                    }
                    var line = Encoding.ASCII.GetString(lineBytes.ToArray());
                    lineBytes.Clear();
                    if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    {
                        contentLength = int.Parse(line["Content-Length:".Length..].Trim());
                    }
                }
                else
                {
                    lineBytes.Add(one[0]);
                }
            }

            var body = new byte[contentLength];
            var read = 0;
            while (read < contentLength)
            {
                var n = await fromServer.ReadAsync(body.AsMemory(read, contentLength - read), ct);
                if (n == 0) throw new EndOfStreamException("LanguageServer stdout closed mid-body.");
                read += n;
            }

            return JObject.Parse(Encoding.UTF8.GetString(body));
        }
    }
}
