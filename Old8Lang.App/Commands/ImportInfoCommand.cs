namespace Old8Lang.App.Commands;

/// <summary>
/// 导入库信息命令
/// </summary>
public class ImportInfoCommand : ICommand
{
    public string Name => "import";
    public string Description => "显示导入库信息";
    public string Help => "使用: Old8Lang.App import";

    public Task<int> ExecuteAsync(string[] args)
    {
        throw new NotImplementedException();
    }
}
