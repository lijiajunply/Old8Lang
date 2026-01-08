using System.Diagnostics;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.App.Commands;

/// <summary>
/// 编译执行文件命令
/// </summary>
public class CompilerCommand : ICommand
{
    public string Name => "-c";
    public string Description => "编译并执行指定的 .old8 或 .ol 文件";
    public string Help => "使用: Old8Lang.App -c <文件名> [-D SYMBOL1] [-D SYMBOL2] ...";

    public int Execute(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("错误: 缺少文件参数");
            Console.WriteLine(Help);
            return 1;
        }

        // 解析命令行参数
        var fileName = args[0];
        var symbols = new List<string>();

        // 解析 -D 参数
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "-D" && i + 1 < args.Length)
            {
                symbols.Add(args[i + 1]);
                i++; // 跳过符号名
            }
        }

        // 总是创建预编译符号管理器（即使没有符号也可以处理#define等指令）
        PreprocessorSymbols preprocessorSymbols = new PreprocessorSymbols(symbols);

        // 验证文件扩展名
        var ext = Path.GetExtension(fileName).ToLower();
        if (ext != ".old8" && ext != ".ol")
        {
            Console.WriteLine($"不支持的文件扩展名: {ext}，仅支持 .old8 和 .ol 文件");
            return 1;
        }

        var interpreter = new LangInterpreter();
        var stopwatch = new Stopwatch();

        try
        {
            // 测量解析时间
            stopwatch.Start();
            var ast = interpreter.Build(Apis.FromFile(fileName), fileName, preprocessorSymbols);
            stopwatch.Stop();
            var parseTime = stopwatch.Elapsed.TotalMilliseconds;
            var timeInfo = $"------------------\nParser Build Time : {parseTime}ms\n";
            var totalTime = parseTime;

            // 编译代码
            var compiledAction = Compiler.Compiler.Compile(ast, fileName, interpreter);

            // 测量执行时间
            stopwatch.Restart();
            compiledAction();
            stopwatch.Stop();
            var executionTime = stopwatch.Elapsed.TotalMilliseconds;
            timeInfo += $"Process Run Time : {executionTime}ms\n";
            totalTime += executionTime;
            timeInfo += $"Total : {totalTime}ms";
            Console.WriteLine(timeInfo);

            return 0;
        }
        catch (Exception e)
        {
#if DEBUG
            throw;
#endif
            Console.WriteLine(e.Message);
            return 1;
        }
    }
}
