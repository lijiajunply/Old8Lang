using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.App.Commands;

/// <summary>
/// 解释执行文件命令
/// </summary>
public class FromFileCommand : ICommand
{
    public string Name => "-f";
    public string Description => "解释执行指定的 .old8 或 .ol 文件";
    public string Help => "使用: Old8Lang.App -f <文件名> [-D SYMBOL1] [-D SYMBOL2] ...";

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

        var langInterpreter = new LangInterpreter();

        try
        {
            var code = Apis.FromFile(fileName);
            var ast = langInterpreter.Build(code, fileName, preprocessorSymbols);
            ast.Run(langInterpreter.Manager);
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
