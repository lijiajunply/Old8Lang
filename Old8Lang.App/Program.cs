using Old8Lang.App.Commands;
using Old8Lang.App.Commands.Debug;
using Old8Lang.App.Commands.Profiler;
using Old8Lang.Interpreter;

namespace Old8Lang.App;

/// <summary>
/// 应用程序入口点，使用基于 ICommand 的命令架构
/// </summary>
public abstract class Program
{
    private static readonly CommandRegistry CommandRegistry = new();

    /// <summary>
    /// 主入口点
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>应用程序退出码</returns>
    [STAThread]
    private static int Main(string[] args)
    {
        // 注册所有命令
        RegisterCommands();

        // 调试模式下的默认参数设置
#if DEBUG
        if (args.Length == 0)
        {
            args =
            [
                "-f",
                "C:\\Projects\\RiderProjects\\Old8Lang\\test_langlist_conversions.old8"
            ];
        }
#endif

        // 如果没有提供命令行参数，从控制台读取输入
        if (args.Length == 0)
        {
            Console.Write("请输入命令 (输入 -h 获取帮助): ");
            var input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input))
            {
                args = input.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            }
        }

        // 初始化调试和日志设置
        var debugEnabled = false;
        var logLevel = Compiler.Compiler.LogLevel.Info;
        args = ParseDebugAndLogOptions(args, ref debugEnabled, ref logLevel);

        // 设置编译器的调试输出开关和日志级别
        Compiler.Compiler.DebugOutputEnabled = debugEnabled;
        Compiler.Compiler.CurrentLogLevel = logLevel;

        // 设置 PackageManager 的调试日志
        ProjectManagement.PackageManager.DebugEnabled = debugEnabled;

        // 交互式命令行模式
        if (args.Length == 0)
        {
            return RunInteractiveMode();
        }

        // 验证命令行参数
        if (args.Length < 1)
        {
            Console.WriteLine("错误: 缺少命令参数");
            Console.WriteLine("使用 -h 获取帮助");
            return 1;
        }

        // 获取命令名称
        var commandName = args[0];
        var commandArgs = args.Skip(1).ToArray();

        // 查找并执行命令
        var command = CommandRegistry.GetCommand(commandName);
        if (command != null)
        {
            try
            {
                return command.Execute(commandArgs);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"命令执行错误: {e.Message}");
                Console.WriteLine($"错误类型: {e.GetType().Name}");
#if DEBUG
                Console.WriteLine($"堆栈跟踪: {e.StackTrace}");
#endif
                Console.ResetColor();
                return 1;
            }
        }

        // 如果命令未找到，显示错误和帮助
        Console.WriteLine($"错误: 未知命令 '{commandName}'");
        Console.WriteLine("使用 -h 获取可用命令列表");
        return 1;
    }

    /// <summary>
    /// 注册所有可用命令
    /// </summary>
    private static void RegisterCommands()
    {
        // 执行命令
        CommandRegistry.Register(new FromFileCommand());
        CommandRegistry.Register(new CompilerCommand());
        CommandRegistry.Register(new SyntaxTestCommand());
        CommandRegistry.Register(new RunCommand());

        // 虚拟机命令
        CommandRegistry.Register(new VMCommand());
        CommandRegistry.Register(new CompileBytecodeCommand());
        CommandRegistry.Register(new ExecuteBytecodeCommand());

        // 信息命令
        CommandRegistry.Register(new HelpCommand());
        CommandRegistry.Register(new VersionCommand());

        // 项目管理命令
        CommandRegistry.Register(new InitCommand());
        CommandRegistry.Register(new InstallCommand());
        CommandRegistry.Register(new RemoveCommand());
        CommandRegistry.Register(new RestoreCommand());
        CommandRegistry.Register(new ListCommand());

        // 包打包和签名命令
        CommandRegistry.Register(new PackCommand());
        CommandRegistry.Register(new UnpackCommand());
        CommandRegistry.Register(new SignCommand());
        CommandRegistry.Register(new VerifyCommand());
        CommandRegistry.Register(new CertCommand());
        CommandRegistry.Register(new PublishCommand());

        // 调试命令
        CommandRegistry.Register(new DebugStartCommand());
        CommandRegistry.Register(new DebugBreakpointCommand());
        CommandRegistry.Register(new DebugControlCommand());

        // 性能分析命令
        CommandRegistry.Register(new ProfileCommand());

        // 别名支持 - 直接注册别名指向相同命令实例
        var listCommand = new ListCommand();
        CommandRegistry.Register(listCommand);
        CommandRegistry.Register(new CommandAlias("ls", listCommand, "list 命令的别名"));
    }

    /// <summary>
    /// 解析调试和日志选项
    /// </summary>
    /// <param name="args">原始参数数组</param>
    /// <param name="debugEnabled">调试启用标志</param>
    /// <param name="logLevel">日志级别</param>
    /// <returns>处理后的参数数组</returns>
    private static string[] ParseDebugAndLogOptions(string[] args, ref bool debugEnabled,
        ref Old8Lang.Compiler.Compiler.LogLevel logLevel)
    {
        var processedArgs = new List<string>(args);

        for (int i = 0; i < processedArgs.Count; i++)
        {
            if (processedArgs[i] == "-d" || processedArgs[i] == "--debug")
            {
                debugEnabled = true;
                logLevel = Compiler.Compiler.LogLevel.Debug;
                processedArgs.RemoveAt(i);
                i--;
            }
            else if (processedArgs[i] == "-l" || processedArgs[i] == "--log-level")
            {
                if (i + 1 < processedArgs.Count)
                {
                    var levelStr = processedArgs[i + 1].ToLower();
                    switch (levelStr)
                    {
                        case "error":
                            logLevel = Compiler.Compiler.LogLevel.Error;
                            break;
                        case "warning":
                            logLevel = Compiler.Compiler.LogLevel.Warning;
                            break;
                        case "info":
                            logLevel = Compiler.Compiler.LogLevel.Info;
                            break;
                        case "debug":
                            logLevel = Compiler.Compiler.LogLevel.Debug;
                            debugEnabled = true;
                            break;
                    }

                    processedArgs.RemoveRange(i, 2);
                    i--;
                }
            }
        }

        return processedArgs.ToArray();
    }

    /// <summary>
    /// 运行交互式命令行模式
    /// </summary>
    /// <returns>退出码</returns>
    private static int RunInteractiveMode()
    {
        Console.WriteLine("\n========================================");
        Console.WriteLine("    Old8Lang 交互式命令行模式");
        Console.WriteLine("========================================");
        Console.WriteLine("输入 'exit' 退出");
        Console.WriteLine("输入 '-h' 获取帮助");
        Console.WriteLine("========================================\n");

        var interpreter = new LangInterpreter();

        while (true)
        {
            Console.Write("> ");
            var code = Console.ReadLine();
            if (string.IsNullOrEmpty(code)) continue;

            if (code == "exit")
            {
                Console.WriteLine("\n感谢使用 Old8Lang！");
                return 0;
            }

            if (code == "-h")
            {
                var helpCommand = CommandRegistry.GetCommand("-h");
                if (helpCommand != null)
                {
                    helpCommand.Execute([]);
                }

                continue;
            }

            // 检查是否为命令
            var parts = code.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                var command = CommandRegistry.GetCommand(parts[0]);
                if (command != null)
                {
                    var commandArgs = parts.Skip(1).ToArray();
                    try
                    {
                        command.Execute(commandArgs);
                        continue;
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"命令执行错误: {e.Message}");
                        Console.ResetColor();
                        continue;
                    }
                }
            }

            // 否则作为代码执行
            try
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                var ast = interpreter.Build(code: code);
                ast.Run(interpreter.Manager);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"执行错误: {e.Message}");
                Console.WriteLine($"错误类型: {e.GetType().Name}");
#if DEBUG
                Console.WriteLine($"堆栈跟踪: {e.StackTrace}");
#endif
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}

/// <summary>
/// 命令别名实现，用于支持命令的短名称
/// </summary>
internal class CommandAlias(string name, ICommand targetCommand, string description) : ICommand
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public string Help { get; } = $"别名命令，等价于 '{targetCommand.Name}'";

    public int Execute(string[] args)
    {
        return targetCommand.Execute(args);
    }
}