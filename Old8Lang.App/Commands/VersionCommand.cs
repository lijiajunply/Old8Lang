namespace Old8Lang.App.Commands;

/// <summary>
/// 版本信息命令
/// </summary>
public class VersionCommand : ICommand
{
    public string Name => "-var";
    public string Description => "显示当前版本号";
    public string Help => "使用: Old8Lang.App -var";

    public Task<int> ExecuteAsync(string[] args)
    {
        throw new NotImplementedException();
    }
}
