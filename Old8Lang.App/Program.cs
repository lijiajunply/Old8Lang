using System.Diagnostics;
using Old8Lang;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;
using Old8Lang.LangParser;

const string newTokenCode = """
                            // 基本计算和赋值

                            a <- 123+1
                            a <- 1233
                            b <- a >= 1233 
                            PrintLine(b)

                            // if语句
                            if a == 123
                              PrintLine("123")
                            elif a == 1234
                              PrintLine("1234")
                            elif a == 1233
                              PrintLine("1233")
                            else
                              PrintLine("false")

                            // for语句


                            for a <- 1,a <= 5,a++{
                                PrintLine(a)
                            }


                            // switch语句

                            switch a {
                                case 123
                                    PrintLine("1")
                                case 1233
                                    PrintLine("2")
                                default
                                    PrintLine("default")
                            }

                            for i in [1~3] {
                                PrintLine(i)
                            }

                            func main(a) {
                                PrintLine(a)
                            }

                            main("asdf")
                            """;

var block = new LangInterpreter().Build(newTokenCode);
Console.WriteLine(block.ToCode());

// fib , compiler

#if DEBUG
string[] strings =
[
    "-f", Path.Combine(Path.GetDirectoryName(BasicInfo.CodePath)!,
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
    var aLangInterpreter = new LangInterpreter
    {
        Manager = { Path = args[1] }
    };
    aLangInterpreter.Build(Apis.FromFile(args[1]));
    //Apis.CslyUsing(args[1], false);
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