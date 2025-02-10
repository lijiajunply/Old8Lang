using System.Diagnostics;
using Old8Lang;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;
using Old8Lang.LangParser;

const string newTokenCode = """
                            func main (a) {
                                return PrintLine(a)
                            }
                            """;

LangInterpreter.Tokenize(newTokenCode).ForEach(x => Console.WriteLine(x));
Console.WriteLine(new LangInterpreter().Build(newTokenCode));

// fib , compiler

#if DEBUG
string[] strings =
[
    "-c", Path.Combine(Path.GetDirectoryName(BasicInfo.CodePath)!,
        "Old8Lang", "Ex", "class.ws")
];

args = args.Length == 0 ? strings : args;
#endif

if (args.Length == 0)
    args = Console.ReadLine()!.Split(" ");

var a = Apis.ReadJson();

if (args.Length == 0)
{
    Console.WriteLine("Command Line Mode");
    var i = new Interpreter();

    while (true)
    {
        Console.Write(">");
        var code = Console.ReadLine();
        if (string.IsNullOrEmpty(code)) continue;
        if (code == "exit") return;
        var b = i.Build(code: code);
        var error = i.GetError();
        if (error.Count == 0)
            b.Run(i.Manager);
        else
            error.ForEach(Console.WriteLine);
    }
}

if (args[0] == BasicInfo.Order["FromFile"])
{
    Apis.CslyUsing(args[1], false);
    return;
}

if (args[0] == BasicInfo.Order["FromDir"])
{
    Apis.CslyUsing(args[1], true);
    return;
}

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
    var interpreter = new MiniInterpreter();
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