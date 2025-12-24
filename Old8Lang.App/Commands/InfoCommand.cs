namespace Old8Lang.App.Commands;

/// <summary>
/// 语言信息命令
/// </summary>
public class InfoCommand : ICommand
{
    public string Name => "info";
    public string Description => "显示 Old8Lang 语言信息";
    public string Help => "使用: Old8Lang.App info";

    public Task<int> ExecuteAsync(string[] args)
    {
        throw new NotImplementedException();
    }
}
