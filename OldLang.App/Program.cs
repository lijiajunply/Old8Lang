using Old8Lang;

string code = "a <- 1 b <- true c <- 2 d <- a+c";
var codeinfo = APIs.CslyUsing(code);
//Console.WriteLine(code);
Console.WriteLine(code);
// error
foreach (var VARIABLE in codeinfo.Item2)
{
    Console.WriteLine(VARIABLE);
}
// variable and value
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