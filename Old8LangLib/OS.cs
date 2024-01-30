using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Old8LangLib;

public static class OS
{
    public static string OsInfo()
    {
        var sb = new StringBuilder();
        sb.Append($"MachineName: {Environment.MachineName} \n");
        sb.Append($"UserName : {Environment.UserName} \n");
        sb.Append($"TickCount : {Environment.TickCount} \n");
        sb.Append($"WorkingSet : {Environment.WorkingSet} \n");
        return sb.ToString();
    }

    public static string Process(string code)
    {
        Process a = new Process();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            a.StartInfo.FileName = "cmd.exe";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            a.StartInfo.FileName = "/bin/bash";
        }

        a.StartInfo.UseShellExecute = false; //是否使用操作系统shell启动
        a.StartInfo.RedirectStandardInput = true; //接受来自调用程序的输入信息
        a.StartInfo.RedirectStandardOutput = true; //由调用程序获取输出信息
        a.StartInfo.RedirectStandardError = true; //重定向标准错误输出
        a.Start();
        a.StandardInput.WriteLine(code); //指令
        a.StandardInput.Close();

        return a.StandardOutput.ReadToEnd(); //输出
    }
}