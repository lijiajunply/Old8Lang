using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class ImportStatement(string importString, SourcePosition position = default) : OldStatement(position)
{
    public override void Run(VariateManager manager)
    {
        if (manager.LangInfo!.LibInfos.Any(x => importString == x.LibName))
        {
            var b = manager.LangInfo.LibInfos.Where(x => x.LibName == importString).Select(x => x.IsDir).ToArray()[0];
            // 检查文件扩展名，只支持.old8和.ol
            var fileName = importString;
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
                        throw new ImportError(Position, importString);
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
            a.ImportRun(manager);
            manager.Path = previousPath;
            return;
        }

        if (Apis.ImportInstall(importString))
        {
            var b = manager.LangInfo.LibInfos.Where(x => x.LibName == importString).Select(x => x.IsDir).ToArray()[0];
            // 检查文件扩展名，只支持.old8和.ol
            var fileName = importString;
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
        var fileNameLocal = importString;
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
            throw new ImportError(Position, importString);
        }

        var managerPath = manager.Path;
        manager.Path = filePath;
        var result = manager.Interpreter.Build(code: Apis.FromFile(filePath));
        result.ImportRun(manager);
        manager.Path = managerPath;
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        var langInfo = Apis.ReadJson();
        if (langInfo.LibInfos.Any(x => importString == x.LibName))
        {
            var b = langInfo.LibInfos.Where(x => x.LibName == importString).Select(x => x.IsDir).ToArray()[0];
            // 检查文件扩展名，只支持.old8和.ol
            var fileName = importString;
            var ext = Path.GetExtension(fileName).ToLower();
            if (!b && ext != ".old8" && ext != ".ol")
            {
                fileName += ".old8"; // 默认使用.old8扩展名
            }

            var path = Path.Combine(langInfo.ImportPath, fileName);
            var code = b ? Apis.FromDirectory(path) : Apis.FromFile(path);
            //var a = Interpreter.Build(code: code);

            var pPath = local.FilePath;
            local.FilePath = path;
            var block = local.Interpreter?.Build(code: code);
            block?.GenerateImportIl(ilGenerator, local);
            local.FilePath = pPath;
            return;
        }

        var dic = Path.GetDirectoryName(local.FilePath)!;
        // 检查文件扩展名，只支持.old8和.ol
        var fileNameLocal = importString;
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

    public override string ToString() => $"import {importString}";
}