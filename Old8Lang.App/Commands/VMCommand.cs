using System.Diagnostics;
using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.App.Commands;

/// <summary>
/// 虚拟机模式执行命令
/// </summary>
public class VMCommand : ICommand
{
    public string Name => "-vm";
    public string Description => "使用虚拟机模式编译并执行指定的 .old8 或 .ol 文件";
    public string Help => "使用: Old8Lang.App -vm <文件名> [-D SYMBOL1] [-D SYMBOL2] ... [--debug]";

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
        var debugMode = false;

        // 解析参数
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "-D" && i + 1 < args.Length)
            {
                symbols.Add(args[i + 1]);
                i++; // 跳过符号名
            }
            else if (args[i] == "--debug")
            {
                debugMode = true;
            }
        }

        // 创建预编译符号管理器
        var preprocessorSymbols = new PreprocessorSymbols(symbols);

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

            // 编译为字节码
            stopwatch.Restart();
            var compiler = new BytecodeCompiler();
            var bytecodeFile = compiler.Compile(ast);
            stopwatch.Stop();
            var compileTime = stopwatch.Elapsed.TotalMilliseconds;

            // 调试输出：显示编译后的类信息
            if (debugMode)
            {
                Console.WriteLine("\n=== 编译后的类信息 ===");
                foreach (var classMetadata in bytecodeFile.Classes)
                {
                    Console.WriteLine($"类: {classMetadata.Name}");
                    Console.WriteLine($"  方法数量: {classMetadata.Methods.Count}");
                    foreach (var method in classMetadata.Methods)
                    {
                        Console.WriteLine($"    - {method.Name} (静态: {method.IsStatic})");
                    }
                }
                Console.WriteLine("======================\n");
            }

            // 执行字节码
            stopwatch.Restart();
            var vm = new VirtualMachine(bytecodeFile);
            vm.Execute();
            stopwatch.Stop();
            var executionTime = stopwatch.Elapsed.TotalMilliseconds;

            // 输出时间统计
            Console.WriteLine("------------------");
            Console.WriteLine($"Parser Build Time : {parseTime}ms");
            Console.WriteLine($"Bytecode Compile Time : {compileTime}ms");
            Console.WriteLine($"VM Execution Time : {executionTime}ms");
            Console.WriteLine($"Total : {parseTime + compileTime + executionTime}ms");

            return 0;
        }
        catch (Exception e)
        {
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"虚拟机执行错误: {e.Message}");
            Console.WriteLine($"错误类型: {e.GetType().Name}");
            Console.WriteLine($"堆栈跟踪: {e.StackTrace}");
            Console.ResetColor();
#else
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"虚拟机执行错误: {e.Message}");
            Console.ResetColor();
#endif
            return 1;
        }
    }
}
