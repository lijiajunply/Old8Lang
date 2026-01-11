using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.App.Commands;

/// <summary>
/// 编译为字节码文件命令
/// </summary>
public class CompileBytecodeCommand : ICommand
{
    public string Name => "-compile";
    public string Description => "将 .old8 文件编译为 .o8c 字节码文件";
    public string Help => "使用: Old8Lang.App -compile <输入文件.old8> <输出文件.o8c> [-D SYMBOL1] ...";

    public int Execute(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("错误: 缺少参数");
            Console.WriteLine(Help);
            return 1;
        }

        var inputFile = args[0];
        var outputFile = args[1];
        var symbols = new List<string>();

        // 解析 -D 参数
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "-D" && i + 1 < args.Length)
            {
                symbols.Add(args[i + 1]);
                i++;
            }
        }

        // 验证输入文件扩展名
        var inputExt = Path.GetExtension(inputFile).ToLower();
        if (inputExt != ".old8" && inputExt != ".ol")
        {
            Console.WriteLine($"不支持的输入文件扩展名: {inputExt}，仅支持 .old8 和 .ol 文件");
            return 1;
        }

        // 验证输出文件扩展名
        var outputExt = Path.GetExtension(outputFile).ToLower();
        if (outputExt != ".o8c")
        {
            Console.WriteLine($"警告: 输出文件扩展名应为 .o8c，当前为: {outputExt}");
        }

        // 检查输入文件是否存在
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"错误: 输入文件不存在: {inputFile}");
            return 1;
        }

        try
        {
            Console.WriteLine($"正在编译: {inputFile}");

            // 创建预编译符号管理器
            PreprocessorSymbols preprocessorSymbols = new PreprocessorSymbols(symbols);

            // 解析AST
            var interpreter = new LangInterpreter();
            var ast = interpreter.Build(Apis.FromFile(inputFile), inputFile, preprocessorSymbols);

            // 编译为字节码
            var compiler = new BytecodeCompiler();
            var bytecodeFile = compiler.Compile(ast);

            // 保存到文件
            bytecodeFile.SaveToFile(outputFile);

            Console.WriteLine($"编译成功: {outputFile}");
            Console.WriteLine($"  函数数量: {bytecodeFile.Functions.Count}");
            Console.WriteLine($"  常量池大小: {bytecodeFile.ConstantPool.Count}");
            Console.WriteLine($"  全局变量数量: {bytecodeFile.GlobalVariables.Count}");

            // 显示函数列表
            if (bytecodeFile.Functions.Count > 0)
            {
                Console.WriteLine("\n函数列表:");
                foreach (var func in bytecodeFile.Functions)
                {
                    Console.WriteLine($"  - {func.Name}({string.Join(", ", func.Parameters)}) " +
                                    $"[{func.Instructions.Count} 指令, 栈深度: {func.MaxStackSize}]");
                }
            }

            return 0;
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"编译错误: {e.Message}");
#if DEBUG
            Console.WriteLine($"错误类型: {e.GetType().Name}");
            Console.WriteLine($"堆栈跟踪: {e.StackTrace}");
#endif
            Console.ResetColor();
            return 1;
        }
    }
}
