using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang init - 项目初始化命令
/// </summary>
public class InitCommand : ICommand
{
    public string Name => "init";
    public string Description => "初始化 Old8Lang 项目";

    public string Help => @"
用法: old8lang init [选项]

选项:
  -y, --yes              使用默认配置快速创建
  --template <模板>       使用指定模板（library/application）
  -h, --help             显示帮助信息

示例:
  old8lang init                    # 交互式创建项目
  old8lang init -y                 # 使用默认配置
  old8lang init --template library  # 使用库模板
";

    public Task<int> ExecuteAsync(string[] args)
    {
        // 检查是否已存在项目配置
        var projectRoot = CommandHelper.FindProjectRoot();
        if (projectRoot != null)
        {
            CommandHelper.PrintError($"当前目录已存在 Old8Lang 项目: {projectRoot}");
            CommandHelper.PrintInfo("如果要重新初始化，请先删除 o8packages.json");
            return Task.FromResult(1);
        }

        var useDefaults = args.Contains("-y") || args.Contains("--yes");
        var template = GetTemplate(args);

        ProjectConfig config;

        if (useDefaults)
        {
            config = CreateDefaultConfig();
            CommandHelper.PrintInfo("使用默认配置创建项目...");
        }
        else
        {
            config = CreateInteractiveConfig();
        }

        // 应用模板
        if (!string.IsNullOrEmpty(template))
        {
            ApplyTemplate(config, template);
        }

        // 创建项目结构
        var currentDir = Directory.GetCurrentDirectory();
        CreateProjectStructure(currentDir, config);

        // 保存配置
        config.SaveToDirectory(currentDir);

        CommandHelper.PrintSuccess("项目初始化完成！");
        Console.WriteLine();
        Console.WriteLine("项目信息:");
        Console.WriteLine($"  名称: {config.Name}");
        Console.WriteLine($"  版本: {config.Version}");
        Console.WriteLine($"  描述: {config.Description}");
        Console.WriteLine($"  虚拟环境: {(config.PackageManager.UseVirtualEnv ? "已启用" : "未启用")}");
        Console.WriteLine();
        Console.WriteLine("下一步:");
        Console.WriteLine("  1. old8lang add <包名>     - 添加依赖包");
        Console.WriteLine("  2. old8lang install        - 安装所有依赖");
        Console.WriteLine($"  3. old8lang run {config.Main}  - 运行主文件");

        return Task.FromResult(0);
    }

    private ProjectConfig CreateDefaultConfig()
    {
        var dirName = new DirectoryInfo(Directory.GetCurrentDirectory()).Name;

        return new ProjectConfig
        {
            Name = dirName.ToLower().Replace(" ", "-"),
            Version = "1.0.0",
            Description = $"{dirName} project",
            Author = "",
            License = "MIT",
            Main = "src/main.old8",
            Old8Lang = new Old8LangConfig
            {
                Version = "^1.0.0",
                Runtime = "interpreter"
            },
            Dependencies = new Dictionary<string, string>(),
            DevDependencies = new Dictionary<string, string>(),
            Scripts = new Dictionary<string, string>
            {
                ["start"] = "old8lang run src/main.old8",
                ["test"] = "old8lang run tests/test_main.old8"
            },
            Repositories = ["https://packages.old8lang.org"],
            PackageManager = new PackageManagerConfig
            {
                UseVirtualEnv = true,
                PackagesDir = "./packages",
                AutoLock = true,
                Strict = false
            }
        };
    }

    private ProjectConfig CreateInteractiveConfig()
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("  Old8Lang 项目初始化");
        Console.WriteLine("========================================");
        Console.WriteLine();

        var dirName = new DirectoryInfo(Directory.GetCurrentDirectory()).Name;
        var defaultName = dirName.ToLower().Replace(" ", "-");

        var name = CommandHelper.ReadLine("项目名称", defaultName) ?? defaultName;
        var version = CommandHelper.ReadLine("项目版本", "1.0.0") ?? "1.0.0";
        var description = CommandHelper.ReadLine("项目描述", $"{name} project") ?? $"{name} project";
        var author = CommandHelper.ReadLine("作者", "");
        var license = CommandHelper.ReadLine("License", "MIT") ?? "MIT";
        var main = CommandHelper.ReadLine("入口文件", "src/main.old8") ?? "src/main.old8";
        var useVirtualEnv = CommandHelper.ReadYesNo("使用虚拟环境");
        var old8LangVersion = CommandHelper.ReadLine("Old8Lang 版本", "^1.0.0") ?? "^1.0.0";

        Console.WriteLine();

