using Old8Lang;

//APIs.ChangeBasicInfo("/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/OldLib/","0.3.0");
string[] sadf = { "-f" ,"/home/luckyfish/RiderProjects/Old8Lang/Old8Lang/Ex/init.ws"};
//args = new[] { "import" };
args = args.Length == 0?sadf:args;
if (args.Length == 0)
    args = Console.ReadLine().Split(" ");

Lang(args);

#region LangRun

void Lang(string[] oder)
{
    var a = APIs.Read_JSON();
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
        var b = APIs.ChangeBasicInfo(oder[1],a.Ver);
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
        return;
    }
}

void Run(bool isdic,string path)
{
    var info = APIs.CslyUsing(path,isdic);
    info.Error.ForEach(x => Console.WriteLine(x));
    Console.WriteLine("\n"+info.Time);
}


#endregion