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
        throw new NotImplementedException();
    }
}