        return new ProjectConfig
        {
            Name = name,
            Version = version,
            Description = description,
            Author = author,
            License = license,
            Main = main,
            Old8Lang = new Old8LangConfig
            {
                Version = old8LangVersion,
                Runtime = "interpreter"
            },
            Dependencies = new Dictionary<string, string>(),
            DevDependencies = new Dictionary<string, string>(),
            Scripts = new Dictionary<string, string>
            {
                ["start"] = $"old8lang run {main}",
                ["test"] = "old8lang run tests/test_main.old8"
            },
            Repositories = ["https://packages.old8lang.org"],
            PackageManager = new PackageManagerConfig
            {
                UseVirtualEnv = useVirtualEnv,
                PackagesDir = "./packages",
                AutoLock = true,
                Strict = false
            }
        };
    }

    private void CreateProjectStructure(string projectRoot, ProjectConfig config)
    {
        // 创建 packages 目录
        if (config.PackageManager.UseVirtualEnv)
        {
            var packagesDir = Path.Combine(projectRoot, config.PackageManager.PackagesDir);
            if (!Directory.Exists(packagesDir))
            {
                Directory.CreateDirectory(packagesDir);
                CommandHelper.PrintSuccess($"创建目录: {config.PackageManager.PackagesDir}");
            }
        }

        // 创建 src 目录和主文件
        var mainFilePath = Path.Combine(projectRoot, config.Main ?? "src/main.old8");
        var mainDir = Path.GetDirectoryName(mainFilePath);

        if (!string.IsNullOrEmpty(mainDir) && !Directory.Exists(mainDir))
        {
            Directory.CreateDirectory(mainDir);
            CommandHelper.PrintSuccess($"创建目录: {mainDir}");
        }

        if (!File.Exists(mainFilePath))
        {
            var mainContent = "// " + config.Name + @" - 主程序入口

PrintLine(""Hello, Old8Lang!"")
PrintLine($""项目名称: " + config.Name + @""")
PrintLine($""版本: " + config.Version + @""")
";
            File.WriteAllText(mainFilePath, mainContent);
            CommandHelper.PrintSuccess($"创建文件: {config.Main}");
        }

        // 创建 tests 目录
        var testsDir = Path.Combine(projectRoot, "tests");
        if (!Directory.Exists(testsDir))
        {
            Directory.CreateDirectory(testsDir);
            CommandHelper.PrintSuccess("创建目录: tests");
        }

        // 创建测试文件
        var testFilePath = Path.Combine(testsDir, "test_main.old8");
        if (!File.Exists(testFilePath))
        {
            var testContent = """
                              // 测试文件
                              PrintLine("运行测试...")
                              PrintLine("✓ 所有测试通过")
                              """;
            File.WriteAllText(testFilePath, testContent);
            CommandHelper.PrintSuccess("创建文件: tests/test_main.old8");
        }

        // 创建 README.md
        var readmePath = Path.Combine(projectRoot, "README.md");
        if (!File.Exists(readmePath))
        {
            var readmeContent = $@"# {config.Name}

{config.Description}

## 安装

```bash
old8lang install
```

## 运行

```bash
old8lang run {config.Main}
```

## 测试

```bash
old8lang run tests/test_main.old8
```

## License

{config.License}
";
            File.WriteAllText(readmePath, readmeContent);
            CommandHelper.PrintSuccess("创建文件: README.md");
        }

        // 创建 dll 目录
        var dllDir = Path.Combine(projectRoot, "dll");
        if (!Directory.Exists(dllDir))
        {
            Directory.CreateDirectory(dllDir);
            CommandHelper.PrintSuccess("创建目录: dll");
        }

        // 创建 gitignore 文件
        var gitignorePath = Path.Combine(projectRoot, ".gitignore");
        if (!File.Exists(gitignorePath))
        {
            var gitignoreContent = """
                                   # 忽略 packages 目录
                                   packages/
                                                                      
                                   # 忽略 dll 目录
                                   dll/
                                                                      
                                   # 忽略 dist 目录
                                   dist/
                                   """;
            File.WriteAllText(gitignorePath, gitignoreContent);
        }
    }

    private string? GetTemplate(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--template" && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private void ApplyTemplate(ProjectConfig config, string template)
    {
        switch (template.ToLower())
        {
            case "library":
                config.Main = "src/index.old8";
                config.Scripts["build"] = "old8lang compile src/index.old8";
                CommandHelper.PrintInfo("应用库模板");
                break;

            case "application":
            case "app":
                // 默认就是应用模板
                CommandHelper.PrintInfo("应用应用程序模板");
                break;

            default:
                CommandHelper.PrintWarning($"未知模板: {template}，使用默认模板");
                break;
        }
    }
}