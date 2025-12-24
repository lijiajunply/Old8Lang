using Old8Lang.Interpreter;

namespace Old8Lang.App.Commands;

/// <summary>
/// 解释执行文件命令
/// </summary>
public class FromFileCommand : ICommand
{
    public string Name => "-f";
    public string Description => "解释执行指定的 .old8 或 .ol 文件";
    public string Help => "使用: Old8Lang.App -f <文件名>";

    public Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("错误: 缺少文件参数");
            Console.WriteLine(Help);
            return Task.FromResult(1);
        }

        var langInterpreter = new LangInterpreter();

        try
        {
            var code = Apis.FromFile(args[0]);
            var ast = langInterpreter.Build(code, args[0]);
            ast.Run(langInterpreter.Manager);
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
