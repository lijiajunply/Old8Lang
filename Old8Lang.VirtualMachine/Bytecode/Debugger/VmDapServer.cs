using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Bytecode.VM;
using Old8Lang.LangParser;
using Old8Lang.ModuleSystem.StandardLibrary;

namespace Old8Lang.Bytecode.Debugger;

/// <summary>
/// 基于 <see cref="VirtualMachine"/> 与 <see cref="VMDebugger"/> 的 Debug Adapter Protocol (DAP) 服务端实现。
/// 采用标准 <c>Content-Length</c> 帧协议，支持在 CLI（<c>Old8Lang.App --dap</c>）与进程内流测试中复用。
/// </summary>
public sealed class VmDapServer : IDisposable
{
    private const int MainThreadId = 1;

    private sealed class DapAbortException : Exception;

    private sealed record PendingLineBreakpoint(int Line, string? Condition);
    private sealed record PendingFunctionBreakpoint(string Name, string? Condition);

    private sealed class PausedFrameSnapshot
    {
        public required int FrameId { get; init; }
        public required string FunctionName { get; init; }
        public required string SourcePath { get; init; }
        public required int Line { get; init; }
        public required int Column { get; init; }
        public required Dictionary<string, object?> Locals { get; init; }
    }

    private sealed class PausedSnapshot
    {
        public required List<PausedFrameSnapshot> Frames { get; init; }
        public required Dictionary<string, object?> Globals { get; init; }
        public Dictionary<int, object?> ReferenceValues { get; } = new();
        public int NextReferenceId = 1000;
    }

    private readonly Stream _input;
    private readonly Stream _output;
    private readonly StandardLibraryCatalog _standardLibraries;
    private readonly Lock _writeLock = new();
    private readonly Lock _stateLock = new();
    private readonly ManualResetEventSlim _resumeGate = new(initialState: false);

    private int _nextSeq = 1;
    private bool _disposed;
    private volatile bool _abortRequested;
    private volatile bool _sessionEnded;

    private string? _programPath;
    private string? _workingDirectory;
    private bool _stopOnEntry;
    private bool _noDebug;

    private readonly Dictionary<string, List<PendingLineBreakpoint>> _lineBreakpoints = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PendingFunctionBreakpoint> _functionBreakpoints = [];

    private VirtualMachine? _vm;
    private VMDebugger? _debugger;
    private Thread? _vmThread;
    private PausedSnapshot? _pausedSnapshot;
    private int _pausedTopLine;

