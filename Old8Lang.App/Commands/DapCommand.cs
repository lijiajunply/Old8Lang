using System.Text;
using Old8Lang.Bytecode.Debugger;
using Old8Lang.ModuleSystem.StandardLibrary;

namespace Old8Lang.App.Commands;

/// <summary>
/// Debug Adapter Protocol (DAP) 服务端命令：通过标准输入/输出提供与 VS Code 兼容的 VM 调试协议。
/// 启动后将脚本的 Console.Out/Console.Error 重定向为 DAP output 事件，避免普通打印破坏协议帧。
/// </summary>
public sealed class DapCommand : ICommand
{
    public string Name => "--dap";
    public string Description => "以 Debug Adapter Protocol (DAP) 模式启动 VM 调试适配器（stdio）";
    public string Help => "使用: Old8Lang.App --dap";

    public int Execute(string[] args)
    {
        var inputStream = Console.OpenStandardInput();
        var outputStream = Console.OpenStandardOutput();

        var originalOut = Console.Out;
        var originalErr = Console.Error;

        using var server = new VmDapServer(
            inputStream,
            outputStream,
            StandardLibraryRegistry.DefaultCatalog);

        using var stdoutWriter = new DapOutputTextWriter(text => server.SendOutputEvent("stdout", text));
        using var stderrWriter = new DapOutputTextWriter(text => server.SendOutputEvent("stderr", text));

        Console.SetOut(stdoutWriter);
        Console.SetError(stderrWriter);

        try
        {
            server.RunAsync().GetAwaiter().GetResult();
            return 0;
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }

    private sealed class DapOutputTextWriter(Action<string> emit) : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            emit(value.ToString());
        }

        public override void Write(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                emit(value);
            }
        }

        public override void WriteLine(string? value)
        {
            emit((value ?? string.Empty) + "\n");
        }
    }
}
