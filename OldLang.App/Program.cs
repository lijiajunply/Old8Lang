using System.Text;
using Old8Lang;

// file
Console.Write("filename:");
var filename = Console.ReadLine();
var a = new StreamReader(filename);
StringBuilder sb = new StringBuilder();
string line = "";
while ((line = a.ReadLine()) != null)
{
    Console.WriteLine(line);
    sb.Append(line);
}

string code = sb.ToString();
//code = "a <- 1\nb <- 2";
var codeinfo = APIs.CslyUsing(code);
//Console.WriteLine(code);
Console.WriteLine(code);

foreach (var VARIABLE in codeinfo.Item2)
{
    Console.WriteLine(VARIABLE);
}

foreach (var VARIABLE in codeinfo.Item1.Variates)
{
    Console.Write(VARIABLE + " ");
}

foreach (var VARIABLE in codeinfo.Item1.VariateDirectValue)
{
    Console.Write(VARIABLE + " ");
}

foreach (var VARIABLE in codeinfo.Item1.Values)
{
    Console.Write(VARIABLE.Value + " ");
}