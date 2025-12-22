// Program.cs
using Old8Lang.CodeGen.Scanner;
using Old8Lang.CodeGen.Generator;
using Old8Lang.CodeGen.Configuration;
using Old8Lang.CodeGen.Utils;

namespace Old8Lang.CodeGen;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         Old8Lang Visitor 代码生成器 v1.0                  ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // 检查命令
        if (args.Length > 0 && args[0] == "make-partial")
        {
            return await MakePartialCommand(args.Skip(1).ToArray());
        }

        // 解析命令行参数
        var configPath = "visitor-codegen.json";
        var preview = false;
        var incremental = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--config":
                    if (i + 1 < args.Length)
                    {
                        configPath = args[++i];
                    }
                    break;
                case "--preview":
                    preview = true;
                    break;
                case "--incremental":
                    incremental = true;
                    break;
                case "--help":
                case "-h":
                    ShowHelp();
                    return 0;
            }
        }

        Console.WriteLine($"[INFO] 开始生成 Visitor 代码...");
        Console.WriteLine($"[INFO] 配置文件: {configPath}");
        Console.WriteLine($"[INFO] 预览模式: {(preview ? "是" : "否")}");
        Console.WriteLine($"[INFO] 增量模式: {(incremental ? "是" : "否")}");
        Console.WriteLine();

        try
        {
            await GenerateVisitorCode(configPath, preview, incremental);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("[ERROR] 代码生成失败！");
            Console.WriteLine($"错误信息: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            return 1;
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("用法:");
        Console.WriteLine("  dotnet run [命令] [选项]");
        Console.WriteLine();
        Console.WriteLine("命令:");
        Console.WriteLine("  (无)                生成 Visitor 模式代码（默认）");
        Console.WriteLine("  make-partial        将 AST 节点类标记为 partial class");
        Console.WriteLine();
        Console.WriteLine("选项:");
        Console.WriteLine("  --config <路径>     指定配置文件路径 (默认: visitor-codegen.json)");
        Console.WriteLine("  --preview           预览模式，不写入文件");
        Console.WriteLine("  --incremental       增量模式，只生成变更的文件");
        Console.WriteLine("  --dry-run           试运行模式（仅用于 make-partial）");
        Console.WriteLine("  --help, -h          显示帮助信息");
        Console.WriteLine();
        Console.WriteLine("示例:");
        Console.WriteLine("  dotnet run");
        Console.WriteLine("  dotnet run --preview");
        Console.WriteLine("  dotnet run --config my-config.json");
        Console.WriteLine("  dotnet run make-partial");
        Console.WriteLine("  dotnet run make-partial --dry-run");
    }

    static async Task<int> MakePartialCommand(string[] args)
    {
        Console.WriteLine("[INFO] 开始 make-partial 命令...");
        Console.WriteLine();

        var configPath = "visitor-codegen.json";
        var dryRun = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--config":
                    if (i + 1 < args.Length)
                    {
                        configPath = args[++i];
                    }
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
            }
        }

        try
        {
            // 1. 加载配置
            var config = CodeGenConfig.Load(configPath);
            Console.WriteLine($"[INFO] 扫描目录: {config.ScanDirectory}");

            if (dryRun)
            {
                Console.WriteLine("[INFO] 模式: 试运行（不会修改文件）");
            }
            Console.WriteLine();

            // 2. 扫描节点
            Console.WriteLine("[STEP 1/2] 扫描 AST 节点...");
            var scanner = new AstNodeScanner(
                config.ScanDirectory,
                new HashSet<string>(config.ExcludeClasses),
                config.ExcludePatterns
            );
            var nodes = scanner.ScanNodes();
            Console.WriteLine($"[SUCCESS] 发现 {nodes.Count} 个 AST 节点");
            Console.WriteLine();

            // 3. 转换为 partial class
            Console.WriteLine("[STEP 2/2] 转换类为 partial class...");
            var converter = new PartialClassConverter(nodes, dryRun);
            var convertedCount = await converter.ConvertAll();

            Console.WriteLine();
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  🎉 转换完成！                             ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine($"已处理 {convertedCount} 个文件");

            if (dryRun)
            {
                Console.WriteLine();
                Console.WriteLine("提示: 这是试运行模式，没有实际修改文件。");
                Console.WriteLine("      移除 --dry-run 参数以应用修改。");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("[ERROR] make-partial 命令失败！");
            Console.WriteLine($"错误信息: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            return 1;
        }
    }

    static async Task GenerateVisitorCode(string configPath, bool preview, bool incremental)
    {
        // 1. 加载配置
        var config = CodeGenConfig.Load(configPath);
        Console.WriteLine($"[INFO] 扫描目录: {config.ScanDirectory}");
        Console.WriteLine($"[INFO] 输出目录: {config.OutputDirectory}");
        Console.WriteLine();

        // 2. 扫描节点
        Console.WriteLine("[STEP 1/4] 扫描 AST 节点...");
        var scanner = new AstNodeScanner(
            config.ScanDirectory,
            new HashSet<string>(config.ExcludeClasses),
            config.ExcludePatterns
        );
        var nodes = scanner.ScanNodes();
        Console.WriteLine($"[SUCCESS] 发现 {nodes.Count} 个 AST 节点");

        // 分类统计
        var statementCount = nodes.Count(n => n.Category == AstNodeCategory.Statement);
        var expressionCount = nodes.Count(n => n.Category == AstNodeCategory.Expression);
        var valueCount = nodes.Count(n => n.Category == AstNodeCategory.Value);
        Console.WriteLine($"[INFO] 分类统计:");
        Console.WriteLine($"  - Statement: {statementCount} 个");
        Console.WriteLine($"  - Expression: {expressionCount} 个");
        Console.WriteLine($"  - Value: {valueCount} 个");
        Console.WriteLine();

        // 3. 生成 IVisitor 接口
        Console.WriteLine("[STEP 2/4] 生成 IVisitor 接口...");
        var visitorGen = new VisitorInterfaceGenerator(nodes);
        var visitorCode = visitorGen.Generate();
        var visitorPath = Path.Combine(config.OutputDirectory, "IVisitor.generated.cs");

        if (preview)
        {
            Console.WriteLine();
            Console.WriteLine("═══════ IVisitor.generated.cs 预览 (前1000字符) ═══════");
            Console.WriteLine(visitorCode.Substring(0, Math.Min(1000, visitorCode.Length)));
            if (visitorCode.Length > 1000)
            {
                Console.WriteLine("...");
                Console.WriteLine($"(已截断，总长度: {visitorCode.Length} 字符)");
            }
            Console.WriteLine();
        }
        else
        {
            Directory.CreateDirectory(config.OutputDirectory);
            await File.WriteAllTextAsync(visitorPath, visitorCode);
            Console.WriteLine($"[SUCCESS] 生成 IVisitor.generated.cs ({visitorCode.Length} 字符)");
        }

        // 4. 生成 Accept 方法
        Console.WriteLine();
        Console.WriteLine("[STEP 3/4] 生成 Accept 方法...");
        var acceptGen = new AcceptMethodGenerator(nodes);
        var acceptFiles = acceptGen.GenerateAll();

        if (preview)
        {
            Console.WriteLine($"[INFO] 将生成 {acceptFiles.Count} 个 Accept 方法文件");
            var sampleFile = acceptFiles.First();
            Console.WriteLine();
            Console.WriteLine($"═══════ {sampleFile.Key} 预览 ═══════");
            Console.WriteLine(sampleFile.Value);
            Console.WriteLine();
            Console.WriteLine($"... 还有 {acceptFiles.Count - 1} 个文件");
        }
        else
        {
            int writtenCount = 0;
            foreach (var (fileName, code) in acceptFiles)
            {
                var filePath = Path.Combine(config.OutputDirectory, fileName);
                if (!incremental || !File.Exists(filePath) || File.ReadAllText(filePath) != code)
                {
                    await File.WriteAllTextAsync(filePath, code);
                    writtenCount++;
                }
            }
            Console.WriteLine($"[SUCCESS] 生成 {writtenCount}/{acceptFiles.Count} 个 Accept 方法文件");
        }

        // 5. 完成
        Console.WriteLine();
        Console.WriteLine("[STEP 4/4] 代码生成完成");
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                  🎉 代码生成成功！                         ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");

        if (!preview)
        {
            Console.WriteLine();
            Console.WriteLine($"输出目录: {Path.GetFullPath(config.OutputDirectory)}");
            Console.WriteLine();
            Console.WriteLine("下一步:");
            Console.WriteLine("1. 将所有 AST 节点类标记为 partial class");
            Console.WriteLine("2. 在 IOldLangTree 接口中添加 Accept 方法声明");
            Console.WriteLine("3. 编译项目验证生成的代码");
        }
    }
}
