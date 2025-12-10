using System.Diagnostics;
using Old8Lang;
using Old8Lang.App;
using Old8Lang.Compiler;
using Old8Lang.LangParser;

// 调试模式下的默认参数
#if DEBUG
if (args.Length == 0)
{
    args =
    [
        "-f", "/Users/luckyfish/Documents/Project/RiderProjects/Old8Lang/Old8Lang/Ex/init.old8"
    ];
}
#endif

// 读取命令行参数
if (args.Length == 0)
{
    Console.Write("请输入命令 (输入 -h 获取帮助): ");
    var input = Console.ReadLine();
    if (!string.IsNullOrEmpty(input))
    {
        args = input.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
    }
}

var langInfo = Apis.ReadJson();

// 命令行模式
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

// 处理命令
var command = args[0];
var fromFileCmd = BasicInfo.Order["FromFile"];
var compilerCmd = BasicInfo.Order["Compiler"];
var syntaxTestCmd = BasicInfo.Order["SyntaxTest"];
var helpCmd = BasicInfo.Order["Help"];
var varCmd = BasicInfo.Order["Var"];
var infoCmd = BasicInfo.Order["Info"];
var importCmd = BasicInfo.Order["Import"];
var changeImportCmd = BasicInfo.Order["ChangeImport"];
var installCmd = BasicInfo.Order["Install"];
var removeCmd = BasicInfo.Order["Remove"];

// 帮助命令
if (command == helpCmd)
{
    Console.WriteLine(BasicInfo.Help);
    return;
}

// 版本信息命令
if (command == varCmd)
{
    Console.WriteLine($"Old8Lang 版本: {langInfo.Ver}");
    return;
}

// 语言信息命令
if (command == infoCmd)
{
    Console.WriteLine("========================================");
    Console.WriteLine(BasicInfo.Info());
    Console.WriteLine("========================================");
    return;
}

// 导入库信息命令
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

// 修改导入路径命令
if (command == changeImportCmd)
{
    if (args.Length < 2)
    {
        Console.WriteLine("错误: 缺少导入路径参数");
        Console.WriteLine("使用: Old8Lang.App -change <路径>");
        return;
    }

    var newPath = args[1];
    var updatedInfo = Apis.ChangeBasicInfo(newPath, langInfo.Ver);
    Console.WriteLine($"\n导入路径已更新为: {updatedInfo.ImportPath}");
    return;
}

// 安装命令（占位）
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
        var b = langInterpreter.Build(code, args[1]);
        b.Run(langInterpreter.Manager);
    }
    catch (Exception e)
    {
#if DEBUG
        throw;
#else
        Console.WriteLine(e.Message);
#endif
    }

    return;
}

if (command == compilerCmd)
{
    if (args.Length < 2)
    {
        Console.WriteLine("错误: 缺少文件参数");
        Console.WriteLine("使用: Old8Lang.App -c <文件名>");
        return;
    }

    var ext = Path.GetExtension(args[1]).ToLower();
    if (ext != ".old8" && ext != ".ol")
    {
        Console.WriteLine($"不支持的文件扩展名: {ext}，仅支持 .old8 和 .ol 文件");
        return;
    }

    var interpreter = new LangInterpreter();
    var sw = new Stopwatch();
    sw.Start();
    var build = interpreter.Build(Apis.FromFile(args[1]), args[1]);
    sw.Stop();
    var ts = sw.Elapsed.TotalMilliseconds;
    var time = $"------------------\nParser Build Time : {ts}ms\n";
    var milliseconds = ts;

    var action = Compiler.Compile(build, args[1], interpreter);

    sw.Restart();
    try
    {
        action();
    }
    catch (Exception e)
    {
#if DEBUG
        throw;
#else
        Console.WriteLine(e.Message);
#endif
    }

    sw.Stop();
    ts = sw.Elapsed.TotalMilliseconds;
    time += $"Process Run Time : {ts}ms\n";
    milliseconds += ts;
    time += $"Total : {milliseconds}ms";
    Console.WriteLine(time);
}

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
        var sw = new Stopwatch();
        sw.Start();
        var code = Apis.FromFile(args[1]);
        var build = interpreter.Build(code, args[1]);
        sw.Stop();
        var ts = sw.Elapsed.TotalMilliseconds;

        Console.WriteLine($"------------------\nSyntax Test Result\nParser Build Time : {ts}ms\n------------------");
        Console.WriteLine(build.ToCode());
    }
    catch (Exception e)
    {
#if DEBUG
        throw;
#else
        Console.WriteLine(e.Message);
#endif
    }
}