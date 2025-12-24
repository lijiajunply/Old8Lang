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
        throw new NotImplementedException();
    }
}
