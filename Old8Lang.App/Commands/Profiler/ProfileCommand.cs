using Old8Lang.App.Services;
using Old8Lang.Profiler;

namespace Old8Lang.App.Commands.Profiler;

/// <summary>
/// 性能分析命令
/// </summary>
public class ProfileCommand : ICommand
{
    public string Name => "profile";
    public string Description => "性能分析工具";

    public string Help =>
        "用法: profile <子命令> [参数]\n" +
        "子命令:\n" +
        "  start <文件> [名称]  - 开始性能分析\n" +
        "  stop                    - 停止性能分析\n" +
        "  status                  - 查看分析状态\n" +
        "  clear                   - 清除当前会话";

    private static ProfilerManager Profiler => ProfilerService.GetProfiler();

    public async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("错误: 缺少子命令");
            Console.WriteLine(Help);
            return 1;
        }

        var subCommand = args[0].ToLower();
        return subCommand switch
        {
            "start" => StartProfiling(args.Skip(1).ToArray()),
            "stop" => StopProfiling(),
            "status" => ShowStatus(),
            "clear" => ClearSession(),
            _ => InvalidSubCommand(subCommand)
        };
    }

    private static int StartProfiling(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("错误: 请指定要分析的文件");
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
            var sessionName = args.Length > 1 ? args[1] : "";
            var sessionId = Profiler.StartProfiling(sessionName, filePath, "解释模式");

            Console.WriteLine("性能分析已开始");
            Console.WriteLine($"会话ID: {sessionId}");
            Console.WriteLine($"源文件: {filePath}");
            Console.WriteLine($"会话名称: {sessionName}");
            Console.WriteLine("运行 'profile stop' 停止分析");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动性能分析失败: {ex.Message}");
            return 1;
        }
    }

    private static int StopProfiling()
    {
        try
        {
            var summary = Profiler.StopProfiling();
            if (summary == null)
            {
                Console.WriteLine("没有正在进行的性能分析会话");
                return 1;
            }

            Console.WriteLine("性能分析已完成");
            Console.WriteLine($"会话时长: {summary.Session.DurationMs:F2}ms");

            var totalCalls = summary.Session.FunctionStats.Values.Sum(f => f.CallCount);
            Console.WriteLine($"函数调用总数: {totalCalls:N0}");
            Console.WriteLine($"性能分数: {summary.FormattedScore}");

            if (summary.Bottlenecks.Count > 0)
            {
                Console.WriteLine($"发现瓶颈: {summary.Bottlenecks.Count} 个");
            }
            else
            {
                Console.WriteLine("未发现明显性能瓶颈!");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"停止性能分析失败: {ex.Message}");
            return 1;
        }
    }

    private static int ShowStatus()
    {
        try
        {
            var status = Profiler.GetSessionStatus();

            Console.WriteLine("性能分析状态:");

            var isProfiling = (bool)status["isProfiling"];
            Console.WriteLine($"  正在分析: {isProfiling}");

            var hasSession = (bool)status["hasSession"];
            if (hasSession)
            {
                Console.WriteLine($"  会话ID: {status["sessionId"]}");
                Console.WriteLine($"  会话名称: {status["sessionName"]}");
                Console.WriteLine($"  执行时长: {status["durationMs"]:F2}ms");
                Console.WriteLine($"  函数数量: {status["functionCount"]}");
            }
            else
            {
                Console.WriteLine("  当前没有活跃的分析会话");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取状态失败: {ex.Message}");
            return 1;
        }
    }

    private static int ClearSession()
    {
        try
        {
            Profiler.ClearSession();
            Console.WriteLine("性能分析会话已清除");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"清除会话失败: {ex.Message}");
            return 1;
        }
    }

    private static int InvalidSubCommand(string subCommand)
    {
        Console.WriteLine($"错误: 未知子命令 '{subCommand}'");
        Console.WriteLine("用法: profile <子命令> [参数]\n" +
                          "子命令:\n" +
                          "  start <文件> [名称]  - 开始性能分析\n" +
                          "  stop                    - 停止性能分析\n" +
                          "  status                  - 查看分析状态\n" +
                          "  clear                   - 清除当前会话");
        return 1;
    }
}