using Old8LangLib;
using BindingFlags = System.Reflection.BindingFlags;


//Console.WriteLine(OS.OsInfo());
//Console.WriteLine(OS.Process("neofetch"));


//ColorfulTerminal.PrintAscii("hello,world");
Net  a = new Net("");
Type b = a.GetType();
var  c = b.GetConstructors()[0];
var g = c.Invoke(new []{"asdf"});
var d = b.GetMethod("Print");
d.Invoke(g,null);