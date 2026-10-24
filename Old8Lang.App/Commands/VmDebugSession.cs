using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Debugger;
using Old8Lang.Bytecode.VM;

namespace Old8Lang.App.Commands;

/// <summary>
/// <c>-vm --debug-session</c> 使用的极简交互式调试器。
/// </summary>
/// <remarks>
/// 只负责「暂停时打印现场 + 读一条命令」，断点/单步状态全部交给
/// <see cref="VMDebugger"/>；虚拟机通过 <see cref="VirtualMachine.DebugPaused"/> 事件回调进来。
/// 命令：
/// <c>c</c> 继续、<c>s</c> 单步进入、<c>n</c> 单步跳过、<c>o</c> 跳出、
/// <c>bt</c> 调用栈、<c>p &lt;name&gt;</c> 查看参数、<c>h</c> 帮助、<c>q</c> 退出。
/// </remarks>
public static class VmDebugSession
{
    /// <summary>用户输入 <c>q</c> 时抛出，用于中止虚拟机执行</summary>
    public sealed class AbortException : Exception
    {
    }

    /// <summary>创建绑定到指定调试器的暂停处理器</summary>
    public static Action<VirtualMachine, CallFrame, Instruction> CreateHandler(VMDebugger debugger) =>
        (vm, frame, instruction) => Handle(debugger, frame, instruction);

    private static void Handle(VMDebugger debugger, CallFrame frame, Instruction instruction)
    {
        int line = instruction.LineNumber ?? 0;
        Console.WriteLine();
        Console.WriteLine($"[调试] 暂停于 {frame.Function.Name} (行 {line}, 指令 {frame.IP - 1})");
        Console.WriteLine("输入 h 查看命令。");

        while (true)
        {
            Console.Write("(debug) ");
            var input = Console.ReadLine();
            if (input is null)
            {
                // 标准输入被关闭（例如管道）：当作继续，避免死循环
                debugger.Continue();
                return;
            }

            var parts = input.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                continue;
            }

            switch (parts[0].ToLowerInvariant())
            {
                case "c":
                case "continue":
                    debugger.Continue();
                    return;

                case "s":
                case "step":
                    debugger.StepInto();
                    return;

                case "n":
                case "next":
                    debugger.StepOver();
                    return;

                case "o":
                case "out":
                    debugger.StepOut();
                    return;

                case "bt":
                case "backtrace":
                    PrintCallStack(debugger);
                    break;

                case "p":
                case "print":
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("用法: p <变量名>");
                        break;
                    }

                    PrintVariable(frame, parts[1].Trim());
                    break;

                case "h":
                case "help":
                case "?":
                    PrintHelp();
                    break;

                case "q":
                case "quit":
                case "exit":
                    throw new AbortException();

                default:
                    Console.WriteLine($"未知命令: {parts[0]}（输入 h 查看帮助）");
                    break;
            }
        }
    }

    private static void PrintCallStack(VMDebugger debugger)
    {
        var frames = debugger.GetCallStack().ToArray();
        if (frames.Length == 0)
        {
            Console.WriteLine("(调用栈为空)");
            return;
        }

        Console.WriteLine("调用栈（栈顶在下）:");
        for (int i = frames.Length - 1; i >= 0; i--)
        {
            Console.WriteLine($"  #{frames.Length - 1 - i} {frames[i].FunctionName} (行 {frames[i].Line})");
        }
    }

    private static void PrintVariable(CallFrame frame, string name)
    {
        var variables = VirtualMachine.GetFrameVariables(frame);
        if (variables.TryGetValue(name, out var value))
        {
            Console.WriteLine($"{name} = {value switch { null => "null", _ => value.ToString() }}");
            return;
        }

        Console.WriteLine($"当前帧没有变量 '{name}'（可用: {string.Join(", ", variables.Keys)}）");
    }

    private static void PrintHelp() => Console.WriteLine(
        "命令:\n" +
        "  c / continue   继续执行\n" +
        "  s / step       单步进入\n" +
        "  n / next       单步跳过\n" +
        "  o / out        跳出当前函数\n" +
        "  bt             打印调用栈\n" +
        "  p <name>       查看参数值\n" +
        "  h / ?          帮助\n" +
        "  q / quit       退出");
}
