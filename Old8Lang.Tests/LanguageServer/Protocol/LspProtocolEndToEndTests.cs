using System.IO.Pipelines;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Hosting;
using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using Old8Lang.Modules.Official;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Old8Lang.Tests.LanguageServer.Protocol;

/// <summary>
/// 进程内真实 LSP JSON-RPC 协议级端到端测试。
/// 使用内存双向 Pipe 直接驱动与 Program.cs 相同配置的 OmniSharp LanguageServer，
/// 覆盖协议握手、能力协商、诊断推送、全部 18 个标准 LSP Handler 及全部 8 个自定义 Debug/Profiler Handler。
/// </summary>
public class LspProtocolEndToEndTests : IAsyncLifetime
{
    private readonly Pipe _clientToServer = new();
    private readonly Pipe _serverToClient = new();
    private OmniSharp.Extensions.LanguageServer.Server.LanguageServer? _server;
    private LspJsonRpcHarness? _client;
    private string _tempDir = string.Empty;
    private string _mainFilePath = string.Empty;
    private string _mainUri = string.Empty;
    private string _helperFilePath = string.Empty;

    private const string HelperSource =
        "func sharedSquare(n: int) -> int {\n" +
        "    return n * n\n" +
        "}\n";

    private const string MainSource =
        "import \"./helper.old8\"\n" +
        "\n" +
        "/// 动物基类\n" +
        "class Animal {\n" +
        "    public static kind <- \"animal\"\n" +
        "    public func speak() -> string {\n" +
        "        return \"...\"\n" +
        "    }\n" +
        "}\n" +
        "\n" +
        "class Dog extends Animal {\n" +
        "    public func bark(times: int) -> int {\n" +
        "        return times\n" +
        "    }\n" +
        "}\n" +
        "\n" +
        "func compute(a: int, b: int) -> int {\n" +
        "    sum <- a + b\n" +
        "    return sum\n" +
        "}\n" +
        "\n" +
        "dog <- Dog()\n" +
        "sound <- dog.speak()\n" +
        "val <- compute(1, 2)\n" +
        "sq <- sharedSquare(3)\n" +
        "bad <- unknownSymbol\n";

