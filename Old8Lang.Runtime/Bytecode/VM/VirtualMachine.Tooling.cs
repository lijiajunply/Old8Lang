using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Debugger;
using Old8Lang.Bytecode.Profiler;

namespace Old8Lang.Bytecode.VM;

/// <summary>
/// 虚拟机的调试 / 性能分析 / 反汇编接入点。
/// </summary>
/// <remarks>
/// <para><see cref="Profiler"/>、<see cref="Debugger"/> 默认都是 null：虚拟机正常执行时
/// 每帧只多一次 null 判断（见 <c>ExecuteFrameLoop</c> 的 <see cref="_toolingEnabled"/>），
/// 不改变热路径的指令分发。</para>
///
/// <para>调试暂停点通过 <see cref="DebugPaused"/> 事件交给宿主（CLI 的交互式调试器），
/// 虚拟机本身不读取控制台，便于测试时替换成一个自动继续的处理器。</para>
/// </remarks>
public partial class VirtualMachine
{
    private bool _toolingEnabled;
    private VMProfiler? _profiler;
    private VMDebugger? _debugger;

    /// <summary>性能分析器（null 表示不分析）</summary>
    public VMProfiler? Profiler
    {
        get => _profiler;
        set
        {
            _profiler = value;
            _toolingEnabled = value is not null || _debugger is not null;
        }
    }

    /// <summary>调试器（null 表示不调试）</summary>
    public VMDebugger? Debugger
    {
        get => _debugger;
        set
        {
            _debugger = value;
            _toolingEnabled = value is not null || _profiler is not null;
        }
    }

    /// <summary>
    /// 命中暂停点（断点或单步）时触发。参数依次为虚拟机、当前帧、即将执行的指令。
    /// </summary>
    /// <remarks>
    /// 处理器负责调用 <see cref="VMDebugger.Continue"/>/<see cref="VMDebugger.StepInto"/> 等方法
    /// 让执行继续；没有订阅者时虚拟机自动继续，避免死等。
    /// </remarks>
    public Action<VirtualMachine, CallFrame, Instruction>? DebugPaused { get; set; }

    /// <summary>进入新帧时记录（性能分析计时 + 调试器调用栈）</summary>
    private void OnFrameEnter(CallFrame frame)
    {
        if (!_toolingEnabled)
        {
            return;
        }

        frame.ProfileStopwatch = Profiler?.BeginFunction(frame.Function.Name);
        Debugger?.EnterFunction(frame.Function.Name, 0);
    }

    /// <summary>离开帧时记录</summary>
    private void OnFrameExit(CallFrame frame)
    {
        if (!_toolingEnabled)
        {
            return;
        }

        if (frame.ProfileStopwatch is not null)
        {
            Profiler?.EndFunction(frame.Function.Name, frame.ProfileStopwatch);
            // 帧是从池里复用的，必须清掉，避免下一次租到同一帧时串数据
            frame.ProfileStopwatch = null;
        }

        Debugger?.ExitFunction();
    }

    /// <summary>每条指令执行前的探针（仅工具开启时调用）</summary>
    private void ProbeInstruction(Instruction instruction, CallFrame frame)
    {
        Profiler?.RecordInstruction(instruction.OpCode);

        var debugger = Debugger;
        if (debugger is null || !debugger.Enabled)
        {
            return;
        }

        // frame.IP 在取指后已经自增，指令下标要减一
        int instructionOffset = frame.IP - 1;
        if (!debugger.ShouldPauseBeforeInstruction(frame.Function.Name, instructionOffset))
        {
            return;
        }

        var handler = DebugPaused;
        if (handler is null)
        {
            // 没有宿主接管时自动继续，否则会一直卡在同一个断点
            debugger.Continue();
            return;
        }

        handler(this, frame, instruction);
    }

    /// <summary>
    /// 在字节码里按源码行号设置断点，返回命中的指令下标（没有命中时为空）。
    /// </summary>
    /// <remarks>
    /// <see cref="VMDebugger.AddBreakpoint(string, int)"/> 依赖 <c>DebugInfo</c>，
    /// 而当前编译器只在部分路径填充它；直接按指令自带的 <see cref="Instruction.LineNumber"/>
    /// 建立断点更可靠。扫描所有函数，命中位置按函数名限定。
    /// </remarks>
    public IReadOnlyList<int> AddBreakpointsAtLine(int sourceLine)
    {
        var debugger = Debugger ?? throw new InvalidOperationException("调试器未启用");
        var offsets = new List<int>();

        // 每个函数单独扫描：同名行内可能有多个函数，断点按函数名限定，避免 offset 误命中。
        // 同一行通常对应多条指令，只在「刚进入该行」的那条指令上设断点，否则会连续暂停多次。
        foreach (var function in _bytecodeFile.Functions)
        {
            int? previousLine = null;
            for (int i = 0; i < function.Instructions.Count; i++)
            {
                var line = function.Instructions[i].LineNumber;
                if (line == sourceLine && previousLine != sourceLine)
                {
                    debugger.AddFunctionBreakpoint(function.Name, i);
                    offsets.Add(i);
                }

                previousLine = line;
            }
        }

        return offsets;
    }

    /// <summary>当前帧的局部变量与参数快照（供调试器 REPL 使用）</summary>
    public static IReadOnlyDictionary<string, object?> GetFrameVariables(CallFrame frame)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        var parameters = frame.Function.Parameters;
        for (int i = 0; i < parameters.Count && i < frame.Locals.Length; i++)
        {
            result[parameters[i]] = frame.Locals[i];
        }

        return result;
    }
}