    public VmDapServer(
        Stream input,
        Stream output,
        StandardLibraryCatalog? standardLibraries = null)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _standardLibraries = standardLibraries ?? StandardLibraryRegistry.DefaultCatalog;
    }

    /// <summary>主消息循环：读取 DAP 请求并派发处理，直至收到 disconnect 或输入流关闭。</summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested && !_sessionEnded)
        {
            var rawJson = await ReadMessageAsync(cancellationToken).ConfigureAwait(false);
            if (rawJson == null)
            {
                break;
            }

            JsonNode? node;
            try
            {
                node = JsonNode.Parse(rawJson);
            }
            catch
            {
                continue;
            }

            if (node is not JsonObject message)
            {
                continue;
            }

            var type = message["type"]?.GetValue<string>();
            if (type != "request")
            {
                continue;
            }

            var seq = message["seq"]?.GetValue<int>() ?? 0;
            var command = message["command"]?.GetValue<string>() ?? string.Empty;
            var arguments = message["arguments"] as JsonObject;

            HandleRequest(seq, command, arguments);
        }

        StopVmIfRunning();
    }

    /// <summary>向客户端发送 DAP <c>output</c> 事件（线程安全）。</summary>
    public void SendOutputEvent(string category, string text)
    {
        if (string.IsNullOrEmpty(text) || _disposed)
        {
            return;
        }

        SendEvent("output", new JsonObject
        {
            ["category"] = category,
            ["output"] = text
        });
    }

    private void HandleRequest(int requestSeq, string command, JsonObject? args)
    {
        try
        {
            switch (command)
            {
                case "initialize":
                    HandleInitialize(requestSeq);
                    break;
                case "launch":
                    HandleLaunch(requestSeq, args);
                    break;
                case "setBreakpoints":
                    HandleSetBreakpoints(requestSeq, args);
                    break;
                case "setFunctionBreakpoints":
                    HandleSetFunctionBreakpoints(requestSeq, args);
                    break;
                case "setExceptionBreakpoints":
                    SendSuccessResponse(requestSeq, command, new JsonObject());
                    break;
                case "configurationDone":
                    HandleConfigurationDone(requestSeq);
                    break;
                case "threads":
                    HandleThreads(requestSeq);
                    break;
                case "stackTrace":
                    HandleStackTrace(requestSeq, args);
                    break;
                case "scopes":
                    HandleScopes(requestSeq, args);
                    break;
                case "variables":
                    HandleVariables(requestSeq, args);
                    break;
                case "evaluate":
                    HandleEvaluate(requestSeq, args);
                    break;
                case "continue":
                    HandleContinue(requestSeq);
                    break;
                case "next":
                    HandleNext(requestSeq);
                    break;
                case "stepIn":
                    HandleStepIn(requestSeq);
                    break;
                case "stepOut":
                    HandleStepOut(requestSeq);
                    break;
                case "pause":
                    HandlePause(requestSeq);
                    break;
                case "terminate":
                    HandleTerminate(requestSeq);
                    break;
                case "disconnect":
                    HandleDisconnect(requestSeq);
                    break;
                default:
                    SendSuccessResponse(requestSeq, command, new JsonObject());
                    break;
            }
        }
        catch (Exception ex)
        {
            SendErrorResponse(requestSeq, command, ex.Message);
        }
    }

    private void HandleInitialize(int requestSeq)
    {
        var capabilities = new JsonObject
        {
            ["supportsConfigurationDoneRequest"] = true,
            ["supportsFunctionBreakpoints"] = true,
            ["supportsConditionalBreakpoints"] = true,
            ["supportsEvaluateForHovers"] = true,
            ["supportsTerminateRequest"] = true,
            ["supportsStepBack"] = false,
            ["supportsRestartRequest"] = false,
            ["supportsSingleThreadExecutionRequests"] = false
        };

        SendSuccessResponse(requestSeq, "initialize", capabilities);
        SendEvent("initialized", new JsonObject());
    }

    private void HandleLaunch(int requestSeq, JsonObject? args)
    {
        var program = args?["program"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(program))
        {
            SendErrorResponse(requestSeq, "launch", "launch 请求缺少必需的 program 字段。");
            return;
        }

        var cwd = args?["cwd"]?.GetValue<string>();
        var fullProgramPath = Path.IsPathRooted(program)
            ? Path.GetFullPath(program)
            : Path.GetFullPath(Path.Combine(string.IsNullOrWhiteSpace(cwd) ? Directory.GetCurrentDirectory() : cwd, program));

        if (!File.Exists(fullProgramPath))
        {
            SendErrorResponse(requestSeq, "launch", $"找不到要调试的 Old8Lang 源文件: {fullProgramPath}");
            return;
        }

        _programPath = fullProgramPath;
        _workingDirectory = !string.IsNullOrWhiteSpace(cwd) && Directory.Exists(cwd)
            ? Path.GetFullPath(cwd)
            : Path.GetDirectoryName(fullProgramPath) ?? Directory.GetCurrentDirectory();
        _stopOnEntry = args?["stopOnEntry"]?.GetValue<bool>() ?? false;
        _noDebug = args?["noDebug"]?.GetValue<bool>() ?? false;

        // 解析并编译为字节码
        var sourceText = File.ReadAllText(fullProgramPath);
        var tokens = LangTokenizer.Tokenize(sourceText);
        var parser = new LangParser.LangParser(tokens, sourceText, fullProgramPath);
        var ast = parser.ParseProgram();

        var compiler = new BytecodeCompiler(_standardLibraries)
        {
            EnableTypeChecking = false,
            EnableTypeInference = true
        };
        var bytecodeFile = compiler.Compile(ast);

        var vm = new VirtualMachine(bytecodeFile, _workingDirectory, _standardLibraries);
        var debugger = new VMDebugger(bytecodeFile.DebugInfo);
        debugger.Start();

        if (!_noDebug)
        {
            vm.Debugger = debugger;
            vm.DebugPaused = OnVmPaused;
            debugger.ConditionEvaluator = EvaluateConditionOnCurrentFrame;
        }

        lock (_stateLock)
        {
            _vm = vm;
            _debugger = debugger;
        }

        // 将 launch 之前已经下发的断点安装到 VM 中
        ReapplyAllBreakpoints();

        SendSuccessResponse(requestSeq, "launch", new JsonObject());
    }

    private void HandleSetBreakpoints(int requestSeq, JsonObject? args)
    {
        var sourcePath = args?["source"]?["path"]?.GetValue<string>() ?? _programPath ?? string.Empty;
        var normalizedPath = NormalizePath(sourcePath);
        var requested = new List<PendingLineBreakpoint>();

        if (args?["breakpoints"] is JsonArray bpArray)
        {
            foreach (var item in bpArray)
            {
                if (item is JsonObject obj && obj["line"] != null)
                {
                    var line = obj["line"]!.GetValue<int>();
                    var condition = obj["condition"]?.GetValue<string>();
                    requested.Add(new PendingLineBreakpoint(line, condition));
                }
            }
        }
        else if (args?["lines"] is JsonArray linesArray)
        {
            foreach (var item in linesArray)
            {
                if (item != null)
                {
                    requested.Add(new PendingLineBreakpoint(item.GetValue<int>(), null));
                }
            }
        }

        lock (_stateLock)
        {
            _lineBreakpoints[normalizedPath] = requested;
        }

        var verifiedResults = ApplyLineBreakpointsForResponse(normalizedPath, requested);
        SendSuccessResponse(requestSeq, "setBreakpoints", new JsonObject
        {
            ["breakpoints"] = verifiedResults
        });
    }

    private void HandleSetFunctionBreakpoints(int requestSeq, JsonObject? args)
    {
        var requested = new List<PendingFunctionBreakpoint>();
        if (args?["breakpoints"] is JsonArray bpArray)
        {
            foreach (var item in bpArray)
            {
                if (item is JsonObject obj)
                {
                    var name = obj["name"]?.GetValue<string>();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        var condition = obj["condition"]?.GetValue<string>();
                        requested.Add(new PendingFunctionBreakpoint(name.Trim(), condition));
                    }
                }
            }
        }

        lock (_stateLock)
        {
            _functionBreakpoints.Clear();
            _functionBreakpoints.AddRange(requested);
        }

        var responseBps = new JsonArray();
        VirtualMachine? vm;
        VMDebugger? debugger;
        lock (_stateLock)
        {
            vm = _vm;
            debugger = _debugger;
        }

        if (vm != null && debugger != null && !_noDebug)
        {
            debugger.ClearFunctionBreakpoints();
            var id = 1;
            foreach (var bp in requested)
            {
                var verified = vm.AddFunctionEntryBreakpoint(bp.Name, bp.Condition);
                responseBps.Add(new JsonObject
                {
                    ["id"] = id++,
                    ["verified"] = verified
                });
            }
        }
        else
        {
            var id = 1;
            foreach (var _ in requested)
            {
                responseBps.Add(new JsonObject
                {
                    ["id"] = id++,
                    ["verified"] = true
                });
            }
        }

        SendSuccessResponse(requestSeq, "setFunctionBreakpoints", new JsonObject
        {
            ["breakpoints"] = responseBps
        });
    }

    private void HandleConfigurationDone(int requestSeq)
    {
        SendSuccessResponse(requestSeq, "configurationDone", new JsonObject());

        VirtualMachine? vm;
        VMDebugger? debugger;
        lock (_stateLock)
        {
            vm = _vm;
            debugger = _debugger;
        }

        if (vm == null)
        {
            return;
        }

        if (_stopOnEntry && !_noDebug && debugger != null)
        {
            debugger.PauseOnEntry();
        }

        _vmThread = new Thread(() => RunVmLoop(vm))
        {
            IsBackground = true,
            Name = "Old8Lang-DAP-VM"
        };
        _vmThread.Start();
    }

    private void RunVmLoop(VirtualMachine vm)
    {
        try
        {
            vm.Execute();
            SendEvent("exited", new JsonObject { ["exitCode"] = 0 });
            SendEvent("terminated", new JsonObject());
        }
        catch (DapAbortException)
        {
            SendEvent("exited", new JsonObject { ["exitCode"] = 0 });
            SendEvent("terminated", new JsonObject());
        }
        catch (Exception ex)
        {
            SendOutputEvent("stderr", $"运行时错误: {ex.Message}\n");
            SendEvent("exited", new JsonObject { ["exitCode"] = 1 });
            SendEvent("terminated", new JsonObject());
        }
        finally
        {
            lock (_stateLock)
            {
                _pausedSnapshot = null;
            }
        }
    }

    private void OnVmPaused(VirtualMachine vm, CallFrame currentFrame, Instruction instruction)
    {
        if (_abortRequested)
        {
            throw new DapAbortException();
        }

        var snapshot = CaptureSnapshot(vm, currentFrame, instruction);
        var reason = _debugger?.LastStopReason ?? "step";

        lock (_stateLock)
        {
            _pausedSnapshot = snapshot;
            _pausedTopLine = instruction.LineNumber ?? 0;
            _resumeGate.Reset();
        }

        SendEvent("stopped", new JsonObject
        {
            ["reason"] = reason,
            ["threadId"] = MainThreadId,
            ["allThreadsStopped"] = true
        });

        // 阻塞 VM 线程，等待客户端下发 continue / next / stepIn / stepOut / disconnect
        _resumeGate.Wait();

        if (_abortRequested)
        {
            throw new DapAbortException();
        }
    }

    private PausedSnapshot CaptureSnapshot(VirtualMachine vm, CallFrame topFrame, Instruction topInstruction)
    {
        var liveFrames = vm.GetLiveCallStackFrames();
        var frames = new List<PausedFrameSnapshot>();

        if (liveFrames.Count == 0)
        {
            frames.Add(CreateFrameSnapshot(0, topFrame, topInstruction.LineNumber, topInstruction.ColumnNumber, topInstruction.SourceFile));
        }
        else
        {
            for (var i = 0; i < liveFrames.Count; i++)
            {
                var frame = liveFrames[i];
                int? line;
                int? col;
                string? file;

                if (i == 0)
                {
                    line = topInstruction.LineNumber;
                    col = topInstruction.ColumnNumber;
                    file = topInstruction.SourceFile;
                }
                else
                {
                    var instIndex = Math.Clamp(frame.IP - 1, 0, Math.Max(0, frame.Function.Instructions.Count - 1));
                    var inst = frame.Function.Instructions.Count > 0 ? frame.Function.Instructions[instIndex] : null;
                    line = inst?.LineNumber;
                    col = inst?.ColumnNumber;
                    file = inst?.SourceFile;
                }

                frames.Add(CreateFrameSnapshot(i + 1, frame, line, col, file));
            }
        }

        var globals = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (k, v) in vm.GetGlobalVariables())
        {
            globals[k] = v;
        }

        return new PausedSnapshot
        {
            Frames = frames,
            Globals = globals
        };
    }

    private PausedFrameSnapshot CreateFrameSnapshot(
        int frameId,
        CallFrame frame,
        int? line,
        int? column,
        string? sourceFile)
    {
        var locals = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (k, v) in VirtualMachine.GetFrameVariables(frame))
        {
            locals[k] = v;
        }

        var resolvedPath = !string.IsNullOrWhiteSpace(sourceFile) && File.Exists(sourceFile)
            ? Path.GetFullPath(sourceFile)
            : _programPath ?? sourceFile ?? string.Empty;

        return new PausedFrameSnapshot
        {
            FrameId = frameId,
            FunctionName = frame.Function.Name,
            SourcePath = resolvedPath,
            Line = Math.Max(1, line ?? 1),
            Column = Math.Max(1, (column ?? 0) + 1),
            Locals = locals
        };
    }

    private void HandleThreads(int requestSeq)
    {
        SendSuccessResponse(requestSeq, "threads", new JsonObject
        {
            ["threads"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = MainThreadId,
                    ["name"] = "Main Thread"
                }
            }
        });
    }

    private void HandleStackTrace(int requestSeq, JsonObject? args)
    {
        PausedSnapshot? snapshot;
        lock (_stateLock)
        {
            snapshot = _pausedSnapshot;
        }

        var startFrame = args?["startFrame"]?.GetValue<int>() ?? 0;
        var levels = args?["levels"]?.GetValue<int>() ?? 0;

        var framesArray = new JsonArray();
        var totalFrames = snapshot?.Frames.Count ?? 0;

        if (snapshot != null)
        {
            var slice = snapshot.Frames.Skip(Math.Max(0, startFrame));
            if (levels > 0)
            {
                slice = slice.Take(levels);
            }

            foreach (var frame in slice)
            {
                framesArray.Add(new JsonObject
                {
                    ["id"] = frame.FrameId,
                    ["name"] = frame.FunctionName,
                    ["line"] = frame.Line,
                    ["column"] = frame.Column,
                    ["source"] = new JsonObject
                    {
                        ["name"] = Path.GetFileName(frame.SourcePath),
                        ["path"] = frame.SourcePath
                    }
                });
            }
        }

        SendSuccessResponse(requestSeq, "stackTrace", new JsonObject
        {
            ["stackFrames"] = framesArray,
            ["totalFrames"] = totalFrames
        });
    }

    private void HandleScopes(int requestSeq, JsonObject? args)
    {
        var frameId = args?["frameId"]?.GetValue<int>() ?? 1;
        var localsRef = frameId * 2 + 1;
        var globalsRef = frameId * 2 + 2;

        SendSuccessResponse(requestSeq, "scopes", new JsonObject
        {
            ["scopes"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "Locals",
                    ["presentationHint"] = "locals",
                    ["variablesReference"] = localsRef,
                    ["expensive"] = false
                },
                new JsonObject
                {
                    ["name"] = "Globals",
                    ["variablesReference"] = globalsRef,
                    ["expensive"] = false
                }
            }
        });
    }

    private void HandleVariables(int requestSeq, JsonObject? args)
    {
        var variablesReference = args?["variablesReference"]?.GetValue<int>() ?? 0;
        var variablesArray = new JsonArray();

        PausedSnapshot? snapshot;
        lock (_stateLock)
        {
            snapshot = _pausedSnapshot;
        }

        if (snapshot != null && variablesReference > 0)
        {
            if (variablesReference >= 1000 && snapshot.ReferenceValues.TryGetValue(variablesReference, out var composite))
            {
                PopulateCompositeVariables(composite, snapshot, variablesArray);
            }
            else
            {
                var isGlobals = (variablesReference % 2) == 0;
                var frameId = (variablesReference - (isGlobals ? 2 : 1)) / 2;

                if (isGlobals)
                {
                    foreach (var (name, value) in snapshot.Globals.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                    {
                        variablesArray.Add(CreateVariableJson(name, value, snapshot));
                    }
                }
                else
                {
                    var frame = snapshot.Frames.FirstOrDefault(f => f.FrameId == frameId)
                        ?? snapshot.Frames.FirstOrDefault();
                    if (frame != null)
                    {
                        foreach (var (name, value) in frame.Locals.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                        {
                            variablesArray.Add(CreateVariableJson(name, value, snapshot));
                        }
                    }
                }
            }
        }

        SendSuccessResponse(requestSeq, "variables", new JsonObject
        {
            ["variables"] = variablesArray
        });
    }

    private void HandleEvaluate(int requestSeq, JsonObject? args)
    {
        var expression = (args?["expression"]?.GetValue<string>() ?? string.Empty).Trim();
        var frameId = args?["frameId"]?.GetValue<int>();

        PausedSnapshot? snapshot;
        lock (_stateLock)
        {
            snapshot = _pausedSnapshot;
        }

        if (snapshot == null)
        {
            SendErrorResponse(requestSeq, "evaluate", "调试器当前未处于暂停状态。");
            return;
        }

        var frame = frameId.HasValue
            ? snapshot.Frames.FirstOrDefault(f => f.FrameId == frameId.Value) ?? snapshot.Frames.FirstOrDefault()
            : snapshot.Frames.FirstOrDefault();

        if (!TryEvaluateExpression(expression, frame?.Locals, snapshot.Globals, out var value, out var error))
        {
            SendErrorResponse(requestSeq, "evaluate", error ?? $"无法求值表达式: {expression}");
            return;
        }

        var childRef = AllocateReferenceIfComposite(value, snapshot);
        SendSuccessResponse(requestSeq, "evaluate", new JsonObject
        {
            ["result"] = FormatValue(value),
            ["type"] = GetValueTypeName(value),
            ["variablesReference"] = childRef
        });
    }

    private void HandleContinue(int requestSeq)
    {
        SendSuccessResponse(requestSeq, "continue", new JsonObject
        {
            ["allThreadsContinued"] = true
        });

        lock (_stateLock)
        {
            _pausedSnapshot = null;
            _debugger?.Continue();
            _resumeGate.Set();
        }
    }

    private void HandleNext(int requestSeq)
    {
        SendSuccessResponse(requestSeq, "next", new JsonObject());

        lock (_stateLock)
        {
            var originLine = _pausedTopLine > 0 ? _pausedTopLine : (int?)null;
            _pausedSnapshot = null;
            _debugger?.StepOver(originLine);
            _resumeGate.Set();
        }
    }

    private void HandleStepIn(int requestSeq)
    {
        SendSuccessResponse(requestSeq, "stepIn", new JsonObject());

        lock (_stateLock)
        {
            var originLine = _pausedTopLine > 0 ? _pausedTopLine : (int?)null;
            _pausedSnapshot = null;
            _debugger?.StepInto(originLine);
            _resumeGate.Set();
        }
    }

    private void HandleStepOut(int requestSeq)
    {
        SendSuccessResponse(requestSeq, "stepOut", new JsonObject());

        lock (_stateLock)
        {
            _pausedSnapshot = null;
            _debugger?.StepOut();
            _resumeGate.Set();
        }
    }

    private void HandlePause(int requestSeq)
    {
        SendSuccessResponse(requestSeq, "pause", new JsonObject());

        lock (_stateLock)
        {
            _debugger?.Pause();
        }
    }

    private void HandleTerminate(int requestSeq)
    {
        StopVmIfRunning();
        SendSuccessResponse(requestSeq, "terminate", new JsonObject());
    }

    private void HandleDisconnect(int requestSeq)
    {
        _sessionEnded = true;
        StopVmIfRunning();
        SendSuccessResponse(requestSeq, "disconnect", new JsonObject());
    }

    private void StopVmIfRunning()
    {
        _abortRequested = true;
        _resumeGate.Set();
        if (_vmThread is { IsAlive: true })
        {
            _vmThread.Join(TimeSpan.FromSeconds(2));
        }
    }

    private bool EvaluateConditionOnCurrentFrame(string condition)
    {
        VirtualMachine? vm;
        lock (_stateLock)
        {
            vm = _vm;
        }

        if (vm == null)
        {
            return true;
        }

        var frames = vm.GetLiveCallStackFrames();
        var locals = frames.Count > 0
            ? VirtualMachine.GetFrameVariables(frames[0])
            : new Dictionary<string, object?>();
        var globals = vm.GetGlobalVariables();

        if (!TryEvaluateExpression(condition, locals, globals, out var result, out _))
        {
            return false;
        }

        return IsTruthy(result);
    }

    private JsonArray ApplyLineBreakpointsForResponse(string normalizedPath, List<PendingLineBreakpoint> requested)
    {
        var result = new JsonArray();
        VirtualMachine? vm;
        VMDebugger? debugger;
        string? programPath;
        lock (_stateLock)
        {
            vm = _vm;
            debugger = _debugger;
            programPath = _programPath;
        }

        if (vm == null || debugger == null || _noDebug)
        {
            var id = 1;
            foreach (var bp in requested)
            {
                result.Add(new JsonObject
                {
                    ["id"] = id++,
                    ["verified"] = true,
                    ["line"] = bp.Line
                });
            }
            return result;
        }

        var matchesProgram = string.IsNullOrEmpty(programPath) ||
                             string.Equals(NormalizePath(programPath), normalizedPath, StringComparison.OrdinalIgnoreCase);

        if (matchesProgram)
        {
            debugger.ClearLineBreakpoints();
        }

        var bpId = 1;
        foreach (var bp in requested)
        {
            var verified = matchesProgram && vm.AddBreakpointsAtLine(bp.Line, bp.Condition).Count > 0;
            result.Add(new JsonObject
            {
                ["id"] = bpId++,
                ["verified"] = verified,
                ["line"] = bp.Line
            });
        }

        return result;
    }

    private void ReapplyAllBreakpoints()
    {
        VirtualMachine? vm;
        VMDebugger? debugger;
        string? programPath;
        List<PendingLineBreakpoint>? programLineBps = null;
        List<PendingFunctionBreakpoint> funcBps;

        lock (_stateLock)
        {
            vm = _vm;
            debugger = _debugger;
            programPath = _programPath;
            if (!string.IsNullOrEmpty(programPath))
            {
                _lineBreakpoints.TryGetValue(NormalizePath(programPath), out programLineBps);
            }
            funcBps = [.. _functionBreakpoints];
        }

        if (vm == null || debugger == null || _noDebug)
        {
            return;
        }

        debugger.ClearBreakpoints();

        if (programLineBps != null)
        {
            foreach (var bp in programLineBps)
            {
                vm.AddBreakpointsAtLine(bp.Line, bp.Condition);
            }
        }

        foreach (var fbp in funcBps)
        {
            vm.AddFunctionEntryBreakpoint(fbp.Name, fbp.Condition);
        }
    }

    private static JsonObject CreateVariableJson(string name, object? value, PausedSnapshot snapshot)
    {
        var childRef = AllocateReferenceIfComposite(value, snapshot);
        return new JsonObject
        {
            ["name"] = name,
            ["value"] = FormatValue(value),
            ["type"] = GetValueTypeName(value),
            ["variablesReference"] = childRef
        };
    }

    private static int AllocateReferenceIfComposite(object? value, PausedSnapshot snapshot)
    {
        if (value is BytecodeObjectInstance { Fields.Count: > 0 } or IDictionary { Count: > 0 } or IList { Count: > 0 })
        {
            var id = snapshot.NextReferenceId++;
            snapshot.ReferenceValues[id] = value;
            return id;
        }

        return 0;
    }

    private static void PopulateCompositeVariables(object? composite, PausedSnapshot snapshot, JsonArray target)
    {
        switch (composite)
        {
            case BytecodeObjectInstance obj:
                foreach (var (field, val) in obj.Fields.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                {
                    target.Add(CreateVariableJson(field, val, snapshot));
                }
                break;

            case IDictionary dict:
                foreach (DictionaryEntry entry in dict)
                {
                    var keyName = entry.Key?.ToString() ?? "null";
                    target.Add(CreateVariableJson($"[{keyName}]", entry.Value, snapshot));
                }
                break;

            case IList list:
                for (var i = 0; i < list.Count; i++)
                {
                    target.Add(CreateVariableJson($"[{i}]", list[i], snapshot));
                }
                break;
        }
    }

    internal static bool TryEvaluateExpression(
        string expression,
        IReadOnlyDictionary<string, object?>? locals,
        IReadOnlyDictionary<string, object?>? globals,
        out object? result,
        out string? error)
    {
        result = null;
        error = null;

        if (string.IsNullOrWhiteSpace(expression))
        {
            error = "空表达式";
            return false;
        }

        var trimmed = expression.Trim();

        // 二元比较与算术运算（按低优先级到高优先级拆分）
        string[] operators = ["==", "!=", "<=", ">=", "<", ">", "+", "-", "*", "/"];
        foreach (var op in operators)
        {
            var splitIdx = FindTopLevelOperator(trimmed, op);
            if (splitIdx > 0)
            {
                var leftExpr = trimmed[..splitIdx].Trim();
                var rightExpr = trimmed[(splitIdx + op.Length)..].Trim();
                if (TryEvaluateExpression(leftExpr, locals, globals, out var leftVal, out error) &&
                    TryEvaluateExpression(rightExpr, locals, globals, out var rightVal, out error))
                {
                    result = ApplyBinaryOperator(op, leftVal, rightVal);
                    return true;
                }
                return false;
            }
        }

        // 字面量
        if (trimmed == "null")
        {
            result = null;
            return true;
        }
        if (trimmed == "true")
        {
            result = true;
            return true;
        }
        if (trimmed == "false")
        {
            result = false;
            return true;
        }
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
        {
            result = trimmed[1..^1];
            return true;
        }
        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal))
        {
            result = intVal;
            return true;
        }
        if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var dblVal))
        {
            result = dblVal;
            return true;
        }

        // 成员链访问：a.b.c
        if (trimmed.Contains('.'))
        {
            var parts = trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2 && TryLookupVariable(parts[0], locals, globals, out var current))
            {
                for (var i = 1; i < parts.Length; i++)
                {
                    if (current is BytecodeObjectInstance obj && obj.Fields.TryGetValue(parts[i], out var nextVal))
                    {
                        current = nextVal;
                    }
                    else if (current is IDictionary dict && dict.Contains(parts[i]))
                    {
                        current = dict[parts[i]];
                    }
                    else if (parts[i] == "Length" && current is ICollection col)
                    {
                        current = col.Count;
                    }
                    else if (parts[i] == "Length" && current is string s)
                    {
                        current = s.Length;
                    }
                    else
                    {
                        error = $"找不到成员 '{parts[i]}'";
                        return false;
                    }
                }

                result = current;
                return true;
            }
        }

        // 简单变量查找
        if (TryLookupVariable(trimmed, locals, globals, out var found))
        {
            result = found;
            return true;
        }

        error = $"未找到变量 '{trimmed}'";
        return false;
    }

    private static bool TryLookupVariable(
        string name,
        IReadOnlyDictionary<string, object?>? locals,
        IReadOnlyDictionary<string, object?>? globals,
        out object? value)
    {
        if (locals != null && locals.TryGetValue(name, out value))
        {
            return true;
        }

        if (globals != null && globals.TryGetValue(name, out value))
        {
            return true;
        }

        value = null;
        return false;
    }

    private static int FindTopLevelOperator(string expr, string op)
    {
        var inString = false;
        var parenDepth = 0;

        for (var i = 0; i <= expr.Length - op.Length; i++)
        {
            var c = expr[i];
            if (c == '"' && (i == 0 || expr[i - 1] != '\\'))
            {
                inString = !inString;
                continue;
            }
            if (inString) continue;

            if (c == '(') parenDepth++;
            else if (c == ')') parenDepth--;
            if (parenDepth != 0) continue;

            if (expr.AsSpan(i, op.Length).SequenceEqual(op.AsSpan()))
            {
                // 避免把 <= / >= / == / != 的一部分当成 < / >
                if (op is "<" or ">" && i + 1 < expr.Length && expr[i + 1] == '=')
                {
                    continue;
                }
                if (op is "-" && i == 0)
                {
                    continue;
                }
                return i;
            }
        }

        return -1;
    }

    private static object? ApplyBinaryOperator(string op, object? left, object? right)
    {
        if (op == "==") return ValuesEqual(left, right);
        if (op == "!=") return !ValuesEqual(left, right);

        if (op == "+" && (left is string || right is string))
        {
            return $"{FormatRawString(left)}{FormatRawString(right)}";
        }

        if (TryToDouble(left, out var l) && TryToDouble(right, out var r))
        {
            return op switch
            {
                "<" => l < r,
                ">" => l > r,
                "<=" => l <= r,
                ">=" => l >= r,
                "+" => left is int li && right is int ri ? li + ri : l + r,
                "-" => left is int li2 && right is int ri2 ? li2 - ri2 : l - r,
                "*" => left is int li3 && right is int ri3 ? li3 * ri3 : l * r,
                "/" => r == 0 ? double.NaN : (left is int li4 && right is int ri4 && li4 % ri4 == 0 ? li4 / ri4 : l / r),
                _ => null
            };
        }

        return null;
    }

    private static bool ValuesEqual(object? left, object? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        if (TryToDouble(left, out var l) && TryToDouble(right, out var r))
        {
            return Math.Abs(l - r) < 1e-12;
        }
        return Equals(left, right);
    }

    private static bool TryToDouble(object? val, out double d)
    {
        switch (val)
        {
            case int i:
                d = i;
                return true;
            case long l:
                d = l;
                return true;
            case double dbl:
                d = dbl;
                return true;
            case float f:
                d = f;
                return true;
            default:
                d = 0;
                return false;
        }
    }

    private static bool IsTruthy(object? value) => value switch
    {
        null => false,
        bool b => b,
        int i => i != 0,
        double d => d != 0.0,
        string s => s.Length > 0,
        _ => true
    };

    private static string FormatRawString(object? value) => value switch
    {
        null => "null",
        bool b => b ? "true" : "false",
        _ => value.ToString() ?? string.Empty
    };

    internal static string FormatValue(object? value) => value switch
    {
        null => "null",
        bool b => b ? "true" : "false",
        string s => $"\"{s}\"",
        char c => $"'{c}'",
        BytecodeObjectInstance obj => $"{obj.ClassName} {{ {string.Join(", ", obj.Fields.Select(kv => $"{kv.Key}: {FormatValue(kv.Value)}"))} }}",
        IDictionary dict => $"{{ {string.Join(", ", dict.Keys.Cast<object?>().Select(k => $"{FormatValue(k)}: {FormatValue(dict[k!])}"))} }}",
        IList list => $"[{string.Join(", ", list.Cast<object?>().Select(FormatValue))}]",
        _ => value.ToString() ?? "null"
    };

    private static string GetValueTypeName(object? value) => value switch
    {
        null => "null",
        int => "int",
        double => "double",
        bool => "bool",
        string => "string",
        char => "char",
        BytecodeObjectInstance obj => obj.ClassName,
        IDictionary => "dict",
        Array => "array",
        IList => "list",
        _ => value.GetType().Name
    };

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return path;
        }
    }

    #region DAP Framing I/O

    private void SendSuccessResponse(int requestSeq, string command, JsonObject body)
    {
        var response = new JsonObject
        {
            ["seq"] = Interlocked.Increment(ref _nextSeq),
            ["type"] = "response",
            ["request_seq"] = requestSeq,
            ["success"] = true,
            ["command"] = command,
            ["body"] = body
        };
        WriteMessage(response);
    }

    private void SendErrorResponse(int requestSeq, string command, string message)
    {
        var response = new JsonObject
        {
            ["seq"] = Interlocked.Increment(ref _nextSeq),
            ["type"] = "response",
            ["request_seq"] = requestSeq,
            ["success"] = false,
            ["command"] = command,
            ["message"] = message
        };
        WriteMessage(response);
    }

    private void SendEvent(string eventName, JsonObject body)
    {
        var evt = new JsonObject
        {
            ["seq"] = Interlocked.Increment(ref _nextSeq),
            ["type"] = "event",
            ["event"] = eventName,
            ["body"] = body
        };
        WriteMessage(evt);
    }

    private void WriteMessage(JsonObject payload)
    {
        var jsonBytes = Encoding.UTF8.GetBytes(payload.ToJsonString());
        var headerBytes = Encoding.ASCII.GetBytes($"Content-Length: {jsonBytes.Length}\r\n\r\n");

        lock (_writeLock)
        {
            if (_disposed) return;
            _output.Write(headerBytes, 0, headerBytes.Length);
            _output.Write(jsonBytes, 0, jsonBytes.Length);
            _output.Flush();
        }
    }

    private async Task<string?> ReadMessageAsync(CancellationToken cancellationToken)
    {
        var contentLength = -1;
        var headerLine = new List<byte>(64);
        var singleByte = new byte[1];

        while (true)
        {
            var read = await _input.ReadAsync(singleByte.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return null;
            }

            var b = singleByte[0];
            if (b == (byte)'\n')
            {
                if (headerLine.Count > 0 && headerLine[^1] == (byte)'\r')
                {
                    headerLine.RemoveAt(headerLine.Count - 1);
                }

                if (headerLine.Count == 0)
                {
                    if (contentLength >= 0)
                    {
                        break;
                    }
                    continue;
                }

                var lineText = Encoding.ASCII.GetString(headerLine.ToArray());
                headerLine.Clear();

                if (lineText.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    var lenStr = lineText["Content-Length:".Length..].Trim();
                    int.TryParse(lenStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out contentLength);
                }
            }
            else
            {
                headerLine.Add(b);
            }
        }

        var bodyBytes = new byte[contentLength];
        var offset = 0;
        while (offset < contentLength)
        {
            var n = await _input.ReadAsync(bodyBytes.AsMemory(offset, contentLength - offset), cancellationToken)
                .ConfigureAwait(false);
            if (n == 0)
            {
                return null;
            }
            offset += n;
        }

        return Encoding.UTF8.GetString(bodyBytes);
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _sessionEnded = true;
        StopVmIfRunning();
        _resumeGate.Dispose();
    }
}
