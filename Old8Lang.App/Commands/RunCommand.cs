using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// 运行命令 - 智能运行文件或项目
/// </summary>
public class RunCommand : ICommand
{
    public string Name => "run";
    public string Description => "智能运行文件或项目";
    public string Help => @"使用方法:
  Old8Lang.App run [script-name]              - 运行项目中的脚本
  Old8Lang.App run [-s|-f|-c] <file>          - 使用指定模式运行文件
  Old8Lang.App run <file>                     - 使用解释模式运行文件

参数说明:
  script-name    - 项目配置中定义的脚本名称
  -s             - 语法检查模式
  -f             - 解释执行模式（默认）
  -c             - 编译执行模式
  <file>         - 要运行的文件路径

项目模式:
  - 自动检测项目配置 (o8package.json)
  - 根据项目配置中的 runtime 设置选择运行模式
  - 支持项目脚本定义";

    public async Task<int> ExecuteAsync(string[] args)
    {
        // 检查帮助参数
        if (args.Length >= 1 && (args[0] == "--help" || args[0] == "-h"))
        {
            Console.WriteLine(Help);
            return 0;
        }

        try
        {
            // 检测是否在项目中
            var currentDir = Directory.GetCurrentDirectory();
            var projectRoot = ProjectConfig.FindProjectRoot(currentDir);
            var isProject = projectRoot != null;

            if (args.Length == 0)
            {
                Console.WriteLine("错误: 缺少参数");
                Console.WriteLine(Help);
                return 1;
            }

            // 检查第一个参数是否是文件路径
            var firstArg = args[0];
            var isFilePath = File.Exists(firstArg) && 
                           (Path.GetExtension(firstArg).ToLower() == ".old8" || 
                            Path.GetExtension(firstArg).ToLower() == ".ol");

            if (isProject && !isFilePath && args.Length >= 1)
            {
                // 项目模式 - 第一个参数是脚本名称
                return await RunProjectMode(projectRoot!, args);
            }
            else
            {
                // 文件模式或项目中的文件运行
                return await RunFileMode(args);
            }
        }
        catch (Exception e)
        {
#if DEBUG
            throw;
#endif
            Console.WriteLine($"运行错误: {e.Message}");
            return 1;
        }
    }

    /// <summary>
    /// 项目模式运行
    /// </summary>
    private async Task<int> RunProjectMode(string projectRoot, string[] args)
    {
        var config = ProjectConfig.LoadFromDirectory(projectRoot);
        if (config == null)
        {
            Console.WriteLine("错误: 无法加载项目配置");
            return 1;
        }

        // 检查第一个参数是否为脚本名称
        var scriptName = args[0];
        
        // 检查是否为模式参数（-s, -f, -c）
        if (scriptName.StartsWith("-"))
        {
            // 这是文件模式，不是脚本名称
            return await RunFileMode(args);
        }

        // 检查是否有该脚本
        if (!config.Scripts.TryGetValue(scriptName, out var scriptCommand))
        {
            Console.WriteLine($"错误: 脚本 '{scriptName}' 未在项目配置中定义");
            Console.WriteLine($"可用脚本: {string.Join(", ", config.Scripts.Keys)}");
            return 1;
        }

        // 执行脚本命令
        Console.WriteLine($"运行脚本: {scriptName}");
        Console.WriteLine($"命令: {scriptCommand}");
        
        // 解析脚本命令参数
        var scriptArgs = ParseScriptCommand(scriptCommand, args.Skip(1).ToArray());
        
        // 执行脚本命令
        var commandName = scriptArgs[0];
        var commandArgs = scriptArgs.Skip(1).ToArray();
        
        // 获取命令实例
        ICommand? command = commandName switch
        {
            "-s" => new SyntaxTestCommand(),
            "-f" => new FromFileCommand(),
            "-c" => new CompilerCommand(),
            "run" => new RunCommand(),
            _ => null
        };
        
        if (command == null)
        {
            Console.WriteLine($"错误: 脚本中的命令 '{commandName}' 未找到或不支持");
            return 1;
        }

        return await command.ExecuteAsync(commandArgs);
    }

    /// <summary>
    /// 文件模式运行
    /// </summary>
    private async Task<int> RunFileMode(string[] args)
    {
        string mode;
        string filePath;

        // 检测是否在项目中以获取默认运行模式
        var currentDir = Directory.GetCurrentDirectory();
        var projectRoot = ProjectConfig.FindProjectRoot(currentDir);
        var projectConfig = projectRoot != null ? ProjectConfig.LoadFromDirectory(projectRoot) : null;

        if (args.Length == 1)
        {
            // 只有一个参数，假设是文件路径
            mode = "-f"; // 默认解释模式
            filePath = args[0];
            
            // 如果在项目中，使用项目配置的运行时模式
            if (projectConfig?.Old8Lang?.Runtime?.ToLower() == "compiler")
            {
                mode = "-c";
            }
        }
        else if (args.Length >= 2)
        {
            // 第一个参数是模式，第二个是文件路径
            mode = args[0];
            filePath = args[1];

            // 验证模式参数
            if (mode != "-s" && mode != "-f" && mode != "-c")
            {
                // 第一个参数不是模式参数，假设是文件路径，使用项目配置或默认模式
                mode = "-f";
                filePath = args[0];
                
                // 如果在项目中，使用项目配置的运行时模式
                if (projectConfig?.Old8Lang?.Runtime?.ToLower() == "compiler")
                {
                    mode = "-c";
                }
            }
        }
        else
        {
            Console.WriteLine("错误: 缺少文件参数");
            Console.WriteLine(Help);
            return 1;
        }

        // 验证文件是否存在
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"错误: 文件 '{filePath}' 不存在");
            return 1;
        }

        // 验证文件扩展名
        var ext = Path.GetExtension(filePath).ToLower();
        if (ext != ".old8" && ext != ".ol")
        {
            Console.WriteLine($"错误: 不支持的文件扩展名 '{ext}'，仅支持 .old8 和 .ol 文件");
            return 1;
        }

        // 根据模式执行相应的命令
        ICommand? command = mode switch
        {
            "-s" => new SyntaxTestCommand(),
            "-f" => new FromFileCommand(),
            "-c" => new CompilerCommand(),
            _ => null
        };
        
        if (command == null)
        {
            Console.WriteLine($"错误: 不支持的模式 '{mode}'");
            return 1;
        }

        var commandArgs = new[] { filePath };
        return await command.ExecuteAsync(commandArgs);
    }

    /// <summary>
    /// 解析脚本命令，支持参数替换
    /// </summary>
    private string[] ParseScriptCommand(string scriptCommand, string[] additionalArgs)
    {
        // 简单的参数替换实现
        var parts = scriptCommand.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();

        foreach (var part in parts)
        {
            if (part == "$@" || part == "$*")
            {
                // $@ 和 $* 表示所有额外参数
                result.AddRange(additionalArgs);
            }
            else if (part.StartsWith("$") && part.Length > 1)
            {
                // $1, $2 等位置参数
                if (int.TryParse(part[1..], out var index) && index > 0 && index <= additionalArgs.Length)
                {
                    result.Add(additionalArgs[index - 1]);
                }
            }
            else
            {
                result.Add(part);
            }
        }

        return result.ToArray();
    }
}