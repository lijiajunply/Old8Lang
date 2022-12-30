using Old8Lang;

// file
Console.Write("filename:");
//var filename = Console.ReadLine();
//var a = new StreamReader(filename);
//var code = a.ReadToEnd();
var code = "a <- 1";
var codeinfo = APIs.CslyUsing(code);
Console.WriteLine(code);
if (codeinfo.Item2 is not null)
{
    foreach (var VARIABLE in codeinfo.Item2)
        Console.WriteLine(VARIABLE);
}
else
{
    Console.WriteLine(code);
    foreach (var VARIABLE in codeinfo.Item1.Variates)
    {
        Console.Write(VARIABLE.IdName + " ");
    }

    foreach (var VARIABLE in codeinfo.Item1.VariateDirectValue)
    {
        Console.Write(VARIABLE.ToString() + " ");
    }

    foreach (var VARIABLE in codeinfo.Item1.Values)
    {
        Console.Write(VARIABLE.Value.Value.ToString() + " ");
    }
}

