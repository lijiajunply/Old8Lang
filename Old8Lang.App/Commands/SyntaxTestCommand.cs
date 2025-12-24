using System.Diagnostics;
using Old8Lang.Interpreter;

namespace Old8Lang.App.Commands;

/// <summary>
/// 语法测试命令
/// </summary>
public class SyntaxTestCommand : ICommand
{
    public string Name => "-s";
    public string Description => "对指定的 .old8 或 .ol 文件进行语法测试";
    public string Help => "使用: Old8Lang.App -s <文件名>";

    public Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("错误: 缺少文件参数");
            Console.WriteLine(Help);
            return Task.FromResult(1);
        }

        // 验证文件扩展名
        var ext = Path.GetExtension(args[0]).ToLower();
        if (ext != ".old8" && ext != ".ol")
        {
            Console.WriteLine($"不支持的文件扩展名: {ext}，仅支持 .old8 和 .ol 文件");
            return Task.FromResult(1);
        }

        try
        {
            var interpreter = new LangInterpreter();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var code = Apis.FromFile(args[0]);
            var ast = interpreter.Build(code, args[0]);
            stopwatch.Stop();
            var parseTime = stopwatch.Elapsed.TotalMilliseconds;

            Console.WriteLine(
                $"------------------\nSyntax Test Result\nParser Build Time : {parseTime}ms\n------------------");
            Console.WriteLine(ast.ToCode());

            return Task.FromResult(0);
        }
        catch (Exception e)
        {
#if DEBUG
            throw;
#endif
            Console.WriteLine(e.Message);
            return Task.FromResult(1);
        }
    }
}
