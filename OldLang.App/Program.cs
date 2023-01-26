using Old8Lang;

Lang("/home/luckyfish/RiderProjects/Old8Lang/Old8Lang/Ex/init.ws");

void Lang(string path)
{
    try
    {
        if (args[0] != null)
        {
            if(args[0] == "-f")
                Run(false,args[1]);
            if(args[0] == "-d")
                Run(true,args[1]);
            else
                Run(false,args[0]);
        }
        else
        {
            Run(false,path);
        }
    }
    catch (Exception e)
    {
        Run(false,path);
    }
}

void Run(bool isdic,string path)
{
    var    cslyUsing = APIs.CslyUsing(path,isdic);
    cslyUsing.Error.ForEach(x => Console.WriteLine(x));
    // variable and value
    //Console.Write("\nvariable and value: \n");
    //Console.WriteLine(cslyUsing.Manager);
    Console.WriteLine("\n"+cslyUsing.Time);
}
