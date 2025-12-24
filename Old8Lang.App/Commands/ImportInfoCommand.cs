using Old8Lang;

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
        var langInfo = Apis.ReadJson();
        Console.WriteLine("========================================");
        Console.WriteLine("导入库信息:");
        Console.WriteLine("========================================");
        foreach (var libInfo in langInfo.LibInfos)
        {
            Console.WriteLine(
                $"库名: {libInfo.LibName} | 版本: {libInfo.Var} | 类型: {(libInfo.IsDir ? "目录" : "文件")}");
        }

        Console.WriteLine($"\n导入路径: {langInfo.ImportPath}");
        Console.WriteLine("========================================");

        return Task.FromResult(0);
    }
}
