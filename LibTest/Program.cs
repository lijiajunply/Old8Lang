using Old8LangLib;


#region os class test

Console.WriteLine(OS.OsInfo());
Console.WriteLine(OS.Process("neofetch"));

#endregion

#region terminal class test

Terminal.Title("a");
Terminal.Print("hello,world\n");
Terminal.PrintLine("asdf");
var a =Terminal.ReadKey();
Terminal.PrintLine(a);
int asllc = Terminal.ReadAsllc();
Terminal.PrintLine(asllc.ToString());
Terminal.Clear();
//Terminal.Beep();

#endregion

#region file class test



#endregion

ColorfulTerminal.PrintAscii("hello,world");