using Old8Lang.App.Services;
using Old8Lang.Debugger;
using Old8Lang.Interpreter;

namespace Old8Lang.App.Commands.Debug;

/// <summary>
/// 启动调试命令
/// </summary>
public class DebugStartCommand : ICommand
{
    public string Name => "debug-start";
    public string Description => "启动调试会话";
    public string Help => "用法: debug-start <文件路径>\n启动对指定Old8Lang文件的调试";

    public int Execute(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("错误: 请指定要调试的文件");
            Console.WriteLine(Help);
            return 1;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"错误: 文件不存在: {filePath}");
            return 1;
        }

        try
        {
            // 创建调试器实例
            var debugger = new Old8Lang.Debugger.Debugger();
            DebugService.SetDebugger(debugger);

            debugger.StartDebugging(filePath);
            Console.WriteLine($"已启动调试会话: {filePath}");

            // 设置事件处理器
            SetupEventHandlers(debugger);

            // 创建并启动调试解释器
            RunDebuggedProgram(filePath, debugger);

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动调试失败: {ex.Message}");
            return 1;
        }
    }

    private static void SetupEventHandlers(Old8Lang.Debugger.Debugger debugger)
    {
        debugger.StateChanged += (sender, e) =>
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[调试] {e.Message}");
            Console.ResetColor();
        };

        debugger.BreakpointHit += (sender, e) =>
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[断点] {e.Message}");
            Console.ResetColor();
            ShowContext(e.Position, debugger.CallStack);
        };

        debugger.ErrorOccurred += (sender, e) =>
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[错误] {e.Message}");
            Console.ResetColor();
        };
    }

    private static void RunDebuggedProgram(string filePath, Old8Lang.Debugger.Debugger debugger)
    {
        var interpreter = new LangInterpreter();
        var debugInterpreter = new DebuggableInterpreter(interpreter, debugger);

        try
        {
            var ast = interpreter.Build(Apis.FromFile(filePath));
            debugInterpreter.Execute(ast);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"程序执行错误: {ex.Message}");
        }
    }

    private static void ShowContext(SourcePosition position, CallStack callStack)
    {
        Console.WriteLine($"位置: 行 {position.Line}, 列 {position.Column}");

        var currentFrame = callStack.CurrentFrame;
        if (currentFrame != null)
        {
            Console.WriteLine($"当前函数: {currentFrame.FunctionName}");

            if (currentFrame.LocalVariables.Count > 0)
            {
                Console.WriteLine("局部变量:");
                foreach (var (name, value) in currentFrame.LocalVariables)
                {
                    Console.WriteLine($"  {name} = {value}");
                }
            }
        }
    }
}

/// <summary>
/// 断点管理命令
/// </summary>
public class DebugBreakpointCommand : ICommand
{
    public string Name => "debug-bp";
    public string Description => "断点管理";

    public string Help => "用法: debug-bp <子命令> [参数]\n" +
                          "子命令:\n" +
                          "  add <文件> <行号> [条件] - 添加断点\n" +
                          "  func <函数名> - 添加函数断点\n" +
                          "  list - 列出所有断点\n" +
                          "  remove <断点ID> - 移除断点\n" +
                          "  clear - 清除所有断点";

    public int Execute(string[] args)
    {
        var debugger = DebugService.GetDebugger();
        if (debugger == null)
        {
            Console.WriteLine("错误: 调试器未初始化");
            return 1;
        }

        if (args.Length == 0)
        {
            Console.WriteLine("错误: 缺少子命令");
            Console.WriteLine(Help);
            return 1;
        }

        var subCommand = args[0].ToLower();
        return subCommand switch
        {
            "add" => AddBreakpoint(args.Skip(1).ToArray(), debugger),
            "func" => AddFunctionBreakpoint(args.Skip(1).ToArray(), debugger),
            "list" => ListBreakpoints(debugger),
            "remove" => RemoveBreakpoint(args.Skip(1).ToArray(), debugger),
            "clear" => ClearBreakpoints(debugger),
            _ => InvalidSubCommand(subCommand)
        };
    }

