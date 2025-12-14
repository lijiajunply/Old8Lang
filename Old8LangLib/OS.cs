using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Old8LangLib;

/// <summary>
/// 操作系统相关功能模块，提供系统信息获取和命令执行功能
/// </summary>
public static class OS
{
    /// <summary>
    /// 获取操作系统基本信息
    /// </summary>
    /// <returns>包含机器名、用户名、系统启动时间和工作集大小的字符串</returns>
    public static string OsInfo()
    {
        var sb = new StringBuilder();
        sb.Append($"MachineName: {Environment.MachineName} \n");
        sb.Append($"UserName : {Environment.UserName} \n");
        sb.Append($"TickCount : {Environment.TickCount} \n");
        sb.Append($"WorkingSet : {Environment.WorkingSet} \n");
        return sb.ToString();
    }

    /// <summary>
    /// 执行系统命令
    /// </summary>
    /// <param name="code">要执行的命令字符串</param>
    /// <returns>命令执行的标准输出结果</returns>
    /// <exception cref="ArgumentNullException">当命令字符串为空时抛出</exception>
    /// <exception cref="InvalidOperationException">当命令执行失败时抛出</exception>
    public static string Process(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            throw new ArgumentNullException(nameof(code), "命令字符串不能为空");
        }

        var a = new Process();

        // 根据操作系统设置命令解释器
        a.StartInfo.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "cmd.exe"
            :
            // Linux 和 macOS 都使用 bash
            "/bin/bash";

        a.StartInfo.UseShellExecute = false; // 不使用操作系统shell启动
        a.StartInfo.RedirectStandardInput = true; // 接受来自调用程序的输入信息
        a.StartInfo.RedirectStandardOutput = true; // 由调用程序获取输出信息
        a.StartInfo.RedirectStandardError = true; // 重定向标准错误输出
        a.Start();
        a.StandardInput.WriteLine(code); // 写入命令
        a.StandardInput.Close();

        return a.StandardOutput.ReadToEnd(); // 读取并返回输出结果
    }
}