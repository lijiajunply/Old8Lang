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
        if (args.Length < 1)
        {
            Console.WriteLine("错误: 缺少导入路径参数");
            Console.WriteLine(Help);
            return Task.FromResult(1);
        }

        var newPath = args[0];
        var langInfo = Apis.ReadJson();
        var updatedInfo = Apis.ChangeBasicInfo(newPath, langInfo.Var);
        Console.WriteLine($"\n导入路径已更新为: {updatedInfo.ImportPath}");

        return Task.FromResult(0);
    }
}
