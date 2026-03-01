using Old8Lang.Interpreter;
using Old8Lang.LangParser;
using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// 解释执行文件命令
/// </summary>
public class FromFileCommand : ICommand
{
    public string Name => "-f";
    public string Description => "解释执行指定的 .old8 或 .ol 文件";
    public string Help => "使用: Old8Lang.App -f <文件名> [-D SYMBOL1] ... [--env <environment>] [--perf] [--perf-detailed] [--perf-output <file>]";

    public int Execute(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("错误: 缺少文件参数");
            Console.WriteLine(Help);
            return 1;
        }

        // 解析命令行参数
        var fileName = args[0];
        var symbols = new List<string>();
        string? environment = null;
        bool enablePerf = false;
        bool enablePerfDetailed = false;
        string? perfOutputFile = null;

        // 解析 -D、--env、--perf、--perf-detailed、--perf-output 参数
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "-D" && i + 1 < args.Length)
            {
                symbols.Add(args[i + 1]);
                i++;
            }
            else if (args[i] == "--env" && i + 1 < args.Length)
            {
                environment = args[i + 1];
                i++;
            }
            else if (args[i] == "--perf")
            {
                enablePerf = true;
            }
            else if (args[i] == "--perf-detailed")
            {
                enablePerf = true;
                enablePerfDetailed = true;
            }
            else if (args[i] == "--perf-output" && i + 1 < args.Length)
            {
                perfOutputFile = args[i + 1];
                i++;
            }
        }

        // 检查是否在项目模式下运行
        var projectRoot = ProjectConfig.FindProjectRoot(Path.GetDirectoryName(Path.GetFullPath(fileName)) ?? Directory.GetCurrentDirectory());

        if (projectRoot != null)
        {
            var projectConfig = ProjectConfig.LoadFromDirectory(projectRoot);
            if (projectConfig != null)
            {
                var envManager = new EnvironmentManager(projectConfig, environment);

                Console.WriteLine($"[项目模式] 环境: {envManager.CurrentEnvironment}");
                Console.WriteLine($"[项目模式] 运行时: {envManager.GetRuntimeMode()}");

                foreach (var (key, value) in envManager.GetAllVariables())
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }

        PreprocessorSymbols preprocessorSymbols = new PreprocessorSymbols(symbols);

        // 配置性能监控
        PerformanceMonitor? monitor = null;
        if (enablePerf)
        {
            monitor = new PerformanceMonitor();
            monitor.StartMonitoring(new PerformanceMonitorConfig
            {
                Enabled = true,
                DetailedMonitoring = enablePerfDetailed,
                EnableMemoryTracking = true,
                EnableCacheTracking = true
            });
        }

        var langInterpreter = new LangInterpreter(monitor);

        try
        {
            var code = Apis.FromFile(fileName);
            var ast = langInterpreter.Build(code, fileName, preprocessorSymbols);
            ast.Run(langInterpreter.Manager);

            // 输出性能报告
            if (enablePerf && monitor != null)
            {
                monitor.StopMonitoring();
                var metrics = monitor.GetMetrics();
                var reporter = new PerformanceReporter();

                if (perfOutputFile != null)
                {
                    var ext = Path.GetExtension(perfOutputFile).ToLowerInvariant();
                    string report = ext switch
                    {
                        ".json" => reporter.GenerateJsonReport(metrics),
                        ".csv" => reporter.GenerateCsvReport(metrics),
                        _ => reporter.GenerateTextReport(metrics)
                    };
                    reporter.SaveReport(report, perfOutputFile);
                    Console.WriteLine($"性能报告已保存到: {perfOutputFile}");
                }
                else
                {
                    Console.WriteLine(reporter.GenerateTextReport(metrics));
                }
            }

            return 0;
        }
        catch (Exception e)
        {
            monitor?.StopMonitoring();
#if DEBUG
            throw;
#endif
            Console.WriteLine(e.Message);
            return 1;
        }
    }
}

