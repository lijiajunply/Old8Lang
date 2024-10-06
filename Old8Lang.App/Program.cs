using Old8Lang;
using Old8Lang.CslyParser;

string[] strings = ["-f", Path.Combine(Path.GetDirectoryName(BasicInfo.CodePath)!,
    "Old8Lang", "Docs", "first.ws")];

args = args.Length == 0 ? strings : args;
if (args.Length == 0)
    args = Console.ReadLine()!.Split(" ");

//args = Array.Empty<string>();
Lang(args);

#region LangRun

void Lang(IReadOnlyList<string> order)
{
    var a = Apis.ReadJson();

    if (order.Count == 0)
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
                b.Run(ref i.Manager);
            else
                error.ForEach(Console.WriteLine);
        }
    }

    if (order[0] == BasicInfo.Order["FromFile"])
    {
        Run(false, order[1]);
        return;
    }

    if (order[0] == BasicInfo.Order["FromDir"])
    {
        Run(true, order[1]);
        return;
    }

    if (order[0] == BasicInfo.Order["Import"])
    {
        foreach (var VARIABLE in a.LibInfos)
            Console.WriteLine($"LibName:{VARIABLE.LibName} Var:{VARIABLE.Var} IsDir:{VARIABLE.IsDir}");
        Console.WriteLine("in:" + a.ImportPath);
        return;
    }

    if (order[0] == BasicInfo.Order["ChangeImport"])
    {
        var b = Apis.ChangeBasicInfo(order[1], a.Ver);
        Console.WriteLine("now:" + b.ImportPath);
        return;
    }

    if (order[0] == BasicInfo.Order["Var"])
    {
        Console.WriteLine(a.Ver);
        return;
    }

    if (order[0] == BasicInfo.Order["Info"])
    {
        Console.WriteLine(BasicInfo.Info());
        return;
    }

    if (order[0] == BasicInfo.Order["Install"])
    {
        Console.WriteLine(order[^1]);
        return;
    }

    if (order[0] == BasicInfo.Order["Help"])
    {
        Console.WriteLine(BasicInfo.Help);
        return;
    }

    if (order[0] == BasicInfo.Order["Remove"])
    {
        Console.WriteLine(order[^1]);
    }
}

void Run(bool isDic, string path)
{
    Apis.CslyUsing(path, isDic);
}

#endregion