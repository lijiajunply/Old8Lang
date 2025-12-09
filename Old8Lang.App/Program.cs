using System.Diagnostics;
using Old8Lang;
using Old8Lang.Compiler;
using Old8Lang.LangParser;

// fib , compiler

#if DEBUG
string[] strings =
[
    "-f", "/Users/luckyfish/Documents/Project/RiderProjects/Old8Lang/InterpreterTests/test_func.old8"
];

args = args.Length == 0 ? strings : args;
#endif

if (args.Length == 0)
    args = Console.ReadLine()!.Split(" ");

var a = Apis.ReadJson();

if (args.Length == 0)
{
    Console.WriteLine("Command Line Mode");
    var i = new LangInterpreter();

    while (true)
    {
        Console.Write(">");
        var code = Console.ReadLine();
        if (string.IsNullOrEmpty(code)) continue;
        if (code == "exit") return;
        try
        {
            var b = i.Build(code: code);
            b.Run(i.Manager);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}

if (args[0] == BasicInfo.Order["FromFile"])
{
    // 验证文件扩展名
    var ext = Path.GetExtension(args[1]).ToLower();
    if (ext != ".old8" && ext != ".ol")
    {
        Console.WriteLine($"不支持的文件扩展名: {ext}，仅支持 .old8 和 .ol 文件");
        return;
    }

    var aLangInterpreter = new LangInterpreter
    {
        Manager = { Path = args[1] }
    };
    try
    {
        var code = Apis.FromFile(args[1]);
        var b = aLangInterpreter.Build(code, args[1]);
        b.Run(aLangInterpreter.Manager);
    }
    catch (Exception e)
    {
#if DEBUG
        Console.WriteLine(e);
        throw;
#else
        Console.WriteLine(e.Message);
#endif
    }

    // Apis.CslyUsing(args[1], false);
    return;
}

// if (args[0] == BasicInfo.Order["FromDir"])
// {
//     Apis.CslyUsing(args[1], true);
//     return;
// }

if (args[0] == BasicInfo.Order["Import"])
{
    foreach (var libInfo in a.LibInfos)
        Console.WriteLine($"LibName:{libInfo.LibName} Var:{libInfo.Var} IsDir:{libInfo.IsDir}");
    Console.WriteLine("in:" + a.ImportPath);
    return;
}

if (args[0] == BasicInfo.Order["ChangeImport"])
{
    var b = Apis.ChangeBasicInfo(args[1], a.Ver);
    Console.WriteLine("now:" + b.ImportPath);
    return;
}

if (args[0] == BasicInfo.Order["Var"])
{
    Console.WriteLine(a.Ver);
    return;
}

if (args[0] == BasicInfo.Order["Info"])
{
    Console.WriteLine(BasicInfo.Info());
    return;
}

if (args[0] == BasicInfo.Order["Install"])
{
    Console.WriteLine(args[^1]);
    return;
}

if (args[0] == BasicInfo.Order["Help"])
{
    Console.WriteLine(BasicInfo.Help);
    return;
}

if (args[0] == BasicInfo.Order["Remove"])
{
    Console.WriteLine(args[^1]);
}

if (args[0] == BasicInfo.Order["Compiler"])
{
    // 验证文件扩展名
    var ext = Path.GetExtension(args[1]).ToLower();
    if (ext != ".old8" && ext != ".ol")
    {
        Console.WriteLine($"不支持的文件扩展名: {ext}，仅支持 .old8 和 .ol 文件");
        return;
    }

    var interpreter = new LangInterpreter();
    var sw = new Stopwatch();
    sw.Start();
    var build = interpreter.Build(Apis.FromFile(args[1]));
    sw.Stop();
    var ts = sw.Elapsed.TotalMilliseconds;
    var time = $"------------------\nParser Build Time : {ts}ms\n";
    var milliseconds = ts;

    var aDelegate = Compiler.Compile(build, args[1], interpreter);

    sw.Restart();
    aDelegate();
    sw.Stop();
    ts = sw.Elapsed.TotalMilliseconds;
    time += $"Process Run Time : {ts}ms\n";
    milliseconds += ts;
    time += $"Total : {milliseconds}ms";
    Console.WriteLine(time);
}

if (args[0] == BasicInfo.Order["SyntaxTest"])
{
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
        Console.WriteLine(e);
        throw;
#else
        Console.WriteLine(e.Message);
#endif
    }
}