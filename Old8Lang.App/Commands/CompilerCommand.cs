using System.Diagnostics;
using Old8Lang.Interpreter;

namespace Old8Lang.App.Commands;

/// <summary>
/// 编译执行文件命令
/// </summary>
public class CompilerCommand : ICommand
{
    public string Name => "-c";
    public string Description => "编译并执行指定的 .old8 或 .ol 文件";
    public string Help => "使用: Old8Lang.App -c <文件名>";

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

        var interpreter = new LangInterpreter();
        var stopwatch = new Stopwatch();

        try
        {
            // 测量解析时间
            stopwatch.Start();
            var ast = interpreter.Build(Apis.FromFile(args[0]), args[0]);
            stopwatch.Stop();
            var parseTime = stopwatch.Elapsed.TotalMilliseconds;
            var timeInfo = $"------------------\nParser Build Time : {parseTime}ms\n";
            var totalTime = parseTime;

            // 编译代码
            var compiledAction = Compiler.Compiler.Compile(ast, args[0], interpreter);

            // 测量执行时间
            stopwatch.Restart();
            compiledAction();
            stopwatch.Stop();
            var executionTime = stopwatch.Elapsed.TotalMilliseconds;
            timeInfo += $"Process Run Time : {executionTime}ms\n";
            totalTime += executionTime;
            timeInfo += $"Total : {totalTime}ms";
            Console.WriteLine(timeInfo);

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
