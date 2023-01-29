using Old8LangLib;
using File = Old8LangLib.File;


Console.WriteLine(OS.OsInfo());
Console.WriteLine(OS.Process("neofetch"));


ColorfulTerminal.PrintAscii("hello,world");

File.CopyFile("/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/bin/Debug/net6.0/Old8LangLib.dll","/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/OldLib/dll/Old8LangLib.dll");
File.CopyFile("/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/bin/Debug/net6.0/Old8LangLib.dll","/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/OldLib/Net/dll/Old8LangLib.dll");