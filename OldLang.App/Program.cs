using System.Text;
using Old8Lang;

Lang("/home/luckyfish/RiderProjects/Old8Lang/Old8Lang/Exceple/文本文件.txt");

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

void Run(bool isdir,string path)
{
    string code = "";
    if (isdir)
        code = APIs.FromDirectory(path);
    else
        code = APIs.FromFile(path);
    var codeinfo = APIs.CslyUsing(code);
    
    // error
    codeinfo.Error.ForEach(x => Console.WriteLine(x));

    // variable and value
    Console.Write("\nvariable and value: \n|");
    Console.WriteLine(codeinfo.Manager);

    Console.WriteLine("\n"+codeinfo.Time);
}
