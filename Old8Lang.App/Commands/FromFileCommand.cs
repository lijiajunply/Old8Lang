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
    public string Help => "使用: Old8Lang.App -f <文件名> [-D SYMBOL1] [-D SYMBOL2] ... [--env <environment>]";

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

        // 解析 -D 和 --env 参数
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "-D" && i + 1 < args.Length)
            {
                symbols.Add(args[i + 1]);
                i++; // 跳过符号名
            }
            else if (args[i] == "--env" && i + 1 < args.Length)
            {
                environment = args[i + 1];
                i++; // 跳过环境名
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

                // 应用环境变量到系统环境
                foreach (var (key, value) in envManager.GetAllVariables())
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }

        // 总是创建预编译符号管理器（即使没有符号也可以处理#define等指令）
        PreprocessorSymbols preprocessorSymbols = new PreprocessorSymbols(symbols);

        var langInterpreter = new LangInterpreter();

        try
        {
            var code = Apis.FromFile(fileName);
            var ast = langInterpreter.Build(code, fileName, preprocessorSymbols);
            ast.Run(langInterpreter.Manager);
            return 0;
        }
        catch (Exception e)
        {
#if DEBUG
            throw;
#endif
            Console.WriteLine(e.Message);
            return 1;
        }
    }
}
