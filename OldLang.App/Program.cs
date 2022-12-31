using System.Diagnostics;
using System.Text;
using Old8Lang;

string FromFile(string filename)
{
    if (File.Exists(filename))
    {
        string a = File.ReadAllText(filename, Encoding.UTF8);
        return a;
    }
    else
    {
        return "a <- 1";
    }
}

string FromDirectory(string DirectoryName)
{
    StringBuilder sb = new StringBuilder();
    DirectoryInfo info = new DirectoryInfo(DirectoryName);
    var fileInfos = info.GetFiles();
    bool a = false; 
    sb.Append(FromFile(DirectoryName + "/"+info.Name));
    foreach (var VARIABLE in fileInfos)
    {
        sb.Append(FromFile(VARIABLE.FullName));
    }

    var b = info.GetDirectories();
    foreach (var VARIABLE in b)
    {
        sb.Append(FromDirectory(VARIABLE.FullName));
    }
    return sb.ToString();
}

string code = FromFile("/home/luckyfish/文本文件.txt");
//code = "a <- 1+1 \nb <- true \nc <- 2 \nd <- a+c";

var codeinfo = APIs.CslyUsing(code);

Console.WriteLine(code);
// error
foreach (var VARIABLE in codeinfo.Error)
{
    Console.WriteLine(VARIABLE);
}
Console.WriteLine();
// variable and value
foreach (var VARIABLE in codeinfo.Manager.Variates)
{
    Console.Write(VARIABLE + " ");
}
Console.WriteLine();
foreach (var VARIABLE in codeinfo.Manager.VariateDirectValue)
{
    Console.Write(VARIABLE + " ");
}
Console.WriteLine();
foreach (var VARIABLE in codeinfo.Manager.Values)
{
    Console.Write(VARIABLE.Value + " ");
}
Console.WriteLine();
Console.WriteLine(codeinfo.Time);