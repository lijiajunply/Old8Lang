using System.Diagnostics;
using Old8Lang;
using Old8Lang.App;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;

// 调试模式下的默认参数设置
#if DEBUG
if (args.Length == 0)
{
    args =
    [
        "-f", "/Users/luckyfish/Documents/Project/RiderProjects/Old8Lang/InterpreterTests/test_thread_basic.old8"
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
var logLevel = Compiler.LogLevel.Info;

// 解析调试和日志级别参数
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "-d" || args[i] == "--debug")
    {
        debugEnabled = true;
        logLevel = Compiler.LogLevel.Debug;
        // 移除调试参数，避免影响后续命令解析
        var newArgs = new List<string>(args);
        newArgs.RemoveAt(i);
        args = newArgs.ToArray();
        i--;
    }
    else if (args[i] == "-l" || args[i] == "--log-level")
    {
        if (i + 1 < args.Length)
        {
            var levelStr = args[i + 1].ToLower();
            switch (levelStr)
            {
                case "error":
                    logLevel = Compiler.LogLevel.Error;
                    break;
                case "warning":
                    logLevel = Compiler.LogLevel.Warning;
                    break;
                case "info":
                    logLevel = Compiler.LogLevel.Info;
                    break;
                case "debug":
                    logLevel = Compiler.LogLevel.Debug;
                    debugEnabled = true;
                    break;
            }

            // 移除日志级别参数
            var newArgs = new List<string>(args);
            newArgs.RemoveRange(i, 2);
            args = newArgs.ToArray();
            i--;
        }
    }
}

// 设置编译器的调试输出开关和日志级别
Compiler.DebugOutputEnabled = debugEnabled;
Compiler.CurrentLogLevel = logLevel;

// 读取语言配置信息
var langInfo = Apis.ReadJson();

// 交互式命令行模式
if (args.Length == 0)
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
            return;
        }

        if (code == "-h")
        {
            Console.WriteLine(BasicInfo.Help);
            continue;
        }

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

// 验证命令行参数
if (args.Length < 1)
{
    Console.WriteLine("错误: 缺少命令参数");
    Console.WriteLine("使用 -h 获取帮助");
    return;
}

// 获取命令名称
var command = args[0];
var fromFileCmd = BasicInfo.Order["FromFile"]; // 解释执行文件命令
var compilerCmd = BasicInfo.Order["Compiler"]; // 编译执行文件命令
var syntaxTestCmd = BasicInfo.Order["SyntaxTest"]; // 语法测试命令
var helpCmd = BasicInfo.Order["Help"]; // 帮助命令
var varCmd = BasicInfo.Order["Var"]; // 版本信息命令
var infoCmd = BasicInfo.Order["Info"]; // 语言信息命令
var importCmd = BasicInfo.Order["Import"]; // 导入库信息命令
var changeImportCmd = BasicInfo.Order["ChangeImport"]; // 修改导入路径命令
var installCmd = BasicInfo.Order["Install"]; // 安装库命令
var removeCmd = BasicInfo.Order["Remove"]; // 移除库命令

// 处理帮助命令
if (command == helpCmd)
{
    Console.WriteLine(BasicInfo.Help);
    return;
}

// 处理版本信息命令
if (command == varCmd)
{
    Console.WriteLine($"Old8Lang 版本: {langInfo.Var}");
    return;
}

// 处理语言信息命令
if (command == infoCmd)
{
    Console.WriteLine("========================================");
    Console.WriteLine(BasicInfo.Info());
    Console.WriteLine("========================================");
    return;
}

// 处理导入库信息命令
if (command == importCmd)
{
    Console.WriteLine("========================================");
    Console.WriteLine("导入库信息:");
    Console.WriteLine("========================================");
    foreach (var libInfo in langInfo.LibInfos)
    {
        Console.WriteLine(
            $"库名: {libInfo.LibName} | 版本: {libInfo.Var} | 类型: {(libInfo.IsDir ? "目录" : "文件")}");
    }

    Console.WriteLine($"\n导入路径: {langInfo.ImportPath}");
    Console.WriteLine("========================================");
    return;
}

