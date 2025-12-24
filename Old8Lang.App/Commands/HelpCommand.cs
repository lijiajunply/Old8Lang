namespace Old8Lang.App.Commands;

/// <summary>
/// 帮助命令
/// </summary>
public class HelpCommand : ICommand
{
    public string Name => "-h";
    public string Description => "显示帮助信息";
    public string Help => "使用: Old8Lang.App -h";

    public Task<int> ExecuteAsync(string[] args)
    {
        Console.WriteLine(BasicInfo.Help);
        return Task.FromResult(0);
    }
}