    private static int AddBreakpoint(string[] args, Old8Lang.Debugger.Debugger debugger)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("错误: 请指定文件路径和行号");
            return 1;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"错误: 文件不存在: {filePath}");
            return 1;
        }

        if (!int.TryParse(args[1], out var line))
        {
            Console.WriteLine("错误: 行号必须是数字");
            return 1;
        }

        var condition = args.Length > 2 ? string.Join(" ", args.Skip(2)) : null;
        var bpId = debugger.BreakpointManager.AddLineBreakpoint(filePath, line, condition);
        Console.WriteLine($"已添加断点 #{bpId}: {filePath}:{line}" + (condition != null ? $" (条件: {condition})" : ""));
        return 0;
    }

    private static int AddFunctionBreakpoint(string[] args, Old8Lang.Debugger.Debugger debugger)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("错误: 请指定函数名");
            return 1;
        }

        var functionName = args[0];
        var bpId = debugger.BreakpointManager.AddFunctionBreakpoint(functionName);
        Console.WriteLine($"已添加函数断点 #{bpId}: {functionName}");
        return 0;
    }

    private static int ListBreakpoints(Old8Lang.Debugger.Debugger debugger)
    {
        var breakpoints = debugger.BreakpointManager.GetAllBreakpoints();
        if (breakpoints.Count == 0)
        {
            Console.WriteLine("没有设置断点");
            return 0;
        }

        Console.WriteLine("断点列表:");
        foreach (var bp in breakpoints)
        {
            var status = bp.IsEnabled ? "启用" : "禁用";
            Console.WriteLine($"  #{bp.Id} [{status}] {bp} (命中 {bp.HitCount} 次)");
        }

        return 0;
    }

    private static int RemoveBreakpoint(string[] args, Old8Lang.Debugger.Debugger debugger)
    {
        if (args.Length < 1 || !int.TryParse(args[0], out var id))
        {
            Console.WriteLine("错误: 请提供有效的断点ID");
            return 1;
        }

        var success = debugger.BreakpointManager.RemoveBreakpoint(id);
        if (success)
        {
            Console.WriteLine($"已移除断点 #{id}");
        }
        else
        {
            Console.WriteLine($"断点 #{id} 不存在");
        }

        return success ? 0 : 1;
    }

    private static int ClearBreakpoints(Old8Lang.Debugger.Debugger debugger)
    {
        debugger.BreakpointManager.ClearAllBreakpoints();
        Console.WriteLine("已清除所有断点");
        return 0;
    }

    private static int InvalidSubCommand(string subCommand)
    {
        Console.WriteLine($"错误: 未知子命令 '{subCommand}'");
        Console.WriteLine("用法: debug-bp <子命令> [参数]");
        return 1;
    }
}

/// <summary>
/// 调试控制命令
/// </summary>
public class DebugControlCommand : ICommand
{
    public string Name => "debug";
    public string Description => "调试控制";

    public string Help => "用法: debug <命令>\n" +
                          "命令:\n" +
                          "  continue - 继续执行\n" +
                          "  step - 单步执行\n" +
                          "  stepinto - 单步进入\n" +
                          "  stepover - 单步跳过\n" +
                          "  stepout - 单步跳出\n" +
                          "  pause - 暂停执行\n" +
                          "  stop - 停止调试\n" +
                          "  stack - 显示调用栈\n" +
                          "  vars - 显示变量";

    public int Execute(string[] args)
    {
        var debugger = DebugService.GetDebugger();
        if (debugger == null)
        {
            Console.WriteLine("错误: 调试器未初始化");
            return 1;
        }

        if (args.Length == 0)
        {
            Console.WriteLine("错误: 缺少命令");
            Console.WriteLine(Help);
            return 1;
        }

        var command = args[0].ToLower();
        return command switch
        {
            "continue" => Continue(debugger),
            "step" or "stepinto" => Step(StepType.StepInto, debugger),
            "stepover" => Step(StepType.StepOver, debugger),
            "stepout" => Step(StepType.StepOut, debugger),
            "pause" => Pause(debugger),
            "stop" => Stop(debugger),
            "stack" => ShowStack(debugger),
            "vars" => ShowVariables(debugger),
            _ => InvalidCommand(command)
        };
    }

    private static int Continue(Old8Lang.Debugger.Debugger debugger)
    {
        debugger.Continue();
        Console.WriteLine("继续执行");
        return 0;
    }

    private static int Step(StepType stepType, Old8Lang.Debugger.Debugger debugger)
    {
        debugger.Step(stepType);
        Console.WriteLine($"单步执行: {stepType}");
        return 0;
    }

    private static int Pause(Old8Lang.Debugger.Debugger debugger)
    {
        debugger.Pause();
        Console.WriteLine("暂停执行");
        return 0;
    }

    private static int Stop(Old8Lang.Debugger.Debugger debugger)
    {
        debugger.StopDebugging();
        DebugService.ClearDebugger();
        Console.WriteLine("停止调试");
        return 0;
    }

    private static int ShowStack(Old8Lang.Debugger.Debugger debugger)
    {
        var frames = debugger.CallStack.GetAllFrames();
        if (frames.Count == 0)
        {
            Console.WriteLine("调用栈为空");
            return 0;
        }

        Console.WriteLine("调用栈:");
        for (int i = 0; i < frames.Count; i++)
        {
            var frame = frames[i];
            var prefix = i == 0 ? "->" : "  ";
            Console.WriteLine($"{prefix} {frame}");
        }

        return 0;
    }

    private static int ShowVariables(Old8Lang.Debugger.Debugger debugger)
    {
        var currentFrame = debugger.CallStack.CurrentFrame;
        if (currentFrame == null)
        {
            Console.WriteLine("没有当前上下文");
            return 0;
        }

        Console.WriteLine("局部变量:");
        foreach (var kvp in currentFrame.LocalVariables)
        {
            Console.WriteLine($"  {kvp.Key} = {kvp.Value}");
        }

        return 0;
    }

    private static int InvalidCommand(string command)
    {
        Console.WriteLine($"错误: 未知命令 '{command}'");
        Console.WriteLine("用法: debug <命令>");
        return 1;
    }
}