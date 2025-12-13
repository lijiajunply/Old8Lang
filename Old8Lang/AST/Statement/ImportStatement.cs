using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class ImportItem(string name, string? alias = null)
{
    public readonly string Name = name;
    public readonly string Alias = alias ?? name;
}

public class ImportStatement(
    string importString,
    SourcePosition position = default,
    List<ImportItem>? importSpecifiers = null,
    bool fromClause = false
) : OldStatement(position)
{
    private readonly List<ImportItem> ImportSpecifiers = importSpecifiers ?? [];

    public override void Run(VariateManager manager)
    {
        var moduleName = importString;

        if (manager.LangInfo!.LibInfos.Any(x => moduleName == x.LibName))
        {
            var b = manager.LangInfo.LibInfos.Where(x => x.LibName == moduleName).Select(x => x.IsDir).ToArray()[0];
            // 检查文件扩展名，只支持.old8和.ol
            var fileName = moduleName;
            var ext = Path.GetExtension(fileName).ToLower();
            if (!b && ext != ".old8" && ext != ".ol")
            {
                fileName += ".old8"; // 默认使用.old8扩展名
            }

            var path = Path.Combine(manager.LangInfo.ImportPath, fileName);

            // 检查文件或目录是否存在
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                // 尝试构建绝对路径
                var absolutePath = Path.GetFullPath(path);
                if (!File.Exists(absolutePath) && !Directory.Exists(absolutePath))
                {
                    // 尝试从应用程序基目录查找
                    var appPath = Path.Combine(AppContext.BaseDirectory, path);
                    if (!File.Exists(appPath) && !Directory.Exists(appPath))
                    {
                        // 所有尝试都失败，抛出导入错误
                        throw new ImportError(Position, moduleName);
                    }

                    path = appPath;
                }
                else
                {
                    path = absolutePath;
                }
            }

            var previousPath = manager.Path;
            manager.Path = path;
            var code = b ? Apis.FromDirectory(path) : Apis.FromFile(path);
            var a = manager.Interpreter.Build(code: code);

            if (fromClause)
            {
                // 对于命名导入，我们需要先执行导入，然后根据需要获取指定的变量
                // 暂时先导入所有内容，后续可以优化为只导入指定的变量
                a.ImportRun(manager);
            }
            else
            {
                // 传统导入：导入所有内容
                a.ImportRun(manager);
            }

            manager.Path = previousPath;
            return;
        }

        if (Apis.ImportInstall(moduleName))
        {
            var b = manager.LangInfo.LibInfos.Where(x => x.LibName == moduleName).Select(x => x.IsDir).ToArray()[0];
            // 检查文件扩展名，只支持.old8和.ol
            var fileName = moduleName;
            var ext = Path.GetExtension(fileName).ToLower();
            if (!b && ext != ".old8" && ext != ".ol")
            {
                fileName += ".old8"; // 默认使用.old8扩展名
            }

            var path = Path.Combine(manager.LangInfo.ImportPath, fileName);
            var previousPath = manager.Path;
            manager.Path = path;
            var code = b ? Apis.FromDirectory(path) : Apis.FromFile(path);
            var a = manager.Interpreter.Build(code: code);
            a.ImportRun(manager);
            manager.Path = previousPath;
            return;
        }

        var dic = Path.GetDirectoryName(manager.Path)!;
        // 检查文件扩展名，只支持.old8和.ol
        var fileNameLocal = moduleName;
        var extLocal = Path.GetExtension(fileNameLocal).ToLower();
        if (extLocal != ".old8" && extLocal != ".ol")
        {
            fileNameLocal += ".old8"; // 默认使用.old8扩展名
        }

        // 修复：正确处理绝对路径和相对路径
        var filePath = Path.IsPathRooted(fileNameLocal) ? fileNameLocal : Path.Combine(dic, fileNameLocal);
        if (filePath.StartsWith("Users/") || filePath.StartsWith("Volumes/"))
        {
            filePath = "/" + filePath;
        }

        if (!File.Exists(filePath))
        {
            throw new ImportError(Position, moduleName);
        }

        var managerPath = manager.Path;
        manager.Path = filePath;
        var result = manager.Interpreter.Build(code: Apis.FromFile(filePath));

        if (fromClause)
        {
            // 对于命名导入，我们需要先执行导入，然后根据需要获取指定的变量
            // 暂时先导入所有内容，后续可以优化为只导入指定的变量
            result.ImportRun(manager);
        }
        else
        {
            // 传统导入：导入所有内容
            result.ImportRun(manager);
        }

        manager.Path = managerPath;
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        string moduleName = importString;

        var langInfo = Apis.ReadJson();
        if (langInfo.LibInfos.Any(x => moduleName == x.LibName))
        {
            var b = langInfo.LibInfos.Where(x => x.LibName == moduleName).Select(x => x.IsDir).ToArray()[0];
            // 检查文件扩展名，只支持.old8和.ol
            var fileName = moduleName;
            var ext = Path.GetExtension(fileName).ToLower();
            if (!b && ext != ".old8" && ext != ".ol")
            {
                fileName += ".old8"; // 默认使用.old8扩展名
            }

            var path = Path.Combine(langInfo.ImportPath, fileName);
            var code = b ? Apis.FromDirectory(path) : Apis.FromFile(path);

            var pPath = local.FilePath;
            local.FilePath = path;
            var block = local.Interpreter?.Build(code: code);
            block?.GenerateImportIl(ilGenerator, local);
            local.FilePath = pPath;
            return;
        }

        var dic = Path.GetDirectoryName(local.FilePath)!;
        // 检查文件扩展名，只支持.old8和.ol
        var fileNameLocal = moduleName;
        var extLocal = Path.GetExtension(fileNameLocal).ToLower();
        if (extLocal != ".old8" && extLocal != ".ol")
        {
            fileNameLocal += ".old8"; // 默认使用.old8扩展名
        }

        // 修复：正确处理绝对路径和相对路径
        var filePath = Path.IsPathRooted(fileNameLocal) ? fileNameLocal : Path.Combine(dic, fileNameLocal);
        if (filePath.StartsWith("Users/") || filePath.StartsWith("Volumes/"))
        {
            filePath = "/" + filePath;
        }

        if (!File.Exists(filePath)) return;

        var result = local.Interpreter?.Build(code: Apis.FromFile(filePath));
        result?.GenerateImportIl(ilGenerator, local);
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString()
    {
        if (ImportSpecifiers.Count > 0)
        {
            var specifiers = string.Join(", ",
                ImportSpecifiers.Select(s => s.Name == s.Alias ? s.Name : $"{s.Name} as {s.Alias}"));
            return $"import {{ {specifiers} }} from {importString}";
        }

        return $"import {importString}";
    }
}