    public async Task InitializeAsync()
    {
        var hostBuilder = new Old8HostBuilder();
        OfficialModules.RegisterAll(hostBuilder);
        hostBuilder.Build().InstallAsDefault();
        GlobalFunctionInitializer.EnsureInitialized();

        _tempDir = Path.Combine(Path.GetTempPath(), "old8lang-lsp-proto-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _helperFilePath = Path.Combine(_tempDir, "helper.old8");
        _mainFilePath = Path.Combine(_tempDir, "main.old8");
        File.WriteAllText(_helperFilePath, HelperSource);
        File.WriteAllText(_mainFilePath, MainSource);
        _mainUri = DocumentUri.FromFileSystemPath(_mainFilePath).ToString();

        var serverTask = OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options =>
            options
                .WithInput(_clientToServer.Reader)
                .WithOutput(_serverToClient.Writer)
                .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Error))
                .WithServices(services =>
                {
                    services.AddSingleton<DocumentManager>();
                    services.AddSingleton<WorkspaceIndex>();
                    services.AddSingleton<DebugProfilerService>();
                    services.AddSingleton<FormattingService>();
                })
                // 全部 18 个标准 LSP Handler（与 Program.cs 保持一致）
                .WithHandler<TextDocumentSyncHandler>()
                .WithHandler<CompletionHandler>()
                .WithHandler<DefinitionHandler>()
                .WithHandler<ReferencesHandler>()
                .WithHandler<RenameHandler>()
                .WithHandler<HoverHandler>()
                .WithHandler<DocumentSymbolHandler>()
                .WithHandler<SignatureHelpHandler>()
                .WithHandler<DocumentFormattingHandler>()
                .WithHandler<CodeActionHandler>()
                .WithHandler<WorkspaceSymbolHandler>()
                .WithHandler<DocumentHighlightHandler>()
                .WithHandler<FoldingRangeHandler>()
                .WithHandler<SemanticTokensHandler>()
                .WithHandler<DocumentLinkHandler>()
                .WithHandler<CodeLensHandler>()
                .WithHandler<SelectionRangeHandler>()
                .WithHandler<InlayHintHandler>()
                .WithHandler<InlineValuesHandler>()
                .WithHandler<CallHierarchyHandler>()
                .WithHandler<TypeHierarchyHandler>()
                // 全部 9 个自定义 Debug & Profiler Handler（与 Program.cs 保持一致）
                .WithHandler<StartProfilingHandler>()
                .WithHandler<StopProfilingHandler>()
                .WithHandler<GetProfilingStatusHandler>()
                .WithHandler<StartDebuggingHandler>()
                .WithHandler<StopDebuggingHandler>()
                .WithHandler<AddBreakpointHandler>()
                .WithHandler<RemoveBreakpointHandler>()
                .WithHandler<DebugControlHandler>()
                .WithHandler<UpdateDebugSnapshotHandler>());

        _client = new LspJsonRpcHarness(_clientToServer.Writer, _serverToClient.Reader);

        // 发送标准 initialize 请求
        var rootUri = DocumentUri.FromFileSystemPath(_tempDir).ToString();
        var initResponse = await _client.SendRequestAsync("initialize", new JObject
        {
            ["processId"] = Environment.ProcessId,
            ["rootUri"] = rootUri,
            ["capabilities"] = new JObject
            {
                ["textDocument"] = new JObject
                {
                    ["completion"] = new JObject { ["dynamicRegistration"] = false },
                    ["hover"] = new JObject { ["dynamicRegistration"] = false },
                    ["definition"] = new JObject { ["dynamicRegistration"] = false },
                    ["references"] = new JObject { ["dynamicRegistration"] = false },
                    ["rename"] = new JObject { ["dynamicRegistration"] = false },
                    ["documentSymbol"] = new JObject { ["dynamicRegistration"] = false },
                    ["signatureHelp"] = new JObject { ["dynamicRegistration"] = false },
                    ["formatting"] = new JObject { ["dynamicRegistration"] = false },
                    ["rangeFormatting"] = new JObject { ["dynamicRegistration"] = false },
                    ["codeAction"] = new JObject { ["dynamicRegistration"] = false },
                    ["documentHighlight"] = new JObject { ["dynamicRegistration"] = false },
                    ["foldingRange"] = new JObject { ["dynamicRegistration"] = false },
                    ["semanticTokens"] = new JObject { ["dynamicRegistration"] = false },
                    ["documentLink"] = new JObject { ["dynamicRegistration"] = false },
                    ["codeLens"] = new JObject { ["dynamicRegistration"] = false },
                    ["selectionRange"] = new JObject { ["dynamicRegistration"] = false },
                    ["inlayHint"] = new JObject { ["dynamicRegistration"] = false },
                    ["inlineValue"] = new JObject { ["dynamicRegistration"] = false },
                    ["callHierarchy"] = new JObject { ["dynamicRegistration"] = false },
                    ["typeHierarchy"] = new JObject { ["dynamicRegistration"] = false }
                },
                ["workspace"] = new JObject
                {
                    ["symbol"] = new JObject { ["dynamicRegistration"] = false }
                }
            }
        });

        Assert.NotNull(initResponse["capabilities"]);

        await _client.SendNotificationAsync("initialized", new JObject());
        _server = await serverTask;

        // 模拟 Program.cs 启动后的工作区绑定
        var workspaceIndex = _server.Services.GetRequiredService<WorkspaceIndex>();
        var documentManager = _server.Services.GetRequiredService<DocumentManager>();
        documentManager.AttachWorkspaceIndex(workspaceIndex);
        workspaceIndex.SetRoots([_tempDir]);
    }

    public async Task DisposeAsync()
    {
        if (_client != null)
        {
            try
            {
                await _client.SendRequestAsync("shutdown", null);
                await _client.SendNotificationAsync("exit", null);
            }
            catch
            {
                // 忽略退出时的管道关闭竞态
            }
        }

        await _clientToServer.Writer.CompleteAsync();
        await _serverToClient.Writer.CompleteAsync();
        _server?.Dispose();

        try
        {
            if (!string.IsNullOrEmpty(_tempDir) && Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // 忽略临时目录清理错误
        }
    }

    [Fact]
    public async Task AllRegisteredLspHandlers_RespondCorrectlyOverJsonRpcProtocol()
    {
        Assert.NotNull(_client);

        // 1. textDocument/didOpen -> 验证服务端推送 textDocument/publishDiagnostics
        await _client!.SendNotificationAsync("textDocument/didOpen", new JObject
        {
            ["textDocument"] = new JObject
            {
                ["uri"] = _mainUri,
                ["languageId"] = "old8lang",
                ["version"] = 1,
                ["text"] = MainSource
            }
        });

        var diagNotification = await _client.WaitForNotificationAsync("textDocument/publishDiagnostics");
        var diagnostics = (JArray)diagNotification["diagnostics"]!;
        Assert.Contains(diagnostics, d => d["message"]!.ToString().Contains("unknownSymbol"));
        // 跨文件导入的 sharedSquare 不应误报未定义
        Assert.DoesNotContain(diagnostics, d => d["message"]!.ToString().Contains("sharedSquare"));

        // 2. textDocument/completion (继承成员 dog.)
        var completionResult = await _client.SendRequestAsync("textDocument/completion", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri },
            ["position"] = new JObject { ["line"] = 22, ["character"] = 13 } // sound <- dog.|speak()
        });
        var completionItems = completionResult is JArray arr ? arr : (JArray)completionResult!["items"]!;
        Assert.Contains(completionItems, i => i["label"]?.ToString() == "speak");
        Assert.Contains(completionItems, i => i["label"]?.ToString() == "bark");

        // 3. textDocument/definition (跨文件跳转到 helper.old8 的 sharedSquare)
        var definitionResult = await _client.SendRequestAsync("textDocument/definition", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri },
            ["position"] = new JObject { ["line"] = 24, ["character"] = 8 } // sq <- sharedSquare(3)
        });
        var defLocations = definitionResult is JArray defArr ? defArr : new JArray(definitionResult!);
        Assert.Contains(defLocations, l => l["uri"]!.ToString().EndsWith("helper.old8", StringComparison.OrdinalIgnoreCase));

        // 4. textDocument/references (compute 函数的声明与调用)
        var refsResult = await _client.SendRequestAsync("textDocument/references", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri },
            ["position"] = new JObject { ["line"] = 16, ["character"] = 6 }, // func compute
            ["context"] = new JObject { ["includeDeclaration"] = true }
        });
        Assert.True(((JArray)refsResult!).Count >= 2);

        // 5. textDocument/rename (重命名 compute)
        var renameResult = await _client.SendRequestAsync("textDocument/rename", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri },
            ["position"] = new JObject { ["line"] = 16, ["character"] = 6 },
            ["newName"] = "computeSum"
        });
        Assert.NotNull(renameResult!["changes"]);

        // 6. textDocument/hover (悬停在继承调用 dog.speak 上)
        var hoverResult = await _client.SendRequestAsync("textDocument/hover", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri },
            ["position"] = new JObject { ["line"] = 22, ["character"] = 14 }
        });
        Assert.Contains("speak", hoverResult!["contents"]!["value"]!.ToString());

        // 7. textDocument/documentSymbol
        var docSymbols = (JArray)(await _client.SendRequestAsync("textDocument/documentSymbol", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri }
        }))!;
        Assert.Contains(docSymbols, s => s["name"]?.ToString() == "Animal");
        Assert.Contains(docSymbols, s => s["name"]?.ToString() == "Dog");
        Assert.Contains(docSymbols, s => s["name"]?.ToString() == "compute");

        // 8. textDocument/signatureHelp (compute(1, |2))
        var sigHelp = await _client.SendRequestAsync("textDocument/signatureHelp", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri },
            ["position"] = new JObject { ["line"] = 23, ["character"] = 18 }
        });
        Assert.Equal(1, sigHelp!["activeParameter"]!.Value<int>());

        // 9. textDocument/formatting & textDocument/rangeFormatting
        var fmtResult = await _client.SendRequestAsync("textDocument/formatting", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri },
            ["options"] = new JObject { ["tabSize"] = 4, ["insertSpaces"] = true }
        });
        Assert.NotNull(fmtResult);

        var rangeFmtResult = await _client.SendRequestAsync("textDocument/rangeFormatting", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri },
            ["range"] = new JObject
            {
                ["start"] = new JObject { ["line"] = 16, ["character"] = 0 },
                ["end"] = new JObject { ["line"] = 19, ["character"] = 1 }
            },
            ["options"] = new JObject { ["tabSize"] = 4, ["insertSpaces"] = true }
        });
        Assert.NotNull(rangeFmtResult);

        // 10. textDocument/codeAction (针对 unknownSymbol 的快速修复)
        var codeActions = (JArray)(await _client.SendRequestAsync("textDocument/codeAction", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri },
            ["range"] = new JObject
            {
                ["start"] = new JObject { ["line"] = 25, ["character"] = 7 },
                ["end"] = new JObject { ["line"] = 25, ["character"] = 20 }
            },
            ["context"] = new JObject
            {
                ["diagnostics"] = new JArray
                {
                    new JObject
                    {
                        ["range"] = new JObject
                        {
                            ["start"] = new JObject { ["line"] = 25, ["character"] = 7 },
                            ["end"] = new JObject { ["line"] = 25, ["character"] = 20 }
                        },
                        ["message"] = "未定义的符号 'unknownSymbol'"
                    }
                }
            }
        }))!;
        Assert.Contains(codeActions, a => a["title"]!.ToString().Contains("unknownSymbol"));

        // 11. workspace/symbol (包含未打开的 helper.old8 中的 sharedSquare)
        var wsSymbols = (JArray)(await _client.SendRequestAsync("workspace/symbol", new JObject
        {
            ["query"] = "sharedSquare"
        }))!;
        Assert.Contains(wsSymbols, s => s["name"]?.ToString() == "sharedSquare");

        // 12. textDocument/documentHighlight (局部 sum 变量)
        var highlights = (JArray)(await _client.SendRequestAsync("textDocument/documentHighlight", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri },
            ["position"] = new JObject { ["line"] = 17, ["character"] = 5 } // sum
        }))!;
        Assert.Equal(2, highlights.Count);

        // 13. textDocument/foldingRange
        var folds = (JArray)(await _client.SendRequestAsync("textDocument/foldingRange", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri }
        }))!;
        Assert.NotEmpty(folds);

        // 14. textDocument/semanticTokens/full
        var semTokens = await _client.SendRequestAsync("textDocument/semanticTokens/full", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri }
        });
        Assert.NotEmpty((JArray)semTokens!["data"]!);

        // 15. textDocument/documentLink (import "./helper.old8")
        var links = (JArray)(await _client.SendRequestAsync("textDocument/documentLink", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri }
        }))!;
        var link = Assert.Single(links);
        Assert.Contains("helper.old8", link["target"]!.ToString());

        // 16. textDocument/codeLens
        var lenses = (JArray)(await _client.SendRequestAsync("textDocument/codeLens", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri }
        }))!;
        Assert.NotEmpty(lenses);

        // 17. textDocument/selectionRange
        var selectionRanges = (JArray)(await _client.SendRequestAsync("textDocument/selectionRange", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri },
            ["positions"] = new JArray { new JObject { ["line"] = 17, ["character"] = 5 } }
        }))!;
        Assert.Single(selectionRanges);

        // 18. textDocument/inlayHint (compute(1, 2) -> a:, b:)
        var inlayHints = (JArray)(await _client.SendRequestAsync("textDocument/inlayHint", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri },
            ["range"] = new JObject
            {
                ["start"] = new JObject { ["line"] = 20, ["character"] = 0 },
                ["end"] = new JObject { ["line"] = 26, ["character"] = 0 }
            }
        }))!;
        Assert.Contains(inlayHints, h => h["label"]!.ToString() == "a:");
        Assert.Contains(inlayHints, h => h["label"]!.ToString() == "b:");

        // 19-21. 自定义 Profiler RPC: startProfiling / getProfilingStatus / stopProfiling
        var profStarted = await _client.SendRequestAsync("old8lang/startProfiling", new JObject
        {
            ["uri"] = _mainUri,
            ["sessionName"] = "ProtoSession",
            ["executionMode"] = "interpreter"
        });
        Assert.True(profStarted!["isProfiling"]!.Value<bool>());

        var profStatus = await _client.SendRequestAsync("old8lang/getProfilingStatus", new JObject
        {
            ["uri"] = _mainUri
        });
        Assert.True(profStatus!["isProfiling"]!.Value<bool>());

        var profReport = await _client.SendRequestAsync("old8lang/stopProfiling", new JObject
        {
            ["uri"] = _mainUri
        });
        Assert.Equal("markdown", profReport!["format"]!.ToString());

        // 22-26. 自定义 Debug RPC: startDebugging / addBreakpoint / removeBreakpoint / debugControl / stopDebugging
        var dbgStarted = await _client.SendRequestAsync("old8lang/startDebugging", new JObject
        {
            ["uri"] = _mainUri
        });
        Assert.True(dbgStarted!["isDebugging"]!.Value<bool>());

        var bpAdded = await _client.SendRequestAsync("old8lang/addBreakpoint", new JObject
        {
            ["uri"] = _mainUri,
            ["line"] = 18
        });
        Assert.True(bpAdded!.Value<bool>());

        var bpRemoved = await _client.SendRequestAsync("old8lang/removeBreakpoint", new JObject
        {
            ["uri"] = _mainUri,
            ["line"] = 18
        });
        Assert.True(bpRemoved!.Value<bool>());

        var dbgCtrl = await _client.SendRequestAsync("old8lang/debugControl", new JObject
        {
            ["uri"] = _mainUri,
            ["command"] = "continue"
        });
        Assert.True(dbgCtrl!.Value<bool>());

        var dbgStopped = await _client.SendRequestAsync("old8lang/stopDebugging", new JObject
        {
            ["uri"] = _mainUri
        });
        Assert.False(dbgStopped!["isDebugging"]!.Value<bool>());

        // Call Hierarchy: prepare + incomingCalls + outgoingCalls (compute at line 16)
        var callPrep = await _client.SendRequestAsync("textDocument/prepareCallHierarchy", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri },
            ["position"] = new JObject { ["line"] = 16, ["character"] = 6 }
        });
        var callItems = Assert.IsType<JArray>(callPrep);
        Assert.Single(callItems);
        Assert.Equal("compute", callItems[0]!["name"]!.Value<string>());

        var incomingCalls = await _client.SendRequestAsync("callHierarchy/incomingCalls", new JObject
        {
            ["item"] = callItems[0]
        });
        Assert.NotEmpty(Assert.IsType<JArray>(incomingCalls));

        var outgoingCalls = await _client.SendRequestAsync("callHierarchy/outgoingCalls", new JObject
        {
            ["item"] = callItems[0]
        });
        Assert.IsType<JArray>(outgoingCalls);

        // Type Hierarchy: prepare + supertypes + subtypes (Dog at line 10)
        var typePrep = await _client.SendRequestAsync("textDocument/prepareTypeHierarchy", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri },
            ["position"] = new JObject { ["line"] = 10, ["character"] = 7 }
        });
        var typeItems = Assert.IsType<JArray>(typePrep);
        Assert.Single(typeItems);
        Assert.Equal("Dog", typeItems[0]!["name"]!.Value<string>());

        var supers = await _client.SendRequestAsync("typeHierarchy/supertypes", new JObject
        {
            ["item"] = typeItems[0]
        });
        var superArr = Assert.IsType<JArray>(supers);
        Assert.Contains(superArr, s => s["name"]?.Value<string>() == "Animal");

        var animalItem = superArr.First(s => s["name"]?.Value<string>() == "Animal");
        var subs = await _client.SendRequestAsync("typeHierarchy/subtypes", new JObject
        {
            ["item"] = animalItem
        });
        Assert.Contains(Assert.IsType<JArray>(subs), s => s["name"]?.Value<string>() == "Dog");

        // Inline Values + old8lang/updateDebugSnapshot
        var snapUpdated = await _client.SendRequestAsync("old8lang/updateDebugSnapshot", new JObject
        {
            ["uri"] = _mainUri,
            ["frameId"] = 1,
            ["stoppedLine"] = 18,
            ["variables"] = new JObject { ["a"] = "1", ["b"] = "2", ["sum"] = "3" }
        });
        Assert.True(snapUpdated!.Value<bool>());

        var inlineValues = await _client.SendRequestAsync("textDocument/inlineValue", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri },
            ["range"] = new JObject
            {
                ["start"] = new JObject { ["line"] = 16, ["character"] = 0 },
                ["end"] = new JObject { ["line"] = 19, ["character"] = 0 }
            },
            ["context"] = new JObject
            {
                ["frameId"] = 1,
                ["stoppedLocation"] = new JObject
                {
                    ["start"] = new JObject { ["line"] = 18, ["character"] = 0 },
                    ["end"] = new JObject { ["line"] = 18, ["character"] = 20 }
                }
            }
        });
        var inlineArr = Assert.IsType<JArray>(inlineValues);
        Assert.Contains(inlineArr, iv => iv["text"]?.Value<string>() == "sum = 3");

        // textDocument/didChange 修复错误 -> 推送清空后的诊断
        await _client.SendNotificationAsync("textDocument/didChange", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri, ["version"] = 2 },
            ["contentChanges"] = new JArray
            {
                new JObject { ["text"] = "x <- 1\nPrintLine(x)\n" }
            }
        });
        var updatedDiag = await _client.WaitForNotificationAsync("textDocument/publishDiagnostics");
        Assert.Empty((JArray)updatedDiag["diagnostics"]!);

        // textDocument/didSave & textDocument/didClose
        await _client.SendNotificationAsync("textDocument/didSave", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri }
        });
        await _client.SendNotificationAsync("textDocument/didClose", new JObject
        {
            ["textDocument"] = new JObject { ["uri"] = _mainUri }
        });
    }

    /// <summary>
    /// 轻量级标准 LSP JSON-RPC 2.0 客户端，通过 Content-Length 帧直接与服务端 Pipe 通信。
    /// </summary>
    private sealed class LspJsonRpcHarness(PipeWriter writer, PipeReader reader)
    {
        private readonly Stream _outputStream = writer.AsStream(leaveOpen: true);
        private readonly Stream _inputStream = reader.AsStream(leaveOpen: true);
        private readonly Queue<(string Method, JToken Params)> _bufferedNotifications = new();
        private int _nextId;

        public async Task<JToken?> SendRequestAsync(string method, JToken? parameters, int timeoutMs = 10_000)
        {
            var id = Interlocked.Increment(ref _nextId);
            var payload = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["method"] = method
            };
            if (parameters != null)
            {
                payload["params"] = parameters;
            }

            await WriteFrameAsync(payload);

            using var cts = new CancellationTokenSource(timeoutMs);
            while (true)
            {
                var msg = await ReadFrameAsync(cts.Token);

                // 服务端发来的反向请求（如 client/registerCapability）：回复空成功结果避免阻塞服务端
                if (msg["method"] != null && msg["id"] != null)
                {
                    await WriteFrameAsync(new JObject
                    {
                        ["jsonrpc"] = "2.0",
                        ["id"] = msg["id"],
                        ["result"] = JValue.CreateNull()
                    });
                    continue;
                }

                // 服务端通知（如 textDocument/publishDiagnostics）：入队留给 WaitForNotificationAsync
                if (msg["method"] != null && msg["id"] == null)
                {
                    _bufferedNotifications.Enqueue((msg["method"]!.ToString(), msg["params"] ?? new JObject()));
                    continue;
                }

                if (msg["id"] != null && msg["id"]!.Value<int>() == id)
                {
                    if (msg["error"] != null)
                    {
                        throw new InvalidOperationException($"LSP request '{method}' failed: {msg["error"]}");
                    }

                    return msg["result"];
                }
            }
        }

        public async Task SendNotificationAsync(string method, JToken? parameters)
        {
            var payload = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["method"] = method
            };
            if (parameters != null)
            {
                payload["params"] = parameters;
            }

            await WriteFrameAsync(payload);
        }

        public async Task<JToken> WaitForNotificationAsync(string expectedMethod, int timeoutMs = 10_000)
        {
            var remaining = new Queue<(string Method, JToken Params)>();
            while (_bufferedNotifications.Count > 0)
            {
                var item = _bufferedNotifications.Dequeue();
                if (item.Method == expectedMethod)
                {
                    while (remaining.Count > 0) _bufferedNotifications.Enqueue(remaining.Dequeue());
                    return item.Params;
                }

                remaining.Enqueue(item);
            }

            while (remaining.Count > 0) _bufferedNotifications.Enqueue(remaining.Dequeue());

            using var cts = new CancellationTokenSource(timeoutMs);
            while (true)
            {
                var msg = await ReadFrameAsync(cts.Token);
                if (msg["method"] != null && msg["id"] != null)
                {
                    await WriteFrameAsync(new JObject
                    {
                        ["jsonrpc"] = "2.0",
                        ["id"] = msg["id"],
                        ["result"] = JValue.CreateNull()
                    });
                    continue;
                }

                if (msg["method"]?.ToString() == expectedMethod)
                {
                    return msg["params"] ?? new JObject();
                }

                if (msg["method"] != null)
                {
                    _bufferedNotifications.Enqueue((msg["method"]!.ToString(), msg["params"] ?? new JObject()));
                }
            }
        }

        private async Task WriteFrameAsync(JObject message)
        {
            var json = message.ToString(Formatting.None);
            var bodyBytes = Encoding.UTF8.GetBytes(json);
            var headerBytes = Encoding.ASCII.GetBytes($"Content-Length: {bodyBytes.Length}\r\n\r\n");

            await _outputStream.WriteAsync(headerBytes);
            await _outputStream.WriteAsync(bodyBytes);
            await _outputStream.FlushAsync();
        }

        private async Task<JObject> ReadFrameAsync(CancellationToken ct)
        {
            var contentLength = -1;
            while (true)
            {
                var headerLine = await ReadAsciiLineAsync(ct);
                if (headerLine.Length == 0)
                {
                    break;
                }

                if (headerLine.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    contentLength = int.Parse(headerLine["Content-Length:".Length..].Trim());
                }
            }

            if (contentLength <= 0)
            {
                throw new InvalidOperationException("Invalid LSP frame: missing Content-Length header.");
            }

            var buffer = new byte[contentLength];
            var read = 0;
            while (read < contentLength)
            {
                var n = await _inputStream.ReadAsync(buffer.AsMemory(read, contentLength - read), ct);
                if (n == 0)
                {
                    throw new EndOfStreamException("LSP server stream closed unexpectedly.");
                }

                read += n;
            }

            var text = Encoding.UTF8.GetString(buffer);
            return JObject.Parse(text);
        }

        private async Task<string> ReadAsciiLineAsync(CancellationToken ct)
        {
            var bytes = new List<byte>();
            var single = new byte[1];
            while (true)
            {
                var n = await _inputStream.ReadAsync(single, ct);
                if (n == 0)
                {
                    throw new EndOfStreamException("LSP server stream closed while reading header.");
                }

                if (single[0] == (byte)'\n')
                {
                    if (bytes.Count > 0 && bytes[^1] == (byte)'\r')
                    {
                        bytes.RemoveAt(bytes.Count - 1);
                    }

                    return Encoding.ASCII.GetString(bytes.ToArray());
                }

                bytes.Add(single[0]);
            }
        }
    }
}
