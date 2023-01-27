using Old8Lang;

//APIs.JSON_Info("/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/bin/Debug/net6.0/Old8LangLib.dll",
//              "/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/OldLib/","0.3.0");
string[] sadf = { "-f" ,"/home/luckyfish/RiderProjects/Old8Lang/Old8Lang/Ex/init.ws"};
args = sadf;
Lang();
//Run(false,args[1]);

void Lang()
{
    if (args != null && args[0] != null)
    {
        var a = APIs.Read_JSON();
        if(args[0] == BasicInfo.Order["FromFile"])
            Run(false,args[1]);
        if(args[0] == BasicInfo.Order["FromDir"])
            Run(true,args[1]);
        if (args[0] == BasicInfo.Order["Import"])
        {
            foreach (var VARIABLE in a.ImPortTable)
                Console.WriteLine(VARIABLE.Key + VARIABLE.Value);
            Console.WriteLine("in:"+a.ImportPath);
        }
        if(args[0] == BasicInfo.Order["LibPath"])
            Console.WriteLine(a.LangLibDllPath);
        if (args[0] == BasicInfo.Order["ChangeLibPath"])
        {
            var b = APIs.JSON_Info(args[1],a.ImportPath,a.Ver);
            Console.WriteLine("now:"+b.LangLibDllPath);
        }
        if (args[0] == BasicInfo.Order["ChangeImport"])
        {
            var b = APIs.JSON_Info(a.LangLibDllPath,args[1],a.Ver);
            Console.WriteLine("now:"+b.LangLibDllPath);
        }
        if (args[0] == BasicInfo.Order["Var"])
            Console.WriteLine(a.Ver);
        if (args[0] == BasicInfo.Order["Info"])
            Console.WriteLine(BasicInfo.Info());
    }
}

void Run(bool isdic,string path)
{
    var    info = APIs.CslyUsing(path,isdic);
    info.Error.ForEach(x => Console.WriteLine(x));
    Console.WriteLine("\n"+info.Time);
}
