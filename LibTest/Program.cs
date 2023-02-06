using Old8LangLib;

Console.WriteLine(typeof(Math));
Console.WriteLine(OS.OsInfo());
Console.WriteLine(OS.Process("neofetch"));


ColorfulTerminal.PrintAscii("hello,world");

FileLib.CopyFile("/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/bin/Debug/net6.0/Old8LangLib.dll","/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/OldLib/dll/Old8LangLib.dll");
FileLib.CopyFile("/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/bin/Debug/net6.0/Old8LangLib.dll","/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/OldLib/Net/dll/Old8LangLib.dll");