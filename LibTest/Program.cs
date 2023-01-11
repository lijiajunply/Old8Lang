
using Old8LangLib;


#region os class test

Console.WriteLine(OS.OsInfo());
Console.WriteLine(OS.Process("neofetch"));

#endregion

#region terminal class test

Terminal.Title("a");

#endregion


//解压文件

//读取zip压缩文件里的文件
var filelist= ZipFile.OpenRead(zipPath).Entries.Select(s => s.Name).ToList();