// 处理修改导入路径命令
if (command == changeImportCmd)
{
    if (args.Length < 2)
    {
        Console.WriteLine("错误: 缺少导入路径参数");
        Console.WriteLine("使用: Old8Lang.App -change <路径>");
        return;
    }

    var newPath = args[1];
    var updatedInfo = Apis.ChangeBasicInfo(newPath, langInfo.Var);
    Console.WriteLine($"\n导入路径已更新为: {updatedInfo.ImportPath}");
    return;
}

// 处理安装命令（占位，尚未实现）
if (command == installCmd)
{
    if (args.Length < 2)
    {
        Console.WriteLine("错误: 缺少安装包参数");
        Console.WriteLine("使用: Old8Lang.App -i <包名>");
        return;
    }

    Console.WriteLine($"安装命令已接收，包名: {args[1]}");
}

// 处理移除命令（占位，尚未实现）
if (command == removeCmd)
{
    if (args.Length < 2)
    {
        Console.WriteLine("错误: 缺少库名参数");
        Console.WriteLine("使用: Old8Lang.App -r <库名>");
        return;
    }

    Console.WriteLine($"删除命令已接收，库名: {args[1]}");
}

// 处理解释执行文件命令
if (command == fromFileCmd)
{
    if (args.Length < 2)
    {
        Console.WriteLine("错误: 缺少文件参数");
        Console.WriteLine("使用: Old8Lang.App -f <文件名>");
        return;
    }

    var langInterpreter = new LangInterpreter();

    try
    {
        var code = Apis.FromFile(args[1]);
        var ast = langInterpreter.Build(code, args[1]);
        ast.Run(langInterpreter.Manager);
    }
    catch (Exception e)
    {
#if DEBUG
        throw;
#endif
        Console.WriteLine(e.Message);
    }

    return;
}

// 处理编译执行文件命令
if (command == compilerCmd)
{
    if (args.Length < 2)
    {
        Console.WriteLine("错误: 缺少文件参数");
        Console.WriteLine("使用: Old8Lang.App -c <文件名>");
        return;
    }

    // 验证文件扩展名
    var ext = Path.GetExtension(args[1]).ToLower();
    if (ext != ".old8" && ext != ".ol")
    {
        Console.WriteLine($"不支持的文件扩展名: {ext}，仅支持 .old8 和 .ol 文件");
        return;
    }

    var interpreter = new LangInterpreter();
    var stopwatch = new Stopwatch();

    // 测量解析时间
    stopwatch.Start();
    var ast = interpreter.Build(Apis.FromFile(args[1]), args[1]);
    stopwatch.Stop();
    var parseTime = stopwatch.Elapsed.TotalMilliseconds;
    var timeInfo = $"------------------\nParser Build Time : {parseTime}ms\n";
    var totalTime = parseTime;

    // 编译代码
    var compiledAction = Compiler.Compile(ast, args[1], interpreter);

    // 测量执行时间
    stopwatch.Restart();
    try
    {
        compiledAction();
    }
    catch (Exception e)
    {
#if DEBUG
        throw;
#endif
        Console.WriteLine(e.Message);
    }

    stopwatch.Stop();
    var executionTime = stopwatch.Elapsed.TotalMilliseconds;
    timeInfo += $"Process Run Time : {executionTime}ms\n";
    totalTime += executionTime;
    timeInfo += $"Total : {totalTime}ms";
    Console.WriteLine(timeInfo);
}

// 处理语法测试命令
if (command == syntaxTestCmd)
{
    if (args.Length < 2)
    {
        Console.WriteLine("错误: 缺少文件参数");
        Console.WriteLine("使用: Old8Lang.App -s <文件名>");
        return;
    }

    // 验证文件扩展名
    var ext = Path.GetExtension(args[1]).ToLower();
    if (ext != ".old8" && ext != ".ol")
    {
        Console.WriteLine($"不支持的文件扩展名: {ext}，仅支持 .old8 和 .ol 文件");
        return;
    }

    try
    {
        var interpreter = new LangInterpreter();
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var code = Apis.FromFile(args[1]);
        var ast = interpreter.Build(code, args[1]);
        stopwatch.Stop();
        var parseTime = stopwatch.Elapsed.TotalMilliseconds;

        Console.WriteLine(
            $"------------------\nSyntax Test Result\nParser Build Time : {parseTime}ms\n------------------");
        Console.WriteLine(ast.ToCode());
    }
    catch (Exception e)
    {
#if DEBUG
        throw;
#endif
        Console.WriteLine(e.Message);
    }
}