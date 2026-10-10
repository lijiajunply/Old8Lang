using System.Diagnostics;

namespace Old8Lang.Tests.Compiler.Modules.Extern;

/// <summary>
/// 验证 CLI 使用 Python extern 后能够完成进程收尾。
/// </summary>
[Trait("Category", "Performance")]
public class PythonProcessExitTests
{
    /// <summary>
    /// Python extern 输出完成后必须在有限时间内退出，不能遗留 Python.NET 后台资源。
    /// </summary>
    [Fact]
    public async Task PythonExternCli_ExitsAfterExecution()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(repositoryRoot, "TestFiles", "InterpreterTests", "test_python_simple.old8");
        var appDll = Path.Combine(repositoryRoot, "Old8Lang.App", "bin", "Debug", "net10.0", "Old8Lang.App.dll");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (File.Exists(appDll))
        {
            startInfo.ArgumentList.Add(appDll);
        }
        else
        {
            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--no-restore");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(Path.Combine(repositoryRoot, "Old8Lang.App"));
            startInfo.ArgumentList.Add("--");
        }

        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add(scriptPath);

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        var waitTask = process.WaitForExitAsync();
        var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(15)));

        if (completed != waitTask)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await Task.WhenAll(outputTask, errorTask);
            Assert.Fail($"Python extern CLI did not exit within 15 seconds.\n{await outputTask}\n{await errorTask}");
        }

        var output = await outputTask;
        var error = await errorTask;
        Assert.Equal(0, process.ExitCode);
        Assert.Contains("Result: 30", output);
        Assert.Contains("Done!", output);
        Assert.DoesNotContain("Unhandled exception", error);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Old8Lang.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("无法定位 Old8Lang 仓库根目录。");
    }
}
