namespace Old8Lang.App.Commands;

/// <summary>
/// 修改导入路径命令
/// </summary>
public class ChangeImportCommand : ICommand
{
    public string Name => "-change";
    public string Description => "修改导入路径";
    public string Help => "使用: Old8Lang.App -change <路径>";

    public Task<int> ExecuteAsync(string[] args)
    {
        throw new NotImplementedException();
    }
}
