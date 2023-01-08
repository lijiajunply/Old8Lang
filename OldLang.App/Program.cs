using Old8Lang;

string code = APIs.FromFile("/home/luckyfish/文本文件.txt");
//code = "a <- 1+1 \nb <- true \nc <- 2 \nd <- a+c";

var codeinfo = APIs.CslyUsing(code);

Console.WriteLine(code);
// error
foreach (var VARIABLE in codeinfo.Error)
{
    Console.WriteLine(VARIABLE);
}
// variable and value
Console.Write("variable and value: \n|");
foreach (var VARIABLE in codeinfo.Manager.Variates)
{
    Console.Write(VARIABLE + "|");
}
Console.Write("\n|");
foreach (var VARIABLE in codeinfo.Manager.VariateDirectValue)
{
    Console.Write(VARIABLE + "|");
}
Console.Write("\n|");
foreach (var VARIABLE in codeinfo.Manager.Values)
{
    Console.Write(VARIABLE.Value + "|");
}
Console.Write("\nclass&func:\n");
foreach (var VARIABLE in codeinfo.Manager.ClassAndFuncInfo)
{
    Console.Write(VARIABLE.Key);
}
Console.WriteLine("\n"+codeinfo.Time);