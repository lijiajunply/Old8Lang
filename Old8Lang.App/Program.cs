using Old8Lang;

//APIs.ChangeBasicInfo("/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/OldLib/","0.3.0");
string[] strings = { "-f" ,Path.Combine(Path.GetDirectoryName(BasicInfo.CodePath)!,"Old8Lang","Ex", "init.ws")};
//

args = args.Length == 0?strings:args;
if (args.Length == 0)
    args = Console.ReadLine()!.Split(" ");

Lang(args);

#region LangRun

void Lang(string[] oder)
{
    var a = Apis.ReadJson();
    if (oder[0] == BasicInfo.Order["FromFile"])
    {
        Run(false,oder[1]);
        return;
    }
    if (oder[0] == BasicInfo.Order["FromDir"])
    {
        Run(true,oder[1]);
        return;
    }
    if (oder[0] == BasicInfo.Order["Import"])
    {
        foreach (var VARIABLE in a.LibInfos)
            Console.WriteLine($"LibName:{VARIABLE.LibName} Var:{VARIABLE.Var} IsDir:{VARIABLE.IsDir}");
        Console.WriteLine("in:"+a.ImportPath);
        return;
    }
    if (oder[0] == BasicInfo.Order["ChangeImport"])
    {
        var b = Apis.ChangeBasicInfo(oder[1],a.Ver);
        Console.WriteLine("now:"+b.ImportPath);
        return;
    }
    if (oder[0] == BasicInfo.Order["Var"])
    {
        Console.WriteLine(a.Ver);
        return;
    }
    if (oder[0] == BasicInfo.Order["Info"])
    {
        Console.WriteLine(BasicInfo.Info());
    }
}

void Run(bool isdic,string path)
{
    var info = Apis.CslyUsing(path,isdic);
    info.Error.ForEach(Console.WriteLine);
    Console.WriteLine("\n"+info.Time);
}


#endregion