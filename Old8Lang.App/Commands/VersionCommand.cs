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
        var langInfo = Apis.ReadJson();
        Console.WriteLine($"Old8Lang 版本: {langInfo.Var}");
        return Task.FromResult(0);
    }
}
