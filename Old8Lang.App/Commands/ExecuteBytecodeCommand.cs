using System.Diagnostics;
using Old8Lang.Bytecode;

namespace Old8Lang.App.Commands;

/// <summary>
/// 执行字节码文件命令
/// </summary>
public class ExecuteBytecodeCommand : ICommand
{
    public string Name => "-execute";
    public string Description => "执行已编译的 .o8c 字节码文件";
    public string Help => "使用: Old8Lang.App -execute <文件.o8c> [--debug]";

    public int Execute(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("错误: 缺少文件参数");
            Console.WriteLine(Help);
            return 1;
        }

        var fileName = args[0];
        var debugMode = false;

        // 解析参数
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--debug")
            {
                debugMode = true;
            }
        }

        // 验证文件扩展名
        var ext = Path.GetExtension(fileName).ToLower();
        if (ext != ".o8c")
        {
            Console.WriteLine($"警告: 文件扩展名应为 .o8c，当前为: {ext}");
        }

        // 检查文件是否存在
        if (!File.Exists(fileName))
        {
            Console.WriteLine($"错误: 文件不存在: {fileName}");
            return 1;
        }

        var stopwatch = new Stopwatch();

        try
        {
            // 加载字节码文件
            stopwatch.Start();
            var bytecodeFile = BytecodeFile.LoadFromFile(fileName);
            stopwatch.Stop();
            var loadTime = stopwatch.Elapsed.TotalMilliseconds;

            Console.WriteLine($"已加载字节码文件: {fileName}");
            Console.WriteLine($"  函数数量: {bytecodeFile.Functions.Count}");
            Console.WriteLine($"  常量池大小: {bytecodeFile.ConstantPool.Count}");
            Console.WriteLine($"  全局变量数量: {bytecodeFile.GlobalVariables.Count}");
            Console.WriteLine();

            // 执行字节码
            stopwatch.Restart();
            var vm = new VirtualMachine(bytecodeFile);
            vm.Execute();
            stopwatch.Stop();
            var executionTime = stopwatch.Elapsed.TotalMilliseconds;

            // 输出时间统计
            Console.WriteLine("\n------------------");
            Console.WriteLine($"Load Time : {loadTime}ms");
            Console.WriteLine($"Execution Time : {executionTime}ms");
            Console.WriteLine($"Total : {loadTime + executionTime}ms");

            return 0;
        }
        catch (Exception e)
        {
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"执行错误: {e.Message}");
            Console.WriteLine($"错误类型: {e.GetType().Name}");
            Console.WriteLine($"堆栈跟踪: {e.StackTrace}");
            Console.ResetColor();
#else
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"执行错误: {e.Message}");
            Console.ResetColor();
#endif
            return 1;
        }
    }
}
