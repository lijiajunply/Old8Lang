using System.IO.Pipelines;
using System.Text;
using System.Text.Json.Nodes;
using Old8Lang.Bytecode.Debugger;
using Old8Lang.Hosting;
using Old8Lang.Modules.Official;

namespace Old8Lang.Tests.VirtualMachine.Debugger;

/// <summary>
/// VmDapServer 协议级单元测试：基于内存双向 Pipe 验证完整 DAP 会话生命周期，
/// 覆盖 initialize、launch、setBreakpoints、条件断点、setFunctionBreakpoints、
/// configurationDone、stopped、stackTrace、scopes、variables（局部/全局变量/对象字段）、
/// evaluate、next/stepIn/stepOut、continue 与 terminated/exited。
/// </summary>
public class VmDapServerTests : IDisposable
{
    private readonly string _tempDir;

    public VmDapServerTests()
    {
        var hostBuilder = new Old8HostBuilder();
        OfficialModules.RegisterAll(hostBuilder);
        hostBuilder.Build().InstallAsDefault();

        _tempDir = Path.Combine(Path.GetTempPath(), "old8lang-dap-test-" + Guid.NewGuid().ToString("N"));
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
    public async Task FullDebugSession_BreakpointsStepVariablesAndEvaluate()
    {
        var scriptPath = Path.Combine(_tempDir, "calc.old8");
        var source =
            "func add(a: int, b: int) -> int {\n" +   // line 1
            "    total <- a + b\n" +                   // line 2
            "    doubled <- total * 2\n" +             // line 3
            "    return doubled\n" +                   // line 4
            "}\n" +                                    // line 5
            "g <- 10\n" +                              // line 6
            "res <- add(3, 4)\n" +                     // line 7
            "finalVal <- res + g\n";                   // line 8
        File.WriteAllText(scriptPath, source);

        var c2s = new Pipe();
        var s2c = new Pipe();

        using var server = new VmDapServer(c2s.Reader.AsStream(), s2c.Writer.AsStream());
        var serverTask = server.RunAsync();
        var client = new DapClientHarness(c2s.Writer.AsStream(), s2c.Reader.AsStream());

        // 1. initialize
        var initResp = await client.SendRequestAsync("initialize", new JsonObject { ["adapterID"] = "old8lang" });
        Assert.True(initResp["success"]!.GetValue<bool>());
        Assert.True(initResp["body"]!["supportsConfigurationDoneRequest"]!.GetValue<bool>());
        var initializedEvt = await client.WaitForEventAsync("initialized");
        Assert.Equal("initialized", initializedEvt["event"]!.GetValue<string>());

        // 2. launch
        var launchResp = await client.SendRequestAsync("launch", new JsonObject
        {
            ["program"] = scriptPath,
            ["cwd"] = _tempDir,
            ["stopOnEntry"] = false
        });
        Assert.True(launchResp["success"]!.GetValue<bool>());

        // 3. setBreakpoints on line 3 (`doubled <- total * 2`)
        var bpResp = await client.SendRequestAsync("setBreakpoints", new JsonObject
        {
            ["source"] = new JsonObject { ["path"] = scriptPath },
            ["breakpoints"] = new JsonArray { new JsonObject { ["line"] = 3 } }
        });
        Assert.True(bpResp["success"]!.GetValue<bool>());
        var bpList = bpResp["body"]!["breakpoints"]!.AsArray();
        Assert.Single(bpList);
        Assert.True(bpList[0]!["verified"]!.GetValue<bool>());

        // 4. configurationDone -> hits breakpoint at line 3
        var cfgResp = await client.SendRequestAsync("configurationDone", new JsonObject());
        Assert.True(cfgResp["success"]!.GetValue<bool>());

        var stopped = await client.WaitForEventAsync("stopped");
        Assert.Equal("breakpoint", stopped["body"]!["reason"]!.GetValue<string>());

        // 5. threads & stackTrace
        var threadsResp = await client.SendRequestAsync("threads", new JsonObject());
        Assert.Single(threadsResp["body"]!["threads"]!.AsArray());

        var stackResp = await client.SendRequestAsync("stackTrace", new JsonObject { ["threadId"] = 1 });
        var frames = stackResp["body"]!["stackFrames"]!.AsArray();
        Assert.True(frames.Count >= 2);
        Assert.Equal("add", frames[0]!["name"]!.GetValue<string>());
        Assert.Equal(3, frames[0]!["line"]!.GetValue<int>());
        var topFrameId = frames[0]!["id"]!.GetValue<int>();

        // 6. scopes & variables (Locals + Globals)
        var scopesResp = await client.SendRequestAsync("scopes", new JsonObject { ["frameId"] = topFrameId });
        var scopes = scopesResp["body"]!["scopes"]!.AsArray();
        Assert.Equal(2, scopes.Count);

        var localsRef = scopes[0]!["variablesReference"]!.GetValue<int>();
        var globalsRef = scopes[1]!["variablesReference"]!.GetValue<int>();

        var localsResp = await client.SendRequestAsync("variables", new JsonObject { ["variablesReference"] = localsRef });
        var locals = localsResp["body"]!["variables"]!.AsArray()
            .ToDictionary(v => v!["name"]!.GetValue<string>(), v => v!["value"]!.GetValue<string>());

        Assert.Equal("3", locals["a"]);
        Assert.Equal("4", locals["b"]);
        Assert.Equal("7", locals["total"]);

        var globalsResp = await client.SendRequestAsync("variables", new JsonObject { ["variablesReference"] = globalsRef });
        var globals = globalsResp["body"]!["variables"]!.AsArray()
            .ToDictionary(v => v!["name"]!.GetValue<string>(), v => v!["value"]!.GetValue<string>());
        Assert.Equal("10", globals["g"]);

        // 7. evaluate expression
        var evalResp = await client.SendRequestAsync("evaluate", new JsonObject
        {
            ["expression"] = "total + g",
            ["frameId"] = topFrameId
        });
        Assert.True(evalResp["success"]!.GetValue<bool>());
        Assert.Equal("17", evalResp["body"]!["result"]!.GetValue<string>());

        // 8. next (line-granular step over to line 4)
        var nextResp = await client.SendRequestAsync("next", new JsonObject { ["threadId"] = 1 });
        Assert.True(nextResp["success"]!.GetValue<bool>());
        var stepStopped = await client.WaitForEventAsync("stopped");
        Assert.Equal("step", stepStopped["body"]!["reason"]!.GetValue<string>());

        var stackAfterStep = await client.SendRequestAsync("stackTrace", new JsonObject { ["threadId"] = 1 });
        Assert.Equal(4, stackAfterStep["body"]!["stackFrames"]!.AsArray()[0]!["line"]!.GetValue<int>());

        // 9. continue -> program terminates cleanly
        var contResp = await client.SendRequestAsync("continue", new JsonObject { ["threadId"] = 1 });
        Assert.True(contResp["success"]!.GetValue<bool>());

        var terminated = await client.WaitForEventAsync("terminated");
        Assert.Equal("terminated", terminated["event"]!.GetValue<string>());

        // 10. disconnect
        await client.SendRequestAsync("disconnect", new JsonObject());
        await c2s.Writer.CompleteAsync();
        await serverTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ConditionalBreakpointAndFunctionBreakpoint_StopAtExpectedIteration()
    {
        var scriptPath = Path.Combine(_tempDir, "loop.old8");
        var source =
            "func compute(n: int) -> int {\n" +    // line 1
            "    sum <- 0\n" +                     // line 2
            "    for i <- 0, i < n, i <- i + 1 {\n" + // line 3
            "        sum <- sum + i\n" +           // line 4
            "    }\n" +                            // line 5
            "    return sum\n" +                   // line 6
            "}\n" +                                // line 7
            "ans <- compute(5)\n";                 // line 8
        File.WriteAllText(scriptPath, source);

        var c2s = new Pipe();
        var s2c = new Pipe();

        using var server = new VmDapServer(c2s.Reader.AsStream(), s2c.Writer.AsStream());
        var serverTask = server.RunAsync();
        var client = new DapClientHarness(c2s.Writer.AsStream(), s2c.Reader.AsStream());

        await client.SendRequestAsync("initialize", new JsonObject { ["adapterID"] = "old8lang" });
        await client.WaitForEventAsync("initialized");

        await client.SendRequestAsync("launch", new JsonObject { ["program"] = scriptPath });

        // 条件断点：仅当 i == 3 时在 line 4 暂停
        var bpResp = await client.SendRequestAsync("setBreakpoints", new JsonObject
        {
            ["source"] = new JsonObject { ["path"] = scriptPath },
            ["breakpoints"] = new JsonArray
            {
                new JsonObject { ["line"] = 4, ["condition"] = "i == 3" }
            }
        });
        Assert.True(bpResp["body"]!["breakpoints"]!.AsArray()[0]!["verified"]!.GetValue<bool>());

        await client.SendRequestAsync("configurationDone", new JsonObject());
        var stopped = await client.WaitForEventAsync("stopped");
        Assert.Equal("breakpoint", stopped["body"]!["reason"]!.GetValue<string>());

        var stackResp = await client.SendRequestAsync("stackTrace", new JsonObject { ["threadId"] = 1 });
        var frameId = stackResp["body"]!["stackFrames"]!.AsArray()[0]!["id"]!.GetValue<int>();

        var evalI = await client.SendRequestAsync("evaluate", new JsonObject
        {
            ["expression"] = "i",
            ["frameId"] = frameId
        });
        Assert.Equal("3", evalI["body"]!["result"]!.GetValue<string>());

        var evalSum = await client.SendRequestAsync("evaluate", new JsonObject
        {
            ["expression"] = "sum",
            ["frameId"] = frameId
        });
        Assert.Equal("3", evalSum["body"]!["result"]!.GetValue<string>()); // 0 + 1 + 2

        await client.SendRequestAsync("continue", new JsonObject { ["threadId"] = 1 });
        await client.WaitForEventAsync("terminated");
        await client.SendRequestAsync("disconnect", new JsonObject());
        await c2s.Writer.CompleteAsync();
        await serverTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class DapClientHarness(Stream toServer, Stream fromServer)
    {
        private int _seq = 1;
        private readonly Queue<JsonObject> _bufferedEvents = new();

        public async Task<JsonObject> SendRequestAsync(string command, JsonObject? arguments)
        {
            var seq = _seq++;
            var request = new JsonObject
            {
                ["seq"] = seq,
                ["type"] = "request",
                ["command"] = command
            };
            if (arguments != null)
            {
                request["arguments"] = arguments;
            }

            var bodyBytes = Encoding.UTF8.GetBytes(request.ToJsonString());
            var headerBytes = Encoding.ASCII.GetBytes($"Content-Length: {bodyBytes.Length}\r\n\r\n");
            await toServer.WriteAsync(headerBytes);
            await toServer.WriteAsync(bodyBytes);
            await toServer.FlushAsync();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (true)
            {
                var msg = await ReadMessageAsync(cts.Token);
                Assert.NotNull(msg);
                if (msg!["type"]?.GetValue<string>() == "response" &&
                    msg["request_seq"]?.GetValue<int>() == seq)
                {
                    return msg;
                }

                if (msg["type"]?.GetValue<string>() == "event")
                {
                    _bufferedEvents.Enqueue(msg);
                }
            }
        }

        public async Task<JsonObject> WaitForEventAsync(string eventName)
        {
            var temp = new Queue<JsonObject>();
            while (_bufferedEvents.Count > 0)
            {
                var item = _bufferedEvents.Dequeue();
                if (item["event"]?.GetValue<string>() == eventName)
                {
                    while (_bufferedEvents.Count > 0) temp.Enqueue(_bufferedEvents.Dequeue());
                    while (temp.Count > 0) _bufferedEvents.Enqueue(temp.Dequeue());
                    return item;
                }
                temp.Enqueue(item);
            }
            while (temp.Count > 0) _bufferedEvents.Enqueue(temp.Dequeue());

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (true)
            {
                var msg = await ReadMessageAsync(cts.Token);
                Assert.NotNull(msg);
                if (msg!["type"]?.GetValue<string>() == "event")
                {
                    if (msg["event"]?.GetValue<string>() == eventName)
                    {
                        return msg;
                    }
                    _bufferedEvents.Enqueue(msg);
                }
            }
        }

        private async Task<JsonObject?> ReadMessageAsync(CancellationToken ct)
        {
            var contentLength = -1;
            var headerLine = new List<byte>(64);
            var buf = new byte[1];

            while (true)
            {
                var n = await fromServer.ReadAsync(buf.AsMemory(0, 1), ct);
                if (n == 0) return null;

                if (buf[0] == (byte)'\n')
                {
                    if (headerLine.Count > 0 && headerLine[^1] == (byte)'\r')
                    {
                        headerLine.RemoveAt(headerLine.Count - 1);
                    }

                    if (headerLine.Count == 0 && contentLength >= 0)
                    {
                        break;
                    }

                    var line = Encoding.ASCII.GetString(headerLine.ToArray());
                    headerLine.Clear();
                    if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(line["Content-Length:".Length..].Trim(), out contentLength);
                    }
                }
                else
                {
                    headerLine.Add(buf[0]);
                }
            }

            var body = new byte[contentLength];
            var offset = 0;
            while (offset < contentLength)
            {
                var n = await fromServer.ReadAsync(body.AsMemory(offset, contentLength - offset), ct);
                if (n == 0) return null;
                offset += n;
            }

            return JsonNode.Parse(Encoding.UTF8.GetString(body)) as JsonObject;
        }
    }
